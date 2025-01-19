using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FileManagerApp.Data.Migrations
{
    /// <inheritdoc />
    public partial class ConvertToTagSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Metadata",
                table: "Folders");

            migrationBuilder.DropColumn(
                name: "Metadata",
                table: "Files");

            migrationBuilder.AddColumn<string>(
                name: "Tags",
                table: "Folders",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'[]'::jsonb");

            migrationBuilder.AddColumn<string>(
                name: "Tags",
                table: "Files",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'[]'::jsonb");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Tags",
                table: "Folders");

            migrationBuilder.DropColumn(
                name: "Tags",
                table: "Files");

            migrationBuilder.AddColumn<string>(
                name: "Metadata",
                table: "Folders",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'{}'::jsonb");

            migrationBuilder.AddColumn<string>(
                name: "Metadata",
                table: "Files",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'{}'::jsonb");
        }
    }
}
