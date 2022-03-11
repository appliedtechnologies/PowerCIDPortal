using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace at.D365.PowerCID.Portal.Data.Migrations
{
    public partial class AddAsyncJob : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AsyncOperationId",
                table: "Action");

            migrationBuilder.DropColumn(
                name: "JobId",
                table: "Action");

            migrationBuilder.CreateTable(
                name: "AsyncJob",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AsyncOperationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    JobId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsManaged = table.Column<bool>(type: "bit", nullable: false),
                    Action = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AsyncJob", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AsyncJob_Action",
                        column: x => x.Action,
                        principalTable: "Action",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AsyncJob_Action",
                table: "AsyncJob",
                column: "Action");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AsyncJob");

            migrationBuilder.AddColumn<Guid>(
                name: "AsyncOperationId",
                table: "Action",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "JobId",
                table: "Action",
                type: "uniqueidentifier",
                nullable: true);
        }
    }
}
