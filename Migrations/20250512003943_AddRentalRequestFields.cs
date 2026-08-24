using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrderTrackingApp.Migrations
{
    public partial class AddRentalRequestFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RentalItem_Category_CategoryId",
                table: "RentalItem");

            migrationBuilder.DropForeignKey(
                name: "FK_RentalRequest_CinemaOrders_CinemaOrderId",
                table: "RentalRequest");

            migrationBuilder.DropForeignKey(
                name: "FK_RentalRequestItem_RentalItem_RentalItemId",
                table: "RentalRequestItem");

            migrationBuilder.DropForeignKey(
                name: "FK_RentalRequestItem_RentalRequest_RentalRequestId",
                table: "RentalRequestItem");

            migrationBuilder.DropPrimaryKey(
                name: "PK_RentalRequestItem",
                table: "RentalRequestItem");

            migrationBuilder.DropPrimaryKey(
                name: "PK_RentalRequest",
                table: "RentalRequest");

            migrationBuilder.DropPrimaryKey(
                name: "PK_RentalItem",
                table: "RentalItem");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Category",
                table: "Category");

            migrationBuilder.RenameTable(
                name: "RentalRequestItem",
                newName: "RentalRequestItems");

            migrationBuilder.RenameTable(
                name: "RentalRequest",
                newName: "RentalRequests");

            migrationBuilder.RenameTable(
                name: "RentalItem",
                newName: "RentalItems");

            migrationBuilder.RenameTable(
                name: "Category",
                newName: "Categories");

            migrationBuilder.RenameIndex(
                name: "IX_RentalRequestItem_RentalRequestId",
                table: "RentalRequestItems",
                newName: "IX_RentalRequestItems_RentalRequestId");

            migrationBuilder.RenameIndex(
                name: "IX_RentalRequestItem_RentalItemId",
                table: "RentalRequestItems",
                newName: "IX_RentalRequestItems_RentalItemId");

            migrationBuilder.RenameIndex(
                name: "IX_RentalRequest_CinemaOrderId",
                table: "RentalRequests",
                newName: "IX_RentalRequests_CinemaOrderId");

            migrationBuilder.RenameIndex(
                name: "IX_RentalItem_CategoryId",
                table: "RentalItems",
                newName: "IX_RentalItems_CategoryId");

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "RentalRequests",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddPrimaryKey(
                name: "PK_RentalRequestItems",
                table: "RentalRequestItems",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_RentalRequests",
                table: "RentalRequests",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_RentalItems",
                table: "RentalItems",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Categories",
                table: "Categories",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_RentalItems_Categories_CategoryId",
                table: "RentalItems",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RentalRequestItems_RentalItems_RentalItemId",
                table: "RentalRequestItems",
                column: "RentalItemId",
                principalTable: "RentalItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RentalRequestItems_RentalRequests_RentalRequestId",
                table: "RentalRequestItems",
                column: "RentalRequestId",
                principalTable: "RentalRequests",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RentalRequests_CinemaOrders_CinemaOrderId",
                table: "RentalRequests",
                column: "CinemaOrderId",
                principalTable: "CinemaOrders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RentalItems_Categories_CategoryId",
                table: "RentalItems");

            migrationBuilder.DropForeignKey(
                name: "FK_RentalRequestItems_RentalItems_RentalItemId",
                table: "RentalRequestItems");

            migrationBuilder.DropForeignKey(
                name: "FK_RentalRequestItems_RentalRequests_RentalRequestId",
                table: "RentalRequestItems");

            migrationBuilder.DropForeignKey(
                name: "FK_RentalRequests_CinemaOrders_CinemaOrderId",
                table: "RentalRequests");

            migrationBuilder.DropPrimaryKey(
                name: "PK_RentalRequests",
                table: "RentalRequests");

            migrationBuilder.DropPrimaryKey(
                name: "PK_RentalRequestItems",
                table: "RentalRequestItems");

            migrationBuilder.DropPrimaryKey(
                name: "PK_RentalItems",
                table: "RentalItems");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Categories",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "RentalRequests");

            migrationBuilder.RenameTable(
                name: "RentalRequests",
                newName: "RentalRequest");

            migrationBuilder.RenameTable(
                name: "RentalRequestItems",
                newName: "RentalRequestItem");

            migrationBuilder.RenameTable(
                name: "RentalItems",
                newName: "RentalItem");

            migrationBuilder.RenameTable(
                name: "Categories",
                newName: "Category");

            migrationBuilder.RenameIndex(
                name: "IX_RentalRequests_CinemaOrderId",
                table: "RentalRequest",
                newName: "IX_RentalRequest_CinemaOrderId");

            migrationBuilder.RenameIndex(
                name: "IX_RentalRequestItems_RentalRequestId",
                table: "RentalRequestItem",
                newName: "IX_RentalRequestItem_RentalRequestId");

            migrationBuilder.RenameIndex(
                name: "IX_RentalRequestItems_RentalItemId",
                table: "RentalRequestItem",
                newName: "IX_RentalRequestItem_RentalItemId");

            migrationBuilder.RenameIndex(
                name: "IX_RentalItems_CategoryId",
                table: "RentalItem",
                newName: "IX_RentalItem_CategoryId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_RentalRequest",
                table: "RentalRequest",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_RentalRequestItem",
                table: "RentalRequestItem",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_RentalItem",
                table: "RentalItem",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Category",
                table: "Category",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_RentalItem_Category_CategoryId",
                table: "RentalItem",
                column: "CategoryId",
                principalTable: "Category",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RentalRequest_CinemaOrders_CinemaOrderId",
                table: "RentalRequest",
                column: "CinemaOrderId",
                principalTable: "CinemaOrders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RentalRequestItem_RentalItem_RentalItemId",
                table: "RentalRequestItem",
                column: "RentalItemId",
                principalTable: "RentalItem",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RentalRequestItem_RentalRequest_RentalRequestId",
                table: "RentalRequestItem",
                column: "RentalRequestId",
                principalTable: "RentalRequest",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
