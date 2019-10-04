using Microsoft.EntityFrameworkCore.Migrations;

namespace HalelkeherCiwerecawqal.Migrations
{
    public partial class BejiyuwhalldalJajallwherlere : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RalellawraFayyelchicurlu",
                table: "ResourceModel",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RalellawraFayyelchicurlu",
                table: "ResourceModel");
        }
    }
}
