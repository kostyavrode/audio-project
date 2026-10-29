using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GroupsService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGroupInviteCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "InviteCode",
                table: "Groups",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InviteCode",
                table: "Groups");
        }
    }
}
