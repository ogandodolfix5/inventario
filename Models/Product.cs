using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InventarioMvc.Models
{
    public class Product
    {
        public int Id { get; set; }

        [Required, StringLength(120)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Range(0, 999999999999.99)]
        public decimal Cost { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Range(0, 999999999999.99)]
        public decimal Price { get; set; }

        [Range(0, int.MaxValue)]
        public int Stock { get; set; }

        // Puedes usar uno u otro (URL o archivo subido)
        public string? ImageUrl { get; set; }
        public string? ImagePath { get; set; } // para archivos guardados en wwwroot/uploads

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [NotMapped] public decimal ProfitPerUnit => Price - Cost;
        [NotMapped] public decimal TotalCost => Cost * Stock;
        [NotMapped] public decimal TotalSaleValue => Price * Stock;
    }
}
