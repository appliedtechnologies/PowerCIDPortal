using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace at.D365.PowerCID.Portal.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCrossTenantDelivery : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsReleasedExternally",
                table: "Solution",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsExternalDelivery",
                table: "Action",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "ExternalDeploymentPath",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "varchar(250)", unicode: false, maxLength: 250, nullable: false),
                    Tenant = table.Column<int>(type: "int", nullable: false),
                    CreatedBy = table.Column<int>(name: "Created By", type: "int", nullable: false),
                    CreatedOn = table.Column<DateTime>(name: "Created On", type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<int>(name: "Modified By", type: "int", nullable: false),
                    ModifiedOn = table.Column<DateTime>(name: "Modified On", type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExternalDeploymentPath", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExternalDeploymentPath_Created_By",
                        column: x => x.CreatedBy,
                        principalTable: "User",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ExternalDeploymentPath_Modified_By",
                        column: x => x.ModifiedBy,
                        principalTable: "User",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ExternalDeploymentPath_Tenant",
                        column: x => x.Tenant,
                        principalTable: "Tenant",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ExternalEnvironment",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Environment = table.Column<int>(type: "int", nullable: false),
                    Alias = table.Column<string>(type: "varchar(250)", unicode: false, maxLength: 250, nullable: true),
                    IsDeactive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<int>(name: "Created By", type: "int", nullable: false),
                    CreatedOn = table.Column<DateTime>(name: "Created On", type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<int>(name: "Modified By", type: "int", nullable: false),
                    ModifiedOn = table.Column<DateTime>(name: "Modified On", type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExternalEnvironment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExternalEnvironment_Created_By",
                        column: x => x.CreatedBy,
                        principalTable: "User",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ExternalEnvironment_Environment",
                        column: x => x.Environment,
                        principalTable: "Environment",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ExternalEnvironment_Modified_By",
                        column: x => x.ModifiedBy,
                        principalTable: "User",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "UserExternalTenant",
                columns: table => new
                {
                    User = table.Column<int>(type: "int", nullable: false),
                    Tenant = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserExternalTenant", x => new { x.User, x.Tenant });
                    table.ForeignKey(
                        name: "FK_UserExternalTenant_Tenant",
                        column: x => x.Tenant,
                        principalTable: "Tenant",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserExternalTenant_User",
                        column: x => x.User,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ApplicationExternalDeploymentPath",
                columns: table => new
                {
                    Application = table.Column<int>(type: "int", nullable: false),
                    ExternalDeploymentPath = table.Column<int>(type: "int", nullable: false),
                    HierarchieNumber = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationExternalDeploymentPath", x => new { x.Application, x.ExternalDeploymentPath });
                    table.ForeignKey(
                        name: "FK_ApplicationExternalDeploymentPath_Application",
                        column: x => x.Application,
                        principalTable: "Application",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ApplicationExternalDeploymentPath_ExternalDeploymentPath",
                        column: x => x.ExternalDeploymentPath,
                        principalTable: "ExternalDeploymentPath",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ExternalDeploymentPathEnvironment",
                columns: table => new
                {
                    ExternalDeploymentPath = table.Column<int>(type: "int", nullable: false),
                    ExternalEnvironment = table.Column<int>(type: "int", nullable: false),
                    StepNumber = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExternalDeploymentPathEnvironment", x => new { x.ExternalDeploymentPath, x.ExternalEnvironment });
                    table.ForeignKey(
                        name: "FK_ExternalDeploymentPathEnvironment_ExternalDeploymentPath",
                        column: x => x.ExternalDeploymentPath,
                        principalTable: "ExternalDeploymentPath",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExternalDeploymentPathEnvironment_ExternalEnvironment",
                        column: x => x.ExternalEnvironment,
                        principalTable: "ExternalEnvironment",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationExternalDeploymentPath_ExternalDeploymentPath",
                table: "ApplicationExternalDeploymentPath",
                column: "ExternalDeploymentPath");

            migrationBuilder.CreateIndex(
                name: "IX_ExternalDeploymentPath_Created By",
                table: "ExternalDeploymentPath",
                column: "Created By");

            migrationBuilder.CreateIndex(
                name: "IX_ExternalDeploymentPath_Modified By",
                table: "ExternalDeploymentPath",
                column: "Modified By");

            migrationBuilder.CreateIndex(
                name: "IX_ExternalDeploymentPath_Tenant",
                table: "ExternalDeploymentPath",
                column: "Tenant");

            migrationBuilder.CreateIndex(
                name: "IX_ExternalDeploymentPathEnvironment_ExternalEnvironment",
                table: "ExternalDeploymentPathEnvironment",
                column: "ExternalEnvironment");

            migrationBuilder.CreateIndex(
                name: "IX_ExternalEnvironment_Created By",
                table: "ExternalEnvironment",
                column: "Created By");

            migrationBuilder.CreateIndex(
                name: "IX_ExternalEnvironment_Environment",
                table: "ExternalEnvironment",
                column: "Environment",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExternalEnvironment_Modified By",
                table: "ExternalEnvironment",
                column: "Modified By");

            migrationBuilder.CreateIndex(
                name: "IX_UserExternalTenant_Tenant",
                table: "UserExternalTenant",
                column: "Tenant");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApplicationExternalDeploymentPath");

            migrationBuilder.DropTable(
                name: "ExternalDeploymentPathEnvironment");

            migrationBuilder.DropTable(
                name: "UserExternalTenant");

            migrationBuilder.DropTable(
                name: "ExternalDeploymentPath");

            migrationBuilder.DropTable(
                name: "ExternalEnvironment");

            migrationBuilder.DropColumn(
                name: "IsReleasedExternally",
                table: "Solution");

            migrationBuilder.DropColumn(
                name: "IsExternalDelivery",
                table: "Action");
        }
    }
}
