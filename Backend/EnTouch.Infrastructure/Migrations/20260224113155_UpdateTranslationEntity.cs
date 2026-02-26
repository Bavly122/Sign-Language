using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnTouch.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateTranslationEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "VideoPath",
                table: "Translations",
                newName: "OutputVideoPath");

            migrationBuilder.AlterColumn<int>(
                name: "Type",
                table: "Translations",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "InputVideoPath",
                table: "Translations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Translations",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InputVideoPath",
                table: "Translations");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Translations");

            migrationBuilder.RenameColumn(
                name: "OutputVideoPath",
                table: "Translations",
                newName: "VideoPath");

            migrationBuilder.AlterColumn<string>(
                name: "Type",
                table: "Translations",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");
        }
    }
}
