using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DispatchService.Migrations
{
    /// <inheritdoc />
    public partial class IncidentModelCache : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DispatchAssignments_DispatchOrders_DispatchOrderId",
                table: "DispatchAssignments");

            migrationBuilder.DropForeignKey(
                name: "FK_DispatchAssignments_Units_UnitId",
                table: "DispatchAssignments");

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "Units",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Notes",
                table: "DispatchOrders",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "Incidents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IncidentId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Severity = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Latitude = table.Column<double>(type: "double precision", nullable: false),
                    Longitude = table.Column<double>(type: "double precision", nullable: false),
                    ReportedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastSyncedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Incidents", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Incidents_IncidentId",
                table: "Incidents",
                column: "IncidentId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_DispatchAssignments_DispatchOrders_DispatchOrderId",
                table: "DispatchAssignments",
                column: "DispatchOrderId",
                principalTable: "DispatchOrders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DispatchAssignments_Units_UnitId",
                table: "DispatchAssignments",
                column: "UnitId",
                principalTable: "Units",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DispatchOrders_Incidents_IncidentId",
                table: "DispatchOrders",
                column: "IncidentId",
                principalTable: "Incidents",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DispatchAssignments_DispatchOrders_DispatchOrderId",
                table: "DispatchAssignments");

            migrationBuilder.DropForeignKey(
                name: "FK_DispatchAssignments_Units_UnitId",
                table: "DispatchAssignments");

            migrationBuilder.DropForeignKey(
                name: "FK_DispatchOrders_Incidents_IncidentId",
                table: "DispatchOrders");

            migrationBuilder.DropTable(
                name: "Incidents");

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "Units",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "Notes",
                table: "DispatchOrders",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_DispatchAssignments_DispatchOrders_DispatchOrderId",
                table: "DispatchAssignments",
                column: "DispatchOrderId",
                principalTable: "DispatchOrders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DispatchAssignments_Units_UnitId",
                table: "DispatchAssignments",
                column: "UnitId",
                principalTable: "Units",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
