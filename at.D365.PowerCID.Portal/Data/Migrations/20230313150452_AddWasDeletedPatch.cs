using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace at.D365.PowerCID.Portal.Data.Migrations
{
    public partial class AddWasDeletedPatch : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "WasDeleted",
                table: "Solution",
                type: "bit",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WasDeleted",
                table: "Solution");
        }
    }
}
