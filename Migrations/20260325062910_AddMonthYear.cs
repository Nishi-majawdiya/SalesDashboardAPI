using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SalesDashboardAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddMonthYear : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Month",
                table: "Sales",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Year",
                table: "Sales",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Month",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "Year",
                table: "Sales");
        }
    }
}
