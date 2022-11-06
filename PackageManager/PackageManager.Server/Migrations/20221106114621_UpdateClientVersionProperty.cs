using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PackageManager.Server.Migrations
{
    public partial class UpdateClientVersionProperty : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SupportClientPlatform",
                table: "PackageInfo");

            migrationBuilder.AlterColumn<long>(
                name: "Version",
                table: "PackageInfo",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "SupportMinClientVersion",
                table: "PackageInfo",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Version",
                table: "PackageInfo",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<string>(
                name: "SupportMinClientVersion",
                table: "PackageInfo",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "INTEGER");

            migrationBuilder.AddColumn<string>(
                name: "SupportClientPlatform",
                table: "PackageInfo",
                type: "TEXT",
                nullable: true);
        }
    }
}
