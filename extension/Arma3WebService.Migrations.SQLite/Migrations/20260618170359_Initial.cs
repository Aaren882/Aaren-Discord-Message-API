using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Arma3WebService.Migrations.SQLite.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InternalManagement",
                columns: table => new
                {
                    managementType = table.Column<int>(type: "INTEGER", nullable: false),
                    messageId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    description = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InternalManagement", x => x.managementType);
                });

            migrationBuilder.CreateTable(
                name: "ServerIdentities",
                columns: table => new
                {
                    profileName = table.Column<string>(type: "TEXT", nullable: false),
                    messageId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    profileStateStamp = table.Column<long>(type: "INTEGER", nullable: false),
                    modListMessageId = table.Column<ulong>(type: "INTEGER", nullable: true),
                    lastUpdate = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServerIdentities", x => x.profileName);
                });

            migrationBuilder.CreateTable(
                name: "ServerInfoList",
                columns: table => new
                {
                    messageId = table.Column<ulong>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    messageTemplatePath = table.Column<string>(type: "TEXT", nullable: true),
                    messageOfflinePath = table.Column<string>(type: "TEXT", nullable: true),
                    messageActionPath = table.Column<string>(type: "TEXT", nullable: true),
                    lastUpdate = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServerInfoList", x => x.messageId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InternalManagement");

            migrationBuilder.DropTable(
                name: "ServerIdentities");

            migrationBuilder.DropTable(
                name: "ServerInfoList");
        }
    }
}
