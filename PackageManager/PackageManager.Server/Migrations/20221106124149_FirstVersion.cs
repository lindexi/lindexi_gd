using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PackageManager.Server.Migrations
{
    public partial class FirstVersion : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LatestPackageDbSet",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PackageId = table.Column<string>(type: "TEXT", nullable: true),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    Author = table.Column<string>(type: "TEXT", nullable: true),
                    Version = table.Column<long>(type: "INTEGER", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    IconUrl = table.Column<string>(type: "TEXT", nullable: true),
                    CanShow = table.Column<bool>(type: "INTEGER", nullable: false),
                    DownloadUrl = table.Column<string>(type: "TEXT", nullable: true),
                    SupportMinClientVersion = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LatestPackageDbSet", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PackageStorageDbSet",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PackageId = table.Column<string>(type: "TEXT", nullable: true),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    Author = table.Column<string>(type: "TEXT", nullable: true),
                    Version = table.Column<long>(type: "INTEGER", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    IconUrl = table.Column<string>(type: "TEXT", nullable: true),
                    CanShow = table.Column<bool>(type: "INTEGER", nullable: false),
                    DownloadUrl = table.Column<string>(type: "TEXT", nullable: true),
                    SupportMinClientVersion = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PackageStorageDbSet", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LatestPackageDbSet_PackageId",
                table: "LatestPackageDbSet",
                column: "PackageId");

            migrationBuilder.CreateIndex(
                name: "IX_PackageStorageDbSet_PackageId",
                table: "PackageStorageDbSet",
                column: "PackageId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LatestPackageDbSet");

            migrationBuilder.DropTable(
                name: "PackageStorageDbSet");
        }
    }
}
