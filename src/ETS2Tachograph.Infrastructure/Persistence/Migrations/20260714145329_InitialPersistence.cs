using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ETS2Tachograph.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialPersistence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DriverProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DriverProfiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FerryRestRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    DriverCardId = table.Column<string>(type: "TEXT", nullable: false),
                    StartGameMinute = table.Column<long>(type: "INTEGER", nullable: false),
                    EndGameMinuteExclusive = table.Column<long>(type: "INTEGER", nullable: false),
                    InterruptionCount = table.Column<int>(type: "INTEGER", nullable: false),
                    InterruptionMinutes = table.Column<long>(type: "INTEGER", nullable: false),
                    Accepted = table.Column<bool>(type: "INTEGER", nullable: false),
                    RecordedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FerryRestRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RegulationSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    DriverCardId = table.Column<string>(type: "TEXT", nullable: false),
                    GameMinute = table.Column<long>(type: "INTEGER", nullable: false),
                    RecordedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    ContinuousDrivingMinutes = table.Column<long>(type: "INTEGER", nullable: false),
                    DailyDrivingMinutes = table.Column<long>(type: "INTEGER", nullable: false),
                    WeeklyDrivingMinutes = table.Column<long>(type: "INTEGER", nullable: false),
                    FortnightlyDrivingMinutes = table.Column<long>(type: "INTEGER", nullable: false),
                    MinutesUntilBreak = table.Column<long>(type: "INTEGER", nullable: false),
                    MinutesUntilDailyRestDeadline = table.Column<long>(type: "INTEGER", nullable: false),
                    ViolationsJson = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegulationSnapshots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DriverCards",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    DriverProfileId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CountryCode = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    ValidFrom = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    ValidUntil = table.Column<DateOnly>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DriverCards", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DriverCards_DriverProfiles_DriverProfileId",
                        column: x => x.DriverProfileId,
                        principalTable: "DriverProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ActivitySessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    DriverCardId = table.Column<string>(type: "TEXT", nullable: false),
                    SessionIndex = table.Column<int>(type: "INTEGER", nullable: false),
                    StartedAtGameMinute = table.Column<long>(type: "INTEGER", nullable: false),
                    EndedAtGameMinute = table.Column<long>(type: "INTEGER", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivitySessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ActivitySessions_DriverCards_DriverCardId",
                        column: x => x.DriverCardId,
                        principalTable: "DriverCards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ActivityRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ActivitySessionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Activity = table.Column<int>(type: "INTEGER", nullable: false),
                    StartGameMinute = table.Column<long>(type: "INTEGER", nullable: false),
                    EndGameMinuteExclusive = table.Column<long>(type: "INTEGER", nullable: false),
                    RecordedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    Source = table.Column<int>(type: "INTEGER", nullable: false),
                    Condition = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ActivityRecords_ActivitySessions_ActivitySessionId",
                        column: x => x.ActivitySessionId,
                        principalTable: "ActivitySessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ActivityRecords_ActivitySessionId_StartGameMinute",
                table: "ActivityRecords",
                columns: new[] { "ActivitySessionId", "StartGameMinute" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ActivitySessions_DriverCardId_SessionIndex",
                table: "ActivitySessions",
                columns: new[] { "DriverCardId", "SessionIndex" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DriverCards_DriverProfileId",
                table: "DriverCards",
                column: "DriverProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_DriverProfiles_IsActive",
                table: "DriverProfiles",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_FerryRestRecords_DriverCardId_StartGameMinute",
                table: "FerryRestRecords",
                columns: new[] { "DriverCardId", "StartGameMinute" });

            migrationBuilder.CreateIndex(
                name: "IX_RegulationSnapshots_DriverCardId_GameMinute",
                table: "RegulationSnapshots",
                columns: new[] { "DriverCardId", "GameMinute" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActivityRecords");

            migrationBuilder.DropTable(
                name: "FerryRestRecords");

            migrationBuilder.DropTable(
                name: "RegulationSnapshots");

            migrationBuilder.DropTable(
                name: "ActivitySessions");

            migrationBuilder.DropTable(
                name: "DriverCards");

            migrationBuilder.DropTable(
                name: "DriverProfiles");
        }
    }
}
