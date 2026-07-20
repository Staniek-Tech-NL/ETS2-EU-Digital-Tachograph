using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ETS2Tachograph.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddManualGapResolution : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SourceGapId",
                table: "WarmActivityBlocks",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SourceGapId",
                table: "ActivityRecords",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_WarmActivityBlocks_SourceGapId",
                table: "WarmActivityBlocks",
                column: "SourceGapId");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityRecords_SourceGapId_StartGameMinute",
                table: "ActivityRecords",
                columns: new[] { "SourceGapId", "StartGameMinute" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ActivityRecords_ActivityGaps_SourceGapId",
                table: "ActivityRecords",
                column: "SourceGapId",
                principalTable: "ActivityGaps",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ActivityRecords_ActivityGaps_SourceGapId",
                table: "ActivityRecords");

            migrationBuilder.DropIndex(
                name: "IX_WarmActivityBlocks_SourceGapId",
                table: "WarmActivityBlocks");

            migrationBuilder.DropIndex(
                name: "IX_ActivityRecords_SourceGapId_StartGameMinute",
                table: "ActivityRecords");

            migrationBuilder.DropColumn(
                name: "SourceGapId",
                table: "WarmActivityBlocks");

            migrationBuilder.DropColumn(
                name: "SourceGapId",
                table: "ActivityRecords");
        }
    }
}
