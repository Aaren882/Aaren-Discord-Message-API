using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Arma3WebService.Migrations.SQLite.Migrations
{
    /// <inheritdoc />
    public partial class JsonMessageTemplete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "messageOfflinePath",
                table: "ServerInfoList");

            migrationBuilder.DropColumn(
                name: "messageTemplatePath",
                table: "ServerInfoList");

            migrationBuilder.AddColumn<string>(
                name: "MessageOffline",
                table: "ServerInfoList",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "messageTemplate",
                table: "ServerInfoList",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MessageOffline",
                table: "ServerInfoList");

            migrationBuilder.DropColumn(
                name: "messageTemplate",
                table: "ServerInfoList");

            migrationBuilder.AddColumn<string>(
                name: "messageOfflinePath",
                table: "ServerInfoList",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "messageTemplatePath",
                table: "ServerInfoList",
                type: "TEXT",
                nullable: true);
        }
    }
}
