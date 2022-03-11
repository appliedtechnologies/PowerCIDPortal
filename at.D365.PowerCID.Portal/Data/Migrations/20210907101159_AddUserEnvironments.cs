using Microsoft.EntityFrameworkCore.Migrations;

namespace at.D365.PowerCID.Portal.Data.Migrations
{
    public partial class AddUserEnvironments : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserEnvironment",
                columns: table => new
                {
                    User = table.Column<int>(type: "int", nullable: false),
                    Environment = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserEnvironment", x => new { x.User, x.Environment });
                    table.ForeignKey(
                        name: "FK_UserEnvironment_Environment",
                        column: x => x.Environment,
                        principalTable: "Environment",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserEnvironment_User",
                        column: x => x.User,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserEnvironment_Environment",
                table: "UserEnvironment",
                column: "Environment");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserEnvironment");
        }
    }
}
