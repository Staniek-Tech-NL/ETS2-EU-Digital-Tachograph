using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ETS2Tachograph.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MaterializeProjectedGapResolution : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ProjectionSourceGapId",
                table: "ActivityGaps",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ActivityGaps_ProjectionSourceGapId",
                table: "ActivityGaps",
                column: "ProjectionSourceGapId");

            migrationBuilder.AddForeignKey(
                name: "FK_ActivityGaps_ActivityGaps_ProjectionSourceGapId",
                table: "ActivityGaps",
                column: "ProjectionSourceGapId",
                principalTable: "ActivityGaps",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ActivityGaps_ActivityGaps_ProjectionSourceGapId",
                table: "ActivityGaps");

            migrationBuilder.DropIndex(
                name: "IX_ActivityGaps_ProjectionSourceGapId",
                table: "ActivityGaps");

            migrationBuilder.DropColumn(
                name: "ProjectionSourceGapId",
                table: "ActivityGaps");
        }
    }
}
