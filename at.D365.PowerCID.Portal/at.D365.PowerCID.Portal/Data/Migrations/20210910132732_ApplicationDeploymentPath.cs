using Microsoft.EntityFrameworkCore.Migrations;

namespace at.D365.PowerCID.Portal.Data.Migrations
{
    public partial class ApplicationDeploymentPath : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ApplicationDeploymentPath",
                columns: table => new
                {
                    Application = table.Column<int>(type: "int", nullable: false),
                    DeploymentPath = table.Column<int>(type: "int", nullable: false),
                    HierarchieNumber = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationDeploymentPath", x => new { x.Application, x.DeploymentPath });
                    table.ForeignKey(
                        name: "FK_ApplicationDeploymentPath_Application",
                        column: x => x.Application,
                        principalTable: "Application",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ApplicationDeploymentPath_DeploymentPath",
                        column: x => x.DeploymentPath,
                        principalTable: "Deploymentpath",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationDeploymentPath_DeploymentPath",
                table: "ApplicationDeploymentPath",
                column: "DeploymentPath");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApplicationDeploymentPath");
        }
    }
}
