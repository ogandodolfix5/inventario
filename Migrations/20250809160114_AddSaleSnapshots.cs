using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventarioMvc.Migrations
{
    /// <inheritdoc />
    public partial class AddSaleSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CostAtSale",
                table: "Sales",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProductNameSnapshot",
                table: "Sales",
                type: "TEXT",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CostAtSale",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "ProductNameSnapshot",
                table: "Sales");
        }
    }
}
