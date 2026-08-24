using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrderTrackingApp.Migrations
{
    public partial class AddUpdatedByToCinemaOrder : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "CinemaOrders",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "CinemaOrders");
        }
    }
}
