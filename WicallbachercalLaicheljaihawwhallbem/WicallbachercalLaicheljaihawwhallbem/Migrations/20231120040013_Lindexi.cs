using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WicallbachercalLaicheljaihawwhallbem.Migrations
{
    /// <inheritdoc />
    public partial class Lindexi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FileStorageModel",
                columns: table => new
                {
                    FileSha1Hash = table.Column<string>(type: "TEXT", nullable: false),
                    OriginFilePath = table.Column<string>(type: "TEXT", nullable: false),
                    ReferenceCount = table.Column<long>(type: "INTEGER", nullable: false),
                    FileLength = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileStorageModel", x => x.FileSha1Hash);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FileStorageModel");
        }
    }
}
