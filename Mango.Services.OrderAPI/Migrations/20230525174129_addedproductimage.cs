using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mango.Services.OrderAPI.Migrations
{
    /// <inheritdoc />
    public partial class addedproductimage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProductImage",
                table: "OrderDetails",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProductImage",
                table: "OrderDetails");
        }
    }
}
