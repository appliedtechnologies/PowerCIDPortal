using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace at.D365.PowerCID.Portal.Data.Migrations
{
    public partial class GuidsInsteadStrings : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "MS Id",
                table: "User",
                type: "uniqueidentifier",
                unicode: false,
                maxLength: 36,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(36)",
                oldUnicode: false,
                oldMaxLength: 36);

            migrationBuilder.AlterColumn<Guid>(
                name: "MS Id",
                table: "Tenant",
                type: "uniqueidentifier",
                unicode: false,
                maxLength: 36,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(36)",
                oldUnicode: false,
                oldMaxLength: 36);

            migrationBuilder.AlterColumn<Guid>(
                name: "MS Id",
                table: "Environment",
                type: "uniqueidentifier",
                unicode: false,
                maxLength: 36,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(36)",
                oldUnicode: false,
                oldMaxLength: 36);

            migrationBuilder.AlterColumn<Guid>(
                name: "MS Id",
                table: "Application",
                type: "uniqueidentifier",
                unicode: false,
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(100)",
                oldUnicode: false,
                oldMaxLength: 100);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "MS Id",
                table: "User",
                type: "varchar(36)",
                unicode: false,
                maxLength: 36,
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldUnicode: false,
                oldMaxLength: 36);

            migrationBuilder.AlterColumn<string>(
                name: "MS Id",
                table: "Tenant",
                type: "varchar(36)",
                unicode: false,
                maxLength: 36,
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldUnicode: false,
                oldMaxLength: 36);

            migrationBuilder.AlterColumn<string>(
                name: "MS Id",
                table: "Environment",
                type: "varchar(36)",
                unicode: false,
                maxLength: 36,
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldUnicode: false,
                oldMaxLength: 36);

            migrationBuilder.AlterColumn<string>(
                name: "MS Id",
                table: "Application",
                type: "varchar(100)",
                unicode: false,
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldUnicode: false,
                oldMaxLength: 100);
        }
    }
}
