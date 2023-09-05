using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fumo.Database.Migrations
{
    /// <inheritdoc />
    public partial class CommandLogsDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "Date",
                table: "CommandExecutionLogs",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "now()");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Date",
                table: "CommandExecutionLogs");
        }
    }
}
