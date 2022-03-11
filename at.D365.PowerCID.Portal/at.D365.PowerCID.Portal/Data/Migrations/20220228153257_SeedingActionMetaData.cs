using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace at.D365.PowerCID.Portal.Data.Migrations
{
    public partial class SeedingActionMetaData : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "ActionResult",
                columns: new[] { "Id", "Result" },
                values: new object[,]
                {
                    { 1, "success" },
                    { 2, "failure" }
                });

            migrationBuilder.InsertData(
                table: "ActionStatus",
                columns: new[] { "Id", "Status" },
                values: new object[,]
                {
                    { 1, "queued" },
                    { 2, "in progress" },
                    { 3, "completed" },
                    { 4, "applying upgrade" }
                });

            migrationBuilder.InsertData(
                table: "ActionType",
                columns: new[] { "Id", "Type" },
                values: new object[,]
                {
                    { 1, "export" },
                    { 2, "import" }
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "ActionResult",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "ActionResult",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "ActionStatus",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "ActionStatus",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "ActionStatus",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "ActionStatus",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "ActionType",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "ActionType",
                keyColumn: "Id",
                keyValue: 2);
        }
    }
}
