using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace at.D365.PowerCID.Portal.Data.Migrations
{
    /// <inheritdoc />
    public partial class SelectableDeploymentModes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Default Deployment Mode",
                table: "Solution",
                type: "int",
                nullable: true);

            migrationBuilder.Sql(
                "UPDATE [Solution] " +
                "SET [Default Deployment Mode] = CASE WHEN ISNULL([Apply Manually], 0) = 1 THEN 1 ELSE 0 END");

            migrationBuilder.DropColumn(
                name: "Apply Manually",
                table: "Solution");

            migrationBuilder.AddColumn<int>(
                name: "Deployment Mode",
                table: "Action",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Enable Workflows",
                table: "Action",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Overwrite Unmanaged Customizations",
                table: "Action",
                type: "bit",
                nullable: true);

            migrationBuilder.Sql(
                "UPDATE [Action] " +
                "SET [Deployment Mode] = ISNULL([Solution].[Default Deployment Mode], 0), " +
                "[Overwrite Unmanaged Customizations] = ISNULL([Solution].[Overwrite Unmanaged Customizations], 1), " +
                "[Enable Workflows] = ISNULL([Solution].[Enable Workflows], 1) " +
                "FROM [Action] INNER JOIN [Solution] ON [Action].[Solution] = [Solution].[Id] " +
                "WHERE [Action].[Type] IN (2, 3)");

            migrationBuilder.AddColumn<int>(
                name: "External Deployment Path",
                table: "Action",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Apply Manually",
                table: "Solution",
                type: "bit",
                nullable: true);

            migrationBuilder.Sql(
                "UPDATE [Solution] " +
                "SET [Apply Manually] = CASE WHEN [Default Deployment Mode] = 1 THEN 1 ELSE 0 END");

            migrationBuilder.DropColumn(
                name: "Default Deployment Mode",
                table: "Solution");

            migrationBuilder.DropColumn(
                name: "Deployment Mode",
                table: "Action");

            migrationBuilder.DropColumn(
                name: "Enable Workflows",
                table: "Action");

            migrationBuilder.DropColumn(
                name: "External Deployment Path",
                table: "Action");

            migrationBuilder.DropColumn(
                name: "Overwrite Unmanaged Customizations",
                table: "Action");
        }
    }
}
