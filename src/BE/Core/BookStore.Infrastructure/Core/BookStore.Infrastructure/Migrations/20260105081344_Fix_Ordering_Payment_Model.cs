using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookStore.Infrastructure.Core.BookStore.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Fix_Ordering_Payment_Model : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_OrderAddresses_AddressId",
                schema: "ordering",
                table: "Orders");

            migrationBuilder.DropForeignKey(
                name: "FK_StockItems_Books_BookId1",
                table: "StockItems");

            migrationBuilder.DropIndex(
                name: "IX_StockItems_BookId",
                table: "StockItems");

            migrationBuilder.DropIndex(
                name: "IX_StockItems_BookId1",
                table: "StockItems");

            migrationBuilder.DropIndex(
                name: "IX_Orders_AddressId",
                schema: "ordering",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "BookId1",
                table: "StockItems");

            migrationBuilder.DropColumn(
                name: "AddressId",
                schema: "ordering",
                table: "Orders");

            migrationBuilder.AddColumn<Guid>(
                name: "OrderId",
                schema: "ordering",
                table: "OrderAddresses",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_StockItems_BookId",
                table: "StockItems",
                column: "BookId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrderAddresses_OrderId",
                schema: "ordering",
                table: "OrderAddresses",
                column: "OrderId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_OrderAddresses_Orders_OrderId",
                schema: "ordering",
                table: "OrderAddresses",
                column: "OrderId",
                principalSchema: "ordering",
                principalTable: "Orders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderAddresses_Orders_OrderId",
                schema: "ordering",
                table: "OrderAddresses");

            migrationBuilder.DropIndex(
                name: "IX_StockItems_BookId",
                table: "StockItems");

            migrationBuilder.DropIndex(
                name: "IX_OrderAddresses_OrderId",
                schema: "ordering",
                table: "OrderAddresses");

            migrationBuilder.DropColumn(
                name: "OrderId",
                schema: "ordering",
                table: "OrderAddresses");

            migrationBuilder.AddColumn<Guid>(
                name: "BookId1",
                table: "StockItems",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "AddressId",
                schema: "ordering",
                table: "Orders",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_StockItems_BookId",
                table: "StockItems",
                column: "BookId");

            migrationBuilder.CreateIndex(
                name: "IX_StockItems_BookId1",
                table: "StockItems",
                column: "BookId1",
                unique: true,
                filter: "[BookId1] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_AddressId",
                schema: "ordering",
                table: "Orders",
                column: "AddressId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_OrderAddresses_AddressId",
                schema: "ordering",
                table: "Orders",
                column: "AddressId",
                principalSchema: "ordering",
                principalTable: "OrderAddresses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StockItems_Books_BookId1",
                table: "StockItems",
                column: "BookId1",
                principalSchema: "catalog",
                principalTable: "Books",
                principalColumn: "Id");
        }
    }
}
