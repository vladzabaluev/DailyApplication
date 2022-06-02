using Microsoft.EntityFrameworkCore.Migrations;

namespace DailyApplication.Data.Migrations
{
    public partial class AddUserIsInGroupCheck : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "UserIsInGroup",
                table: "UserGroup",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UserIsInGroup",
                table: "UserGroup");
        }
    }
}
