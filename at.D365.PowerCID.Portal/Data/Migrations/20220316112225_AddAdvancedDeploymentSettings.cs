using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace at.D365.PowerCID.Portal.Data.Migrations
{
    public partial class AddAdvancedDeploymentSettings : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Enable Workflows",
                table: "Solution",
                type: "bit",
                nullable: true,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "Overwrite Unmanaged Customizations",
                table: "Solution",
                type: "bit",
                nullable: true,
                defaultValue: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Enable Workflows",
                table: "Solution");

            migrationBuilder.DropColumn(
                name: "Overwrite Unmanaged Customizations",
                table: "Solution");
        }
    }
}
