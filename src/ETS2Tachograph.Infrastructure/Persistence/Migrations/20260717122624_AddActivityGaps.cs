using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ETS2Tachograph.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddActivityGaps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ActivityGaps",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    DriverCardId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    ActivitySessionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Slot = table.Column<int>(type: "INTEGER", nullable: false),
                    StartGameMinute = table.Column<long>(type: "INTEGER", nullable: false),
                    EndGameMinuteExclusive = table.Column<long>(type: "INTEGER", nullable: true),
                    Reason = table.Column<int>(type: "INTEGER", nullable: false),
                    State = table.Column<int>(type: "INTEGER", nullable: false),
                    ResolvedAtGameMinute = table.Column<long>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityGaps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ActivityGaps_ActivitySessions_ActivitySessionId",
                        column: x => x.ActivitySessionId,
                        principalTable: "ActivitySessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ActivityGaps_ActivitySessionId_StartGameMinute",
                table: "ActivityGaps",
                columns: new[] { "ActivitySessionId", "StartGameMinute" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ActivityGaps_DriverCardId_StartGameMinute",
                table: "ActivityGaps",
                columns: new[] { "DriverCardId", "StartGameMinute" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActivityGaps");
        }
    }
}
