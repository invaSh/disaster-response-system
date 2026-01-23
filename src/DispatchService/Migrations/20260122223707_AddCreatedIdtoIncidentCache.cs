using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DispatchService.Migrations
{
    /// <inheritdoc />
    public partial class AddCreatedIdtoIncidentCache : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                table: "Incidents",
                type: "uuid",
                nullable: true);

            // Update any NULL Notes to empty JSON array before making it non-nullable
            migrationBuilder.Sql(@"
                UPDATE ""DispatchOrders""
                SET ""Notes"" = '[]'::jsonb
                WHERE ""Notes"" IS NULL;
            ");

            // Alter the Notes column to be non-nullable with a JSON default
            migrationBuilder.Sql(@"
                ALTER TABLE ""DispatchOrders""
                ALTER COLUMN ""Notes"" SET NOT NULL,
                ALTER COLUMN ""Notes"" SET DEFAULT '[]'::jsonb;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "Incidents");

            // Revert the Notes column to nullable
            migrationBuilder.Sql(@"
                ALTER TABLE ""DispatchOrders""
                ALTER COLUMN ""Notes"" DROP NOT NULL,
                ALTER COLUMN ""Notes"" DROP DEFAULT;
            ");
        }
    }
}
