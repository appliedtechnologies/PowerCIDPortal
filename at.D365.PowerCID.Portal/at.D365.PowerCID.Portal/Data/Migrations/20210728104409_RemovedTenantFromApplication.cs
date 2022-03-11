using Microsoft.EntityFrameworkCore.Migrations;

namespace at.D365.PowerCID.Portal.Data.Migrations
{
    public partial class RemovedTenantFromApplication : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Application_Tenant",
                table: "Application");

            migrationBuilder.DropIndex(
                name: "IX_Application_Tenant",
                table: "Application");

            migrationBuilder.DropColumn(
                name: "Tenant",
                table: "Application");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Tenant",
                table: "Application",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Application_Tenant",
                table: "Application",
                column: "Tenant");

            migrationBuilder.AddForeignKey(
                name: "FK_Application_Tenant",
                table: "Application",
                column: "Tenant",
                principalTable: "Tenant",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
