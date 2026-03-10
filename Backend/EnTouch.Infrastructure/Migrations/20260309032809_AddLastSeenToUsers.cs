using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnTouch.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLastSeenToUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastSeen",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastSeen",
                table: "AspNetUsers");
        }
    }
}
