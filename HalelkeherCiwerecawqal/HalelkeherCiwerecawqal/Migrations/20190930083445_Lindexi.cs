using Microsoft.EntityFrameworkCore.Migrations;

namespace HalelkeherCiwerecawqal.Migrations
{
    public partial class Lindexi : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ResourceModel",
                columns: table => new
                {
                    Id = table.Column<string>(nullable: false),
                    ResourceId = table.Column<string>(nullable: true),
                    ResourceName = table.Column<string>(nullable: true),
                    LocalPath = table.Column<string>(nullable: true),
                    ResourceSign = table.Column<string>(nullable: true),
                    ResourceFileDetail = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResourceModel", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ResourceModel");
        }
    }
}
