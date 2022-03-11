using Microsoft.EntityFrameworkCore.Migrations;

namespace at.D365.PowerCID.Portal.Data.Migrations
{
    public partial class AddedAsyncJobColumnImportTargetEnvironment : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
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
        }
    }
}
