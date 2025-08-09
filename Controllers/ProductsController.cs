using System.Globalization;
using InventarioMvc.Data;
using InventarioMvc.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InventarioMvc.Controllers
{
    [Authorize]
    public class ProductsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;

        public ProductsController(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // GET: /Products
        public async Task<IActionResult> Index(string? q)
        {
            var query = _context.Products.AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim();
                query = query.Where(p =>
                    p.Name.Contains(q) ||
                    (p.Description != null && p.Description.Contains(q)));
            }

            var items = await query
                .OrderByDescending(p => p.Id)
                .ToListAsync();

            ViewData["q"] = q;
            ViewData["TotalItems"] = items.Count;
            ViewData["SumaCostoTotal"] = items.Sum(p => p.TotalCost);
            ViewData["SumaValorTotalVenta"] = items.Sum(p => p.TotalSaleValue);

            return View(items);
        }

        // GET: /Products/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();
            return View(product);
        }

        // GET: /Products/Create
        public IActionResult Create()
        {
            return View(new Product());
        }

        // POST: /Products/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product model, IFormFile? ImageFile)
        {
            if (!ModelState.IsValid) return View(model);

            // Guardar imagen si subieron archivo
            if (ImageFile is { Length: > 0 })
            {
                var savedPath = await SaveUploadedFile(ImageFile);
                model.ImagePath = savedPath; // relativo a wwwroot
                model.ImageUrl = null;       // preferimos archivo si hay
            }

            _context.Add(model);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: /Products/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();
            return View(product);
        }

        // POST: /Products/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Product model, IFormFile? ImageFile, bool removeImage = false)
        {
            if (id != model.Id) return BadRequest();
            if (!ModelState.IsValid) return View(model);

            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

            product.Name = model.Name;
            product.Description = model.Description;
            product.Cost = model.Cost;
            product.Price = model.Price;
            product.Stock = model.Stock;

            if (removeImage)
            {
                product.ImagePath = null;
                product.ImageUrl = null;
            }

            if (ImageFile is { Length: > 0 })
            {
                var savedPath = await SaveUploadedFile(ImageFile);
                product.ImagePath = savedPath;
                product.ImageUrl = null;
            }
            else if (!string.IsNullOrWhiteSpace(model.ImageUrl))
            {
                product.ImageUrl = model.ImageUrl;
                product.ImagePath = null;
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: /Products/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();
            return View(product);
        }

        // POST: /Products/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private async Task<string> SaveUploadedFile(IFormFile file)
        {
            // Asegura la carpeta /wwwroot/uploads
            var uploadsRoot = Path.Combine(_env.WebRootPath, "uploads");
            if (!Directory.Exists(uploadsRoot))
                Directory.CreateDirectory(uploadsRoot);

            var safeName = Path.GetFileNameWithoutExtension(file.FileName);
            var ext = Path.GetExtension(file.FileName);
            var filename = $"{safeName}-{Guid.NewGuid():N}{ext}";
            var fullPath = Path.Combine(uploadsRoot, filename);

            using (var stream = System.IO.File.Create(fullPath))
            {
                await file.CopyToAsync(stream);
            }

            // Retorna ruta relativa para usar en <img src="/uploads/...">
            return $"/uploads/{filename}";
        }
    }
}
