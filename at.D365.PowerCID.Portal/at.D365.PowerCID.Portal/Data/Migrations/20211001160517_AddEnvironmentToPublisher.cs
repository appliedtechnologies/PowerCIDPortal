using Microsoft.EntityFrameworkCore.Migrations;

namespace at.D365.PowerCID.Portal.Data.Migrations
{
    public partial class AddEnvironmentToPublisher : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Environment",
                table: "Publisher",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Publisher_Environment",
                table: "Publisher",
                column: "Environment");

            migrationBuilder.AddForeignKey(
                name: "FK_Publisher_Environment_Environment",
                table: "Publisher",
                column: "Environment",
                principalTable: "Environment",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Publisher_Environment_Environment",
                table: "Publisher");

            migrationBuilder.DropIndex(
                name: "IX_Publisher_Environment",
                table: "Publisher");

            migrationBuilder.DropColumn(
                name: "Environment",
                table: "Publisher");
        }
    }
}
