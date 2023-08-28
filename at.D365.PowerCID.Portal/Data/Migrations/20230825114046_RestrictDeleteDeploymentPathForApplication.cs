using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace at.D365.PowerCID.Portal.Data.Migrations
{
    public partial class RestrictDeleteDeploymentPathForApplication : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ApplicationDeploymentPath_DeploymentPath",
                table: "ApplicationDeploymentPath");

            migrationBuilder.AddForeignKey(
                name: "FK_ApplicationDeploymentPath_DeploymentPath",
                table: "ApplicationDeploymentPath",
                column: "DeploymentPath",
                principalTable: "Deploymentpath",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ApplicationDeploymentPath_DeploymentPath",
                table: "ApplicationDeploymentPath");

            migrationBuilder.AddForeignKey(
                name: "FK_ApplicationDeploymentPath_DeploymentPath",
                table: "ApplicationDeploymentPath",
                column: "DeploymentPath",
                principalTable: "Deploymentpath",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
