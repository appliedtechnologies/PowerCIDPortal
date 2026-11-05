using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace at.D365.PowerCID.Portal.Data.Migrations
{
    /// <inheritdoc />
    public partial class DeploymentSettingActionSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DeploymentSettingSnapshot",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ActionId = table.Column<int>(name: "Action Id", type: "int", nullable: false),
                    SolutionId = table.Column<int>(name: "Solution Id", type: "int", nullable: false),
                    EnvironmentId = table.Column<int>(name: "Environment Id", type: "int", nullable: false),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    MSId = table.Column<Guid>(name: "MS Id", type: "uniqueidentifier", nullable: false),
                    LogicalName = table.Column<string>(name: "Logical Name", type: "nvarchar(max)", nullable: false),
                    DisplayName = table.Column<string>(name: "Display Name", type: "nvarchar(max)", nullable: false),
                    ConnectorId = table.Column<string>(name: "Connector Id", type: "nvarchar(max)", nullable: true),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsConfigured = table.Column<bool>(name: "Is Configured", type: "bit", nullable: false),
                    CreatedOn = table.Column<DateTime>(name: "Created On", type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeploymentSettingSnapshot", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeploymentSettingSnapshot_Action",
                        column: x => x.ActionId,
                        principalTable: "Action",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DeploymentSettingSnapshot_Environment",
                        column: x => x.EnvironmentId,
                        principalTable: "Environment",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DeploymentSettingSnapshot_Solution",
                        column: x => x.SolutionId,
                        principalTable: "Solution",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_DeploymentSettingSnapshot_Action Id_Kind_MS Id",
                table: "DeploymentSettingSnapshot",
                columns: new[] { "Action Id", "Kind", "MS Id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DeploymentSettingSnapshot_Environment Id",
                table: "DeploymentSettingSnapshot",
                column: "Environment Id");

            migrationBuilder.CreateIndex(
                name: "IX_DeploymentSettingSnapshot_Solution Id",
                table: "DeploymentSettingSnapshot",
                column: "Solution Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DeploymentSettingSnapshot");
        }
    }
}
