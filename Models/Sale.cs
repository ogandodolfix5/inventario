using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InventarioMvc.Models
{
    public class Sale
    {
        public int Id { get; set; }

        [Required]
        public int ProductId { get; set; }
        public Product? Product { get; set; }

        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Range(0, 999999999999.99)]
        public decimal UnitPrice { get; set; }

        // NUEVO: costo del producto al momento de la venta (congelado)
        [Column(TypeName = "decimal(18,2)")]
        public decimal? CostAtSale { get; set; }

        // NUEVO: nombre del producto al momento de la venta (congelado)
        [StringLength(200)]
        public string? ProductNameSnapshot { get; set; }

        [NotMapped]
        public decimal Total => UnitPrice * Quantity;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
