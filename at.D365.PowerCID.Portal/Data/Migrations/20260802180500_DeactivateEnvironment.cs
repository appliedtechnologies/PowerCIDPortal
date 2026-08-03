using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using at.D365.PowerCID.Portal.Data.Models;

#nullable disable

namespace at.D365.PowerCID.Portal.Data.Migrations
{
    [DbContext(typeof(atPowerCIDContext))]
    [Migration("20260802180500_DeactivateEnvironment")]
    public partial class DeactivateEnvironment : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDeactive",
                table: "Environment",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsDeactive",
                table: "Environment");
        }
    }
}
