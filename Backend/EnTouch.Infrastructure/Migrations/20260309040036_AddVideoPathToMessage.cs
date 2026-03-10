using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnTouch.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddVideoPathToMessage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "VideoPath",
                table: "Messages",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VideoPath",
                table: "Messages");
        }
    }
}
