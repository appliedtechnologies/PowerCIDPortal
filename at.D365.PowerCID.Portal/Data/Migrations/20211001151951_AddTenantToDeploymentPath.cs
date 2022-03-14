using Microsoft.EntityFrameworkCore.Migrations;

namespace at.D365.PowerCID.Portal.Data.Migrations
{
    public partial class AddTenantToDeploymentPath : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Tenant",
                table: "Deploymentpath",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Deploymentpath_Tenant",
                table: "Deploymentpath",
                column: "Tenant");

            migrationBuilder.AddForeignKey(
                name: "FK_DeploymentPath_Tenant",
                table: "Deploymentpath",
                column: "Tenant",
                principalTable: "Tenant",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DeploymentPath_Tenant",
                table: "Deploymentpath");

            migrationBuilder.DropIndex(
                name: "IX_Deploymentpath_Tenant",
                table: "Deploymentpath");

            migrationBuilder.DropColumn(
                name: "Tenant",
                table: "Deploymentpath");
        }
    }
}
