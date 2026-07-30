using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Arma3WebService.Migrations.NpgSQL.Models
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
                    managementType = table.Column<int>(type: "integer", nullable: false),
                    messageId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InternalManagement", x => x.managementType);
                });

            migrationBuilder.CreateTable(
                name: "ServerIdentities",
                columns: table => new
                {
                    profileName = table.Column<string>(type: "text", nullable: false),
                    messageId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    profileStateStamp = table.Column<long>(type: "bigint", nullable: false),
                    modListMessageId = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    lastUpdate = table.Column<long>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServerIdentities", x => x.profileName);
                });

            migrationBuilder.CreateTable(
                name: "ServerInfoList",
                columns: table => new
                {
                    messageId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    messageTemplatePath = table.Column<string>(type: "text", nullable: true),
                    messageOfflinePath = table.Column<string>(type: "text", nullable: true),
                    messageActionPath = table.Column<string>(type: "text", nullable: true),
                    lastUpdate = table.Column<long>(type: "timestamp with time zone", nullable: false)
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
