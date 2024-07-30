using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace at.D365.PowerCID.Portal.Data.Migrations
{
    public partial class AddUserIsDeactive : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDeactive",
                table: "User",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsDeactive",
                table: "User");
        }
    }
}
