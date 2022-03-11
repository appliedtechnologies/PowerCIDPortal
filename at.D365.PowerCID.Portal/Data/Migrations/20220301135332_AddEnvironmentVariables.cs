using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace at.D365.PowerCID.Portal.Data.Migrations
{
    public partial class AddEnvironmentVariables : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EnvironmentVariable",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Application = table.Column<int>(type: "int", nullable: false),
                    MsId = table.Column<Guid>(name: "Ms Id", type: "uniqueidentifier", nullable: false),
                    LogicalName = table.Column<string>(name: "Logical Name", type: "nvarchar(max)", nullable: false),
                    DisplayName = table.Column<string>(name: "Display Name", type: "nvarchar(max)", nullable: false),
                    CreatedBy = table.Column<int>(name: "Created By", type: "int", nullable: false),
                    CreatedOn = table.Column<DateTime>(name: "Created On", type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<int>(name: "Modified By", type: "int", nullable: false),
                    ModifiedOn = table.Column<DateTime>(name: "Modified On", type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EnvironmentVariable", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EnvironmentVariable_Application_Application",
                        column: x => x.Application,
                        principalTable: "Application",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_EnvironmentVariable_User_Created By",
                        column: x => x.CreatedBy,
                        principalTable: "User",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_EnvironmentVariable_User_Modified By",
                        column: x => x.ModifiedBy,
                        principalTable: "User",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "EnvironmentVariableEnvironment",
                columns: table => new
                {
                    EnvironmentVariable = table.Column<int>(type: "int", nullable: false),
                    Environment = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EnvironmentVariableEnvironment", x => new { x.EnvironmentVariable, x.Environment });
                    table.ForeignKey(
                        name: "FK_EnvironmentVariableEnvironment_Environment_Environment",
                        column: x => x.Environment,
                        principalTable: "Environment",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EnvironmentVariableEnvironment_EnvironmentVariable_EnvironmentVariable",
                        column: x => x.EnvironmentVariable,
                        principalTable: "EnvironmentVariable",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EnvironmentVariable_Application",
                table: "EnvironmentVariable",
                column: "Application");

            migrationBuilder.CreateIndex(
                name: "IX_EnvironmentVariable_Created By",
                table: "EnvironmentVariable",
                column: "Created By");

            migrationBuilder.CreateIndex(
                name: "IX_EnvironmentVariable_Modified By",
                table: "EnvironmentVariable",
                column: "Modified By");

            migrationBuilder.CreateIndex(
                name: "IX_EnvironmentVariableEnvironment_Environment",
                table: "EnvironmentVariableEnvironment",
                column: "Environment");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EnvironmentVariableEnvironment");

            migrationBuilder.DropTable(
                name: "EnvironmentVariable");
        }
    }
}
