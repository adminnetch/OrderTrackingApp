using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrderTrackingApp.Migrations
{
    public partial class nometroupeeorari : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Nome",
                table: "TroupeOrari",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Nome",
                table: "TroupeOrari");
        }
    }
}
