using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace at.D365.PowerCID.Portal.Data.Migrations
{
    public partial class ChangedNameDeployUnmanaged : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "DeployUnmanged",
                table: "Environment",
                newName: "DeployUnmanaged");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "DeployUnmanaged",
                table: "Environment",
                newName: "DeployUnmanged");
        }
    }
}
