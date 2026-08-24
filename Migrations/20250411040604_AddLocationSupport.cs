using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrderTrackingApp.Migrations
{
    public partial class AddLocationSupport : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CinemaOrderId",
                table: "Locations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Locations_CinemaOrderId",
                table: "Locations",
                column: "CinemaOrderId");

            migrationBuilder.AddForeignKey(
                name: "FK_Locations_CinemaOrders_CinemaOrderId",
                table: "Locations",
                column: "CinemaOrderId",
                principalTable: "CinemaOrders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Locations_CinemaOrders_CinemaOrderId",
                table: "Locations");

            migrationBuilder.DropIndex(
                name: "IX_Locations_CinemaOrderId",
                table: "Locations");

            migrationBuilder.DropColumn(
                name: "CinemaOrderId",
                table: "Locations");
        }
    }
}
