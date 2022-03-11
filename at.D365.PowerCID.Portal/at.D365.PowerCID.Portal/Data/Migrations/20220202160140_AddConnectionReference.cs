using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace at.D365.PowerCID.Portal.Data.Migrations
{
    public partial class AddConnectionReference : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ConnectionReference",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Application = table.Column<int>(type: "int", nullable: false),
                    MsId = table.Column<string>(name: "Ms Id", type: "nvarchar(max)", nullable: false),
                    LogicalName = table.Column<string>(name: "Logical Name", type: "nvarchar(max)", nullable: false),
                    ConnectorId = table.Column<string>(name: "Connector Id", type: "nvarchar(max)", nullable: false),
                    CreatedBy = table.Column<int>(name: "Created By", type: "int", nullable: false),
                    CreatedOn = table.Column<DateTime>(name: "Created On", type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<int>(name: "Modified By", type: "int", nullable: false),
                    ModifiedOn = table.Column<DateTime>(name: "Modified On", type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConnectionReference", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConnectionReference_Application",
                        column: x => x.Application,
                        principalTable: "Application",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ConnectionReference_Created_By",
                        column: x => x.CreatedBy,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ConnectionReference_Modified_By",
                        column: x => x.ModifiedBy,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ConnectionReferenceEnvironment",
                columns: table => new
                {
                    ConnectionReference = table.Column<int>(type: "int", nullable: false),
                    Environment = table.Column<int>(type: "int", nullable: false),
                    ConnectionId = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConnectionReferenceEnvironment", x => new { x.ConnectionReference, x.Environment });
                    table.ForeignKey(
                        name: "FK_ConnectionReferenceEnvironment_ConnectionReference",
                        column: x => x.ConnectionReference,
                        principalTable: "ConnectionReference",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ConnectionReferenceEnvironment_Environment",
                        column: x => x.Environment,
                        principalTable: "Environment",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConnectionReference_Application",
                table: "ConnectionReference",
                column: "Application");

            migrationBuilder.CreateIndex(
                name: "IX_ConnectionReference_Created By",
                table: "ConnectionReference",
                column: "Created By");

            migrationBuilder.CreateIndex(
                name: "IX_ConnectionReference_Modified By",
                table: "ConnectionReference",
                column: "Modified By");

            migrationBuilder.CreateIndex(
                name: "IX_ConnectionReferenceEnvironment_Environment",
                table: "ConnectionReferenceEnvironment",
                column: "Environment");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConnectionReferenceEnvironment");

            migrationBuilder.DropTable(
                name: "ConnectionReference");
        }
    }
}
