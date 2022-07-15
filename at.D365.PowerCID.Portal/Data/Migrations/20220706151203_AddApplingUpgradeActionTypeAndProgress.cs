using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace at.D365.PowerCID.Portal.Data.Migrations
{
    public partial class AddApplingUpgradeActionTypeAndProgress : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "ActionStatus",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.AddColumn<double>(
                name: "Progress",
                table: "Action",
                type: "float",
                nullable: true);

            migrationBuilder.InsertData(
                table: "ActionType",
                columns: new[] { "Id", "Type" },
                values: new object[] { 3, "applying upgrade" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "ActionType",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DropColumn(
                name: "Progress",
                table: "Action");

            migrationBuilder.InsertData(
                table: "ActionStatus",
                columns: new[] { "Id", "Status" },
                values: new object[] { 4, "applying upgrade" });
        }
    }
}
