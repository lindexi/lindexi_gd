using Microsoft.EntityFrameworkCore.Migrations;

namespace HalelkeherCiwerecawqal.Migrations
{
    public partial class YahulihceGeliwcal : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "WaircegalhallwayneeHuwairfejaije",
                table: "ResourceModel",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WaircegalhallwayneeHuwairfejaije",
                table: "ResourceModel");
        }
    }
}
