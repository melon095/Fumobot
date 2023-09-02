using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fumo.Database.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    TwitchID = table.Column<string>(type: "text", nullable: false),
                    TwitchName = table.Column<string>(type: "text", nullable: false),
                    UsernameHistory = table.Column<string[]>(type: "text[]", nullable: false, defaultValueSql: "'{}'::text[]"),
                    DateSeen = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    Settings = table.Column<Setting[]>(type: "jsonb", nullable: false, defaultValueSql: "'[]'::jsonb")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.TwitchID);
                });

            migrationBuilder.CreateTable(
                name: "Channels",
                columns: table => new
                {
                    TwitchID = table.Column<string>(type: "text", nullable: false),
                    TwitchName = table.Column<string>(type: "text", nullable: false),
                    DateJoined = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    UserTwitchID = table.Column<string>(type: "text", nullable: false),
                    Settings = table.Column<Setting[]>(type: "jsonb", nullable: false, defaultValueSql: "'[]'::jsonb")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Channels", x => x.TwitchID);
                    table.ForeignKey(
                        name: "FK_Channels_Users_UserTwitchID",
                        column: x => x.UserTwitchID,
                        principalTable: "Users",
                        principalColumn: "TwitchID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Channels_TwitchID",
                table: "Channels",
                column: "TwitchID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Channels_UserTwitchID",
                table: "Channels",
                column: "UserTwitchID",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Channels");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
