using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace at.D365.PowerCID.Portal.Data.Migrations
{
    /// <inheritdoc />
    public partial class VersionScopedDeploymentSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SolutionDeploymentManifest",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SolutionId = table.Column<int>(type: "int", nullable: false),
                    DataverseSolutionId = table.Column<Guid>(name: "Dataverse Solution Id", type: "uniqueidentifier", nullable: false),
                    DataverseVersion = table.Column<string>(name: "Dataverse Version", type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    DataverseModifiedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ManifestHash = table.Column<string>(name: "Manifest Hash", type: "varchar(128)", unicode: false, maxLength: 128, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    LastSyncedOn = table.Column<DateTime>(name: "Last Synced On", type: "datetime2", nullable: true),
                    LastSyncError = table.Column<string>(name: "Last Sync Error", type: "nvarchar(max)", nullable: true),
                    CreatedOn = table.Column<DateTime>(name: "Created On", type: "datetime2", nullable: false),
                    ModifiedOn = table.Column<DateTime>(name: "Modified On", type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(name: "Row Version", type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SolutionDeploymentManifest", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SolutionDeploymentManifest_Solution",
                        column: x => x.SolutionId,
                        principalTable: "Solution",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SolutionDeploymentSetting",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ManifestId = table.Column<int>(type: "int", nullable: false),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    MSId = table.Column<Guid>(name: "MS Id", type: "uniqueidentifier", nullable: false),
                    LogicalName = table.Column<string>(name: "Logical Name", type: "nvarchar(max)", nullable: false),
                    DisplayName = table.Column<string>(name: "Display Name", type: "nvarchar(max)", nullable: false),
                    ConnectorId = table.Column<string>(name: "Connector Id", type: "nvarchar(max)", nullable: true),
                    EnvironmentVariableType = table.Column<string>(name: "Environment Variable Type", type: "nvarchar(max)", nullable: true),
                    DefaultValue = table.Column<string>(name: "Default Value", type: "nvarchar(max)", nullable: true),
                    IsRequired = table.Column<bool>(name: "Is Required", type: "bit", nullable: false),
                    ComponentHash = table.Column<string>(name: "Component Hash", type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SolutionDeploymentSetting", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SolutionDeploymentSetting_Manifest",
                        column: x => x.ManifestId,
                        principalTable: "SolutionDeploymentManifest",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SolutionDeploymentSettingValue",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SettingId = table.Column<int>(type: "int", nullable: false),
                    EnvironmentId = table.Column<int>(name: "Environment Id", type: "int", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsConfigured = table.Column<bool>(name: "Is Configured", type: "bit", nullable: false),
                    IsInherited = table.Column<bool>(name: "Is Inherited", type: "bit", nullable: false),
                    InheritedFromSolutionId = table.Column<int>(name: "Inherited From Solution Id", type: "int", nullable: true),
                    ModifiedBy = table.Column<int>(name: "Modified By", type: "int", nullable: true),
                    ModifiedOn = table.Column<DateTime>(name: "Modified On", type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(name: "Row Version", type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SolutionDeploymentSettingValue", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SolutionDeploymentSettingValue_Environment",
                        column: x => x.EnvironmentId,
                        principalTable: "Environment",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SolutionDeploymentSettingValue_InheritedSolution",
                        column: x => x.InheritedFromSolutionId,
                        principalTable: "Solution",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SolutionDeploymentSettingValue_ModifiedBy",
                        column: x => x.ModifiedBy,
                        principalTable: "User",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SolutionDeploymentSettingValue_Setting",
                        column: x => x.SettingId,
                        principalTable: "SolutionDeploymentSetting",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SolutionDeploymentManifest_SolutionId",
                table: "SolutionDeploymentManifest",
                column: "SolutionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SolutionDeploymentSetting_ManifestId_Kind_MS Id",
                table: "SolutionDeploymentSetting",
                columns: new[] { "ManifestId", "Kind", "MS Id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SolutionDeploymentSettingValue_Environment Id",
                table: "SolutionDeploymentSettingValue",
                column: "Environment Id");

            migrationBuilder.CreateIndex(
                name: "IX_SolutionDeploymentSettingValue_Inherited From Solution Id",
                table: "SolutionDeploymentSettingValue",
                column: "Inherited From Solution Id");

            migrationBuilder.CreateIndex(
                name: "IX_SolutionDeploymentSettingValue_Modified By",
                table: "SolutionDeploymentSettingValue",
                column: "Modified By");

            migrationBuilder.CreateIndex(
                name: "IX_SolutionDeploymentSettingValue_SettingId_Environment Id",
                table: "SolutionDeploymentSettingValue",
                columns: new[] { "SettingId", "Environment Id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SolutionDeploymentSettingValue");

            migrationBuilder.DropTable(
                name: "SolutionDeploymentSetting");

            migrationBuilder.DropTable(
                name: "SolutionDeploymentManifest");
        }
    }
}
