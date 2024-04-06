using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fumo.Database.Migrations
{
    /// <inheritdoc />
    public partial class OAuth_Column_Order : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Provider",
                table: "UserOauth",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text")
                .Annotation("Relational:ColumnOrder", 1);

            migrationBuilder.AlterColumn<string>(
                name: "TwitchID",
                table: "UserOauth",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text")
                .Annotation("Relational:ColumnOrder", 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Provider",
                table: "UserOauth",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text")
                .OldAnnotation("Relational:ColumnOrder", 1);

            migrationBuilder.AlterColumn<string>(
                name: "TwitchID",
                table: "UserOauth",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text")
                .OldAnnotation("Relational:ColumnOrder", 0);
        }
    }
}
