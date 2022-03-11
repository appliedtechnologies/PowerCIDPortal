using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace at.D365.PowerCID.Portal.Data.Migrations
{
    public partial class SolutionTypesInheritance : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ActionResult",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Result = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActionResult", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ActionStatus",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Status = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActionStatus", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ActionType",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Type = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActionType", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tenant",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    MSId = table.Column<string>(name: "MS Id", type: "varchar(32)", unicode: false, maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tenant", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "User",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Firstname = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    Lastname = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: false),
                    MSId = table.Column<string>(name: "MS Id", type: "varchar(32)", unicode: false, maxLength: 32, nullable: false),
                    Tenant = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_User", x => x.Id);
                    table.ForeignKey(
                        name: "FK_User_Tenant",
                        column: x => x.Tenant,
                        principalTable: "Tenant",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Application",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrdinalNumber = table.Column<int>(name: "Ordinal Number", type: "int", nullable: true),
                    Name = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    MSId = table.Column<string>(name: "MS Id", type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    SolutionUniqueName = table.Column<string>(name: "Solution Unique Name", type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    DevelopmentEnvironment = table.Column<int>(name: "Development Environment", type: "int", nullable: false),
                    Tenant = table.Column<int>(type: "int", nullable: false),
                    CreatedBy = table.Column<int>(name: "Created By", type: "int", nullable: false),
                    CreatedOn = table.Column<DateTime>(name: "Created On", type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<int>(name: "Modified By", type: "int", nullable: false),
                    ModifiedOn = table.Column<DateTime>(name: "Modified On", type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Application", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Application_Created_By",
                        column: x => x.CreatedBy,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Application_Modified_By",
                        column: x => x.ModifiedBy,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Application_Tenant",
                        column: x => x.Tenant,
                        principalTable: "Tenant",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Environment",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrdinalNumber = table.Column<int>(name: "Ordinal Number", type: "int", nullable: true),
                    Name = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    BasicURL = table.Column<string>(name: "Basic URL", type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    IsDevelopmentEnvironment = table.Column<bool>(name: "Is Development Environment", type: "bit", nullable: false),
                    MSId = table.Column<string>(name: "MS Id", type: "varchar(32)", unicode: false, maxLength: 32, nullable: false),
                    Tenant = table.Column<int>(type: "int", nullable: false),
                    CreatedBy = table.Column<int>(name: "Created By", type: "int", nullable: false),
                    CreatedOn = table.Column<DateTime>(name: "Created On", type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<int>(name: "Modified By", type: "int", nullable: false),
                    ModifiedOn = table.Column<DateTime>(name: "Modified On", type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Environment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Environment_Created_By",
                        column: x => x.CreatedBy,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Environment_Modified_By",
                        column: x => x.ModifiedBy,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Environment_Tenant",
                        column: x => x.Tenant,
                        principalTable: "Tenant",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Solution",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    Application = table.Column<int>(type: "int", nullable: false),
                    Version = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    URLMakerportal = table.Column<string>(name: "URL Makerportal", type: "varchar(512)", unicode: false, maxLength: 512, nullable: false),
                    UniqueName = table.Column<string>(name: "Unique Name", type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    CreatedBy = table.Column<int>(name: "Created By", type: "int", nullable: false),
                    CreatedOn = table.Column<DateTime>(name: "Created On", type: "datetime2", nullable: false),
                    ModifiedBy = table.Column<int>(name: "Modified By", type: "int", nullable: false),
                    ModifiedOn = table.Column<DateTime>(name: "Modified On", type: "datetime2", nullable: false),
                    SolutionType = table.Column<int>(type: "int", nullable: false),
                    ApplyManually = table.Column<bool>(name: "Apply Manually", type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Solution", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Solution_Application",
                        column: x => x.Application,
                        principalTable: "Application",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Solution_Created_by",
                        column: x => x.CreatedBy,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Solution_Modified_by",
                        column: x => x.ModifiedBy,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Action",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    TargetEnvironment = table.Column<int>(name: "Target Environment", type: "int", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: true),
                    Result = table.Column<int>(type: "int", nullable: true),
                    StartTime = table.Column<DateTime>(name: "Start Time", type: "datetime2", nullable: true),
                    Solution = table.Column<int>(type: "int", nullable: true),
                    ErrorMessage = table.Column<string>(name: "Error Message", type: "varchar(max)", unicode: false, nullable: true),
                    CreatedBy = table.Column<int>(name: "Created By", type: "int", nullable: false),
                    CreatedOn = table.Column<DateTime>(name: "Created On", type: "datetime2", nullable: false),
                    FinishTime = table.Column<DateTime>(name: "Finish Time", type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Action", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Action_Created_By",
                        column: x => x.CreatedBy,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Action_Result",
                        column: x => x.Result,
                        principalTable: "ActionResult",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Action_Solution",
                        column: x => x.Solution,
                        principalTable: "Solution",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Action_Status",
                        column: x => x.Status,
                        principalTable: "ActionStatus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Action_Target_Environment",
                        column: x => x.TargetEnvironment,
                        principalTable: "Environment",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Action_Type",
                        column: x => x.Type,
                        principalTable: "ActionType",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Action_Created By",
                table: "Action",
                column: "Created By");

            migrationBuilder.CreateIndex(
                name: "IX_Action_Result",
                table: "Action",
                column: "Result");

            migrationBuilder.CreateIndex(
                name: "IX_Action_Solution",
                table: "Action",
                column: "Solution");

            migrationBuilder.CreateIndex(
                name: "IX_Action_Status",
                table: "Action",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Action_Target Environment",
                table: "Action",
                column: "Target Environment");

            migrationBuilder.CreateIndex(
                name: "IX_Action_Type",
                table: "Action",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_Application_Created By",
                table: "Application",
                column: "Created By");

            migrationBuilder.CreateIndex(
                name: "IX_Application_Modified By",
                table: "Application",
                column: "Modified By");

            migrationBuilder.CreateIndex(
                name: "IX_Application_Tenant",
                table: "Application",
                column: "Tenant");

            migrationBuilder.CreateIndex(
                name: "IX_Environment_Created By",
                table: "Environment",
                column: "Created By");

            migrationBuilder.CreateIndex(
                name: "IX_Environment_Modified By",
                table: "Environment",
                column: "Modified By");

            migrationBuilder.CreateIndex(
                name: "IX_Environment_Tenant",
                table: "Environment",
                column: "Tenant");

            migrationBuilder.CreateIndex(
                name: "IX_Solution_Application",
                table: "Solution",
                column: "Application");

            migrationBuilder.CreateIndex(
                name: "IX_Solution_Created By",
                table: "Solution",
                column: "Created By");

            migrationBuilder.CreateIndex(
                name: "IX_Solution_Modified By",
                table: "Solution",
                column: "Modified By");

            migrationBuilder.CreateIndex(
                name: "IX_User_Tenant",
                table: "User",
                column: "Tenant");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Action");

            migrationBuilder.DropTable(
                name: "ActionResult");

            migrationBuilder.DropTable(
                name: "Solution");

            migrationBuilder.DropTable(
                name: "ActionStatus");

            migrationBuilder.DropTable(
                name: "Environment");

            migrationBuilder.DropTable(
                name: "ActionType");

            migrationBuilder.DropTable(
                name: "Application");

            migrationBuilder.DropTable(
                name: "User");

            migrationBuilder.DropTable(
                name: "Tenant");
        }
    }
}
