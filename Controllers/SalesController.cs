using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Globalization;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using InventarioMvc.Data;
using InventarioMvc.Models;

namespace InventarioMvc.Controllers
{
    [Authorize]
    public class SalesController : Controller
    {
        private readonly AppDbContext _context;

        public SalesController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /Sales
        public async Task<IActionResult> Index(string? q, DateTime? from, DateTime? to)
        {
            var query = _context.Sales
                .Include(s => s.Product)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim();
                query = query.Where(s => s.Product != null && s.Product.Name.Contains(q));
            }

            if (from.HasValue)
            {
                var f = from.Value.Date;
                query = query.Where(s => s.CreatedAt >= f);
            }
            if (to.HasValue)
            {
                var t = to.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(s => s.CreatedAt <= t);
            }

            var items = await query
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();

            ViewData["q"] = q;
            ViewData["from"] = from?.ToString("yyyy-MM-dd");
            ViewData["to"] = to?.ToString("yyyy-MM-dd");
            ViewData["TotalVentas"] = items.Sum(i => i.Total);
            ViewData["CantidadItems"] = items.Count;

            return View(items);
        }

        // GET: /Sales/Create
        public async Task<IActionResult> Create()
        {
            ViewBag.Products = await _context.Products
                .OrderBy(p => p.Name)
                .Select(p => new { p.Id, p.Name, p.Price, p.Stock })
                .ToListAsync();
            return View(new Sale { Quantity = 1 });
        }

        // POST: /Sales/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Sale model)
        {
            var product = await _context.Products.FindAsync(model.ProductId);
            if (product == null)
            {
                ModelState.AddModelError(nameof(Sale.ProductId), "Producto no encontrado.");
            }
            else
            {
                if (model.Quantity <= 0)
                    ModelState.AddModelError(nameof(Sale.Quantity), "Cantidad debe ser mayor a 0.");

                if (model.UnitPrice < 0)
                    ModelState.AddModelError(nameof(Sale.UnitPrice), "Precio inválido.");

                if (product.Stock < model.Quantity)
                    ModelState.AddModelError(nameof(Sale.Quantity), $"Stock insuficiente. Disponible: {product.Stock}");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Products = await _context.Products
                    .OrderBy(p => p.Name)
                    .Select(p => new { p.Id, p.Name, p.Price, p.Stock })
                    .ToListAsync();
                return View(model);
            }

            // Descontar stock y guardar venta con snapshots
            product!.Stock -= model.Quantity;

            var sale = new Sale
            {
                ProductId = product.Id,
                Quantity = model.Quantity,
                UnitPrice = model.UnitPrice,
                CostAtSale = product.Cost,          // snapshot costo
                ProductNameSnapshot = product.Name, // snapshot nombre
                CreatedAt = DateTime.UtcNow
            };

            _context.Sales.Add(sale);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // POST: /Sales/SellOne/5 (Vender 1 rápido)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SellOne(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

            if (product.Stock <= 0)
            {
                TempData["Error"] = $"No hay stock de '{product.Name}'.";
                return RedirectToAction("Index", "Products");
            }

            // Descontar 1 y guardar venta con snapshots
            product.Stock -= 1;

            var sale = new Sale
            {
                ProductId = product.Id,
                Quantity = 1,
                UnitPrice = product.Price,
                CostAtSale = product.Cost,          // snapshot costo
                ProductNameSnapshot = product.Name, // snapshot nombre
                CreatedAt = DateTime.UtcNow
            };

            _context.Sales.Add(sale);
            await _context.SaveChangesAsync();

            TempData["Ok"] = $"Vendiste 1 unidad de '{product.Name}'.";
            return RedirectToAction("Index", "Products");
        }

        // GET: /Sales/ExportCsv
        [HttpGet]
        public async Task<IActionResult> ExportCsv(string? q, DateTime? from, DateTime? to)
        {
            var query = _context.Sales
                .Include(s => s.Product)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim();
                query = query.Where(s => s.Product != null && s.Product.Name.Contains(q));
            }
            if (from.HasValue)
            {
                var f = from.Value.Date;
                query = query.Where(s => s.CreatedAt >= f);
            }
            if (to.HasValue)
            {
                var t = to.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(s => s.CreatedAt <= t);
            }

            var rows = await query
                .OrderByDescending(s => s.CreatedAt)
                .Select(s => new
                {
                    Fecha = s.CreatedAt.ToLocalTime(),
                    Producto = s.ProductNameSnapshot ?? (s.Product != null ? s.Product.Name : ""),
                    Cantidad = s.Quantity,
                    PrecioUnit = s.UnitPrice,
                    Total = s.UnitPrice * s.Quantity
                })
                .ToListAsync();

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Fecha,Producto,Cantidad,PrecioUnit,Total");

            var ci = CultureInfo.InvariantCulture;

            foreach (var r in rows)
            {
                var fecha = r.Fecha.ToString("yyyy-MM-dd HH:mm");
                var producto = r.Producto?.Replace("\"", "\"\"");
                if (producto?.Contains(',') == true) producto = $"\"{producto}\"";

                sb.AppendLine(string.Join(",", new[]
                {
                    fecha,
                    producto ?? "",
                    r.Cantidad.ToString(ci),
                    r.PrecioUnit.ToString(ci),
                    r.Total.ToString(ci)
                }));
            }

            var bytes = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
            var fileName = $"ventas_{DateTime.Now:yyyyMMdd_HHmm}.csv";
            return File(bytes, "text/csv", fileName);
        }

        // GET: /Sales/ExportXlsx
        [HttpGet]
        public async Task<IActionResult> ExportXlsx(string? q, DateTime? from, DateTime? to)
        {
            // Query base (mismos filtros)
            var query = _context.Sales
                .Include(s => s.Product)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim();
                query = query.Where(s => s.Product != null && s.Product.Name.Contains(q));
            }
            if (from.HasValue)
            {
                var f = from.Value.Date;
                query = query.Where(s => s.CreatedAt >= f);
            }
            if (to.HasValue)
            {
                var t = to.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(s => s.CreatedAt <= t);
            }

            // Usar snapshots si existen; si no, valores actuales
            var data = await query
                .OrderByDescending(s => s.CreatedAt)
                .Select(s => new
                {
                    FechaLocal = s.CreatedAt.ToLocalTime(),
                    Producto = s.ProductNameSnapshot ?? (s.Product != null ? s.Product.Name : ""),
                    CostoUnit = s.CostAtSale ?? (s.Product != null ? s.Product.Cost : 0m),
                    Cantidad = s.Quantity,
                    PrecioUnit = s.UnitPrice,
                    Total = s.UnitPrice * s.Quantity
                })
                .ToListAsync();

            using var wb = new XLWorkbook();

            // ===== Hoja 1: Ventas (detalle) =====
            var ws = wb.Worksheets.Add("Ventas");
            ws.Cell(1, 1).Value = "Fecha (local)";
            ws.Cell(1, 2).Value = "Producto";
            ws.Cell(1, 3).Value = "Cantidad";
            ws.Cell(1, 4).Value = "Precio unit.";
            ws.Cell(1, 5).Value = "Total";
            ws.Cell(1, 6).Value = "Costo unit.";
            ws.Cell(1, 7).Value = "Utilidad (aprox)";

            var row = 2;
            foreach (var r in data)
            {
                ws.Cell(row, 1).Value = r.FechaLocal;
                ws.Cell(row, 1).Style.DateFormat.Format = "yyyy-mm-dd hh:mm";
                ws.Cell(row, 2).Value = r.Producto;
                ws.Cell(row, 3).Value = r.Cantidad;
                ws.Cell(row, 4).Value = r.PrecioUnit;
                ws.Cell(row, 5).Value = r.Total;
                ws.Cell(row, 6).Value = r.CostoUnit;
                ws.Cell(row, 7).FormulaA1 = $"E{row}-C{row}*F{row}";
                row++;
            }

            ws.Cell(row, 2).Value = "Totales:";
            ws.Cell(row, 3).FormulaA1 = $"SUM(C2:C{row-1})";
            ws.Cell(row, 5).FormulaA1 = $"SUM(E2:E{row-1})";
            ws.Cell(row, 7).FormulaA1 = $"SUM(G2:G{row-1})";
            ws.Range(1, 1, 1, 7).Style.Font.SetBold();
            ws.Columns().AdjustToContents();

            // ===== Hoja 2: Resumen por producto =====
            var wsProd = wb.Worksheets.Add("Resumen por producto");

            var porProducto = data
                .GroupBy(x => x.Producto)
                .Select(g => new
                {
                    Producto = g.Key,
                    Cantidad = g.Sum(x => x.Cantidad),
                    Ingreso = g.Sum(x => x.Total),
                    CostoEstimado = g.Sum(x => x.CostoUnit * x.Cantidad),
                    Utilidad = g.Sum(x => (x.PrecioUnit - x.CostoUnit) * x.Cantidad),
                    PrecioProm = g.Sum(x => x.PrecioUnit * x.Cantidad) / Math.Max(1, g.Sum(x => x.Cantidad)),
                    CostoProm = g.Sum(x => x.CostoUnit * x.Cantidad) / Math.Max(1, g.Sum(x => x.Cantidad))
                })
                .OrderByDescending(x => x.Ingreso)
                .ToList();

            wsProd.Cell(1, 1).Value = "Producto";
            wsProd.Cell(1, 2).Value = "Cantidad";
            wsProd.Cell(1, 3).Value = "Ingreso";
            wsProd.Cell(1, 4).Value = "Costo estimado";
            wsProd.Cell(1, 5).Value = "Utilidad (aprox)";
            wsProd.Cell(1, 6).Value = "Precio prom.";
            wsProd.Cell(1, 7).Value = "Costo prom.";

            row = 2;
            foreach (var p in porProducto)
            {
                wsProd.Cell(row, 1).Value = p.Producto;
                wsProd.Cell(row, 2).Value = p.Cantidad;
                wsProd.Cell(row, 3).Value = p.Ingreso;
                wsProd.Cell(row, 4).Value = p.CostoEstimado;
                wsProd.Cell(row, 5).Value = p.Utilidad;
                wsProd.Cell(row, 6).Value = p.PrecioProm;
                wsProd.Cell(row, 7).Value = p.CostoProm;
                row++;
            }

            wsProd.Cell(row, 1).Value = "Totales:";
            wsProd.Cell(row, 2).FormulaA1 = $"SUM(B2:B{row-1})";
            wsProd.Cell(row, 3).FormulaA1 = $"SUM(C2:C{row-1})";
            wsProd.Cell(row, 4).FormulaA1 = $"SUM(D2:D{row-1})";
            wsProd.Cell(row, 5).FormulaA1 = $"SUM(E2:E{row-1})";
            wsProd.Range(1, 1, 1, 7).Style.Font.SetBold();
            wsProd.Columns().AdjustToContents();

            // ===== Hoja 3: Resumen por fecha =====
            var wsFecha = wb.Worksheets.Add("Resumen por fecha");

            var porFecha = data
                .GroupBy(x => x.FechaLocal.Date)
                .Select(g => new
                {
                    Fecha = g.Key,
                    Cantidad = g.Sum(x => x.Cantidad),
                    Ingreso = g.Sum(x => x.Total),
                    CostoEstimado = g.Sum(x => x.CostoUnit * x.Cantidad),
                    Utilidad = g.Sum(x => (x.PrecioUnit - x.CostoUnit) * x.Cantidad)
                })
                .OrderBy(x => x.Fecha)
                .ToList();

            wsFecha.Cell(1, 1).Value = "Fecha";
            wsFecha.Cell(1, 2).Value = "Cantidad";
            wsFecha.Cell(1, 3).Value = "Ingreso";
            wsFecha.Cell(1, 4).Value = "Costo estimado";
            wsFecha.Cell(1, 5).Value = "Utilidad (aprox)";

            row = 2;
            foreach (var d in porFecha)
            {
                wsFecha.Cell(row, 1).Value = d.Fecha;
                wsFecha.Cell(row, 1).Style.DateFormat.Format = "yyyy-mm-dd";
                wsFecha.Cell(row, 2).Value = d.Cantidad;
                wsFecha.Cell(row, 3).Value = d.Ingreso;
                wsFecha.Cell(row, 4).Value = d.CostoEstimado;
                wsFecha.Cell(row, 5).Value = d.Utilidad;
                row++;
            }

            wsFecha.Cell(row, 1).Value = "Totales:";
            wsFecha.Cell(row, 2).FormulaA1 = $"SUM(B2:B{row-1})";
            wsFecha.Cell(row, 3).FormulaA1 = $"SUM(C2:C{row-1})";
            wsFecha.Cell(row, 4).FormulaA1 = $"SUM(D2:D{row-1})";
            wsFecha.Cell(row, 5).FormulaA1 = $"SUM(E2:E{row-1})";
            wsFecha.Range(1, 1, 1, 5).Style.Font.SetBold();
            wsFecha.Columns().AdjustToContents();

            // Formato moneda
            void Money(IXLWorksheet sheet, params int[] cols)
            {
                foreach (var c in cols)
                    sheet.Column(c).Style.NumberFormat.Format = "#,##0.00";
            }
            Money(ws, 4, 5, 6, 7);
            Money(wsProd, 3, 4, 5, 6, 7);
            Money(wsFecha, 3, 4, 5);

            using var ms = new System.IO.MemoryStream();
            wb.SaveAs(ms);
            var fileName = $"ventas_{DateTime.Now:yyyyMMdd_HHmm}.xlsx";
            return File(ms.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
    }
}
