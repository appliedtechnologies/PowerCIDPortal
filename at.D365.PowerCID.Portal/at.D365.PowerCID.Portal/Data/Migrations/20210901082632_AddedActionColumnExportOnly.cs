using Microsoft.EntityFrameworkCore.Migrations;

namespace at.D365.PowerCID.Portal.Data.Migrations
{
    public partial class AddedActionColumnExportOnly : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ExportOnly",
                table: "Action",
                type: "bit",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExportOnly",
                table: "Action");
        }
    }
}
