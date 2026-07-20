using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ETS2Tachograph.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLayeredActivityRetention : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsArchivedToWarm",
                table: "ActivityRecords",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "ActivityRetentionStates",
                columns: table => new
                {
                    DriverCardId = table.Column<string>(type: "TEXT", nullable: false),
                    HighWaterMarkGameMinute = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityRetentionStates", x => x.DriverCardId);
                    table.ForeignKey(
                        name: "FK_ActivityRetentionStates_DriverCards_DriverCardId",
                        column: x => x.DriverCardId,
                        principalTable: "DriverCards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WarmActivityBlocks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    DriverCardId = table.Column<string>(type: "TEXT", nullable: false),
                    StartGameMinute = table.Column<long>(type: "INTEGER", nullable: false),
                    EndGameMinuteExclusive = table.Column<long>(type: "INTEGER", nullable: false),
                    DurationMinutes = table.Column<long>(type: "INTEGER", nullable: false),
                    Activity = table.Column<int>(type: "INTEGER", nullable: false),
                    Source = table.Column<int>(type: "INTEGER", nullable: false),
                    Condition = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WarmActivityBlocks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WarmActivityBlocks_DriverCards_DriverCardId",
                        column: x => x.DriverCardId,
                        principalTable: "DriverCards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WarmActivityBlocks_DriverCardId_StartGameMinute",
                table: "WarmActivityBlocks",
                columns: new[] { "DriverCardId", "StartGameMinute" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActivityRetentionStates");

            migrationBuilder.DropTable(
                name: "WarmActivityBlocks");

            migrationBuilder.DropColumn(
                name: "IsArchivedToWarm",
                table: "ActivityRecords");
        }
    }
}
