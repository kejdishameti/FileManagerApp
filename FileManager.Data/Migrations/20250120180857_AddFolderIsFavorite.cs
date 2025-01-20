﻿using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FileManagerApp.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFolderIsFavorite : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsFavorite",
                table: "Folders",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsFavorite",
                table: "Folders");
        }
    }
}
