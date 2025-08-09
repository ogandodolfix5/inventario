using InventarioMvc.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InventarioMvc.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;
        public HomeController(AppDbContext context) => _context = context;

        public async Task<IActionResult> Index()
        {
            var products = await _context.Products.ToListAsync();

            var invCosto = products.Sum(p => p.TotalCost);
            var invVenta = products.Sum(p => p.TotalSaleValue);
            var invUtilidadPotencial = invVenta - invCosto;
            var totalItems = products.Count;
            var totalStock = products.Sum(p => p.Stock);

            var now = DateTime.UtcNow;
            var todayStart = DateTime.UtcNow.Date;
            var day7 = now.AddDays(-7);

            var sales = _context.Sales.Include(s => s.Product);

            var ventasHoy = await sales.Where(s => s.CreatedAt >= todayStart).ToListAsync();
            var ventas7 = await sales.Where(s => s.CreatedAt >= day7).ToListAsync();

            decimal totalHoy = ventasHoy.Sum(s => s.Total);
            decimal total7 = ventas7.Sum(s => s.Total);

            // Utilidad realizada aprox (precio - costo estimado)
            decimal utilidad7 = ventas7.Sum(s => (s.UnitPrice - (s.Product != null ? s.Product.Cost : 0m)) * s.Quantity);

            var ultimasVentas = await sales
                .OrderByDescending(s => s.CreatedAt)
                .Take(10)
                .ToListAsync();

            ViewData["InvCosto"] = invCosto;
            ViewData["InvVenta"] = invVenta;
            ViewData["InvUtilidad"] = invUtilidadPotencial;
            ViewData["TotalItems"] = totalItems;
            ViewData["TotalStock"] = totalStock;
            ViewData["TotalHoy"] = totalHoy;
            ViewData["Total7"] = total7;
            ViewData["Utilidad7"] = utilidad7;
            ViewData["Ultimas"] = ultimasVentas;

            return View();
        }
    }
}
