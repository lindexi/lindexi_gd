using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PackageManager.Server.Migrations
{
    public partial class AddUnInstall : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ShouldUninstall",
                table: "PackageStorageDbSet",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ShouldUninstall",
                table: "LatestPackageDbSet",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ShouldUninstall",
                table: "PackageStorageDbSet");

            migrationBuilder.DropColumn(
                name: "ShouldUninstall",
                table: "LatestPackageDbSet");
        }
    }
}
