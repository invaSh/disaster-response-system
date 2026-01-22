using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IncidentService.Migrations
{
    /// <inheritdoc />
    public partial class AddCreatedUserId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                table: "Incidents",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "Incidents");
        }
    }
}
