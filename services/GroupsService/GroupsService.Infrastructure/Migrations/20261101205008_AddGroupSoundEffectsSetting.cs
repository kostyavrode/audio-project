using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GroupsService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGroupSoundEffectsSetting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "SoundEffectsDisabled",
                table: "Groups",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SoundEffectsDisabled",
                table: "Groups");
        }
    }
}
