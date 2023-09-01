using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fumo.Database.Migrations
{
    /// <inheritdoc />
    public partial class add_permission : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<List<string>>(
                name: "Permissions",
                table: "Users",
                type: "text[]",
                nullable: false,
                defaultValueSql: "'{\"default\"}'::text[]");

            migrationBuilder.AlterColumn<bool>(
                name: "SetForDeletion",
                table: "Channels",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "boolean");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Permissions",
                table: "Users");

            migrationBuilder.AlterColumn<bool>(
                name: "SetForDeletion",
                table: "Channels",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: false);
        }
    }
}
