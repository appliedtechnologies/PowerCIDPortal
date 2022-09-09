using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace at.D365.PowerCID.Portal.Data.Migrations
{
    public partial class AddEnableFlowActionType : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "ActionType",
                keyColumn: "Id",
                keyValue: 3,
                column: "Type",
                value: "apply upgrade");

            migrationBuilder.InsertData(
                table: "ActionType",
                columns: new[] { "Id", "Type" },
                values: new object[] { 4, "enable flows" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "ActionType",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.UpdateData(
                table: "ActionType",
                keyColumn: "Id",
                keyValue: 3,
                column: "Type",
                value: "applying upgrade");
        }
    }
}
