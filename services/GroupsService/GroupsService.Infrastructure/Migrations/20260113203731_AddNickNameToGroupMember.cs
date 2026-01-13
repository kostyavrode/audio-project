using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GroupsService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNickNameToGroupMember : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NickName",
                table: "GroupMembers",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "Unknown");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NickName",
                table: "GroupMembers");
        }
    }
}
