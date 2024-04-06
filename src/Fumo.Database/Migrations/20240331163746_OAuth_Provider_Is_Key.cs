using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fumo.Database.Migrations
{
    /// <inheritdoc />
    public partial class OAuth_Provider_Is_Key : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_UserOauth",
                table: "UserOauth");

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserOauth",
                table: "UserOauth",
                columns: new[] { "TwitchID", "Provider" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_UserOauth",
                table: "UserOauth");

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserOauth",
                table: "UserOauth",
                column: "TwitchID");
        }
    }
}
