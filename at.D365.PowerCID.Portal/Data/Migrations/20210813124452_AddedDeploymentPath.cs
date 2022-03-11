using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace at.D365.PowerCID.Portal.Data.Migrations
{
    public partial class AddedDeploymentPath : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Deploymentpath",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "varchar(250)", unicode: false, maxLength: 250, nullable: false),
                    CreatedBy = table.Column<int>(name: "Created By", type: "int", nullable: false),
                    CreatedOn = table.Column<DateTime>(name: "Created On", type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<int>(name: "Modified By", type: "int", nullable: false),
                    ModifiedOn = table.Column<DateTime>(name: "Modified On", type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Deploymentpath", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeploymentPath_Created_By",
                        column: x => x.CreatedBy,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DeploymentPath_Modified_By",
                        column: x => x.ModifiedBy,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DeploymentPathEnvironment",
                columns: table => new
                {
                    DeploymentPath = table.Column<int>(type: "int", nullable: false),
                    Environment = table.Column<int>(type: "int", nullable: false),
                    StepNumber = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeploymentPathEnvironment", x => new { x.DeploymentPath, x.Environment });
                    table.ForeignKey(
                        name: "FK_DeploymentPathEnvironment_DeploymentPath",
                        column: x => x.DeploymentPath,
                        principalTable: "Deploymentpath",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DeploymentPathEnvironment_Environment",
                        column: x => x.Environment,
                        principalTable: "Environment",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Deploymentpath_Created By",
                table: "Deploymentpath",
                column: "Created By");

            migrationBuilder.CreateIndex(
                name: "IX_Deploymentpath_Modified By",
                table: "Deploymentpath",
                column: "Modified By");

            migrationBuilder.CreateIndex(
                name: "IX_DeploymentPathEnvironment_Environment",
                table: "DeploymentPathEnvironment",
                column: "Environment");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DeploymentPathEnvironment");

            migrationBuilder.DropTable(
                name: "Deploymentpath");
        }
    }
}
