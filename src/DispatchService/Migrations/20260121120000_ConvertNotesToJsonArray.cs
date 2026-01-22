using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DispatchService.Migrations
{
    /// <inheritdoc />
    public partial class ConvertNotesToJsonArray : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 1: Add a temporary column for the JSON data
            migrationBuilder.AddColumn<string>(
                name: "Notes_Temp",
                table: "DispatchOrders",
                type: "jsonb",
                nullable: true);

            // Step 2: Migrate existing data
            // Convert existing string Notes to JSON array format
            // If Notes is null or empty, set to empty array []
            // If Notes has content, wrap it in an array [{"value": "note content"}]
            migrationBuilder.Sql(@"
                UPDATE ""DispatchOrders""
                SET ""Notes_Temp"" = CASE
                    WHEN ""Notes"" IS NULL OR TRIM(""Notes"") = '' THEN '[]'::jsonb
                    ELSE jsonb_build_array(""Notes"")
                END;
            ");

            // Step 3: Drop the old column
            migrationBuilder.DropColumn(
                name: "Notes",
                table: "DispatchOrders");

            // Step 4: Rename the temporary column to Notes
            migrationBuilder.RenameColumn(
                name: "Notes_Temp",
                table: "DispatchOrders",
                newName: "Notes");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Step 1: Add back the string column
            migrationBuilder.AddColumn<string>(
                name: "Notes_String",
                table: "DispatchOrders",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            // Step 2: Convert JSON array back to string
            // Extract the first element from the array, or empty string if array is empty
            migrationBuilder.Sql(@"
                UPDATE ""DispatchOrders""
                SET ""Notes_String"" = CASE
                    WHEN ""Notes"" IS NULL OR jsonb_array_length(""Notes"") = 0 THEN NULL
                    ELSE ""Notes""->>0
                END;
            ");

            // Step 3: Drop the JSON column
            migrationBuilder.DropColumn(
                name: "Notes",
                table: "DispatchOrders");

            // Step 4: Rename the string column back to Notes
            migrationBuilder.RenameColumn(
                name: "Notes_String",
                table: "DispatchOrders",
                newName: "Notes");
        }
    }
}
