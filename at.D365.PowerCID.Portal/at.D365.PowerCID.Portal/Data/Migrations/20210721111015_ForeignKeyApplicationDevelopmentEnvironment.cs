using Microsoft.EntityFrameworkCore.Migrations;

namespace at.D365.PowerCID.Portal.Data.Migrations
{
    public partial class ForeignKeyApplicationDevelopmentEnvironment : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Application_Development Environment",
                table: "Application",
                column: "Development Environment");

            migrationBuilder.AddForeignKey(
                name: "FK_Application_Development_Environment",
                table: "Application",
                column: "Development Environment",
                principalTable: "Environment",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Application_Development_Environment",
                table: "Application");

            migrationBuilder.DropIndex(
                name: "IX_Application_Development Environment",
                table: "Application");
        }
    }
}
