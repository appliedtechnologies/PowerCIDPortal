using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace at.D365.PowerCID.Portal.Data.Migrations
{
    public partial class ApplicationForceManagedDeployment : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ForceManagedDeployment",
                table: "Application",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ForceManagedDeployment",
                table: "Application");
        }
    }
}
