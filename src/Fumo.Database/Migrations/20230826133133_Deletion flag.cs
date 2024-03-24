using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fumo.Database.Migrations
{
    /// <inheritdoc />
    public partial class Deletionflag : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "SetForDeletion",
                table: "Channels",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SetForDeletion",
                table: "Channels");
        }
    }
}
