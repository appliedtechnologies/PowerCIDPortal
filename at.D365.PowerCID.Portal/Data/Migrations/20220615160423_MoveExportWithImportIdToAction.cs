using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace at.D365.PowerCID.Portal.Data.Migrations
{
    public partial class MoveExportWithImportIdToAction : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AsyncJob_Environment",
                table: "AsyncJob");

            migrationBuilder.DropIndex(
                name: "IX_AsyncJob_ImportTargetEnvironment",
                table: "AsyncJob");

            migrationBuilder.DropColumn(
                name: "ImportTargetEnvironment",
                table: "AsyncJob");

            migrationBuilder.AddColumn<int>(
                name: "Import Target Environment",
                table: "Action",
                type: "int",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Import Target Environment",
                table: "Action");

            migrationBuilder.AddColumn<int>(
                name: "ImportTargetEnvironment",
                table: "AsyncJob",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AsyncJob_ImportTargetEnvironment",
                table: "AsyncJob",
                column: "ImportTargetEnvironment");

            migrationBuilder.AddForeignKey(
                name: "FK_AsyncJob_Environment",
                table: "AsyncJob",
                column: "ImportTargetEnvironment",
                principalTable: "Environment",
                principalColumn: "Id");
        }
    }
}
