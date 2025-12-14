using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookStore.Infrastructure.Core.BookStore.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixBookEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ImageUrl",
                schema: "catalog",
                table: "BookImages",
                newName: "ObjectName");

            migrationBuilder.RenameColumn(
                name: "FileUrl",
                schema: "catalog",
                table: "BookFiles",
                newName: "Url");

            migrationBuilder.AddColumn<string>(
                name: "CoverImageUrl",
                schema: "catalog",
                table: "Books",
                type: "varchar(1000)",
                unicode: false,
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContentType",
                schema: "catalog",
                table: "BookImages",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<long>(
                name: "Size",
                schema: "catalog",
                table: "BookImages",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "Url",
                schema: "catalog",
                table: "BookImages",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ObjectName",
                schema: "catalog",
                table: "BookFiles",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CoverImageUrl",
                schema: "catalog",
                table: "Books");

            migrationBuilder.DropColumn(
                name: "ContentType",
                schema: "catalog",
                table: "BookImages");

            migrationBuilder.DropColumn(
                name: "Size",
                schema: "catalog",
                table: "BookImages");

            migrationBuilder.DropColumn(
                name: "Url",
                schema: "catalog",
                table: "BookImages");

            migrationBuilder.DropColumn(
                name: "ObjectName",
                schema: "catalog",
                table: "BookFiles");

            migrationBuilder.RenameColumn(
                name: "ObjectName",
                schema: "catalog",
                table: "BookImages",
                newName: "ImageUrl");

            migrationBuilder.RenameColumn(
                name: "Url",
                schema: "catalog",
                table: "BookFiles",
                newName: "FileUrl");
        }
    }
}
