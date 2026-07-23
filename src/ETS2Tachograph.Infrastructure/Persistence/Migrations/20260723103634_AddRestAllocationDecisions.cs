using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ETS2Tachograph.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRestAllocationDecisions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RestAllocationDecisions",
                columns: table => new
                {
                    DecisionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    DriverCardId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    RestBlockId = table.Column<string>(type: "TEXT", maxLength: 96, nullable: false),
                    CandidateId = table.Column<string>(type: "TEXT", maxLength: 96, nullable: false),
                    EffectiveAtGameMinute = table.Column<long>(type: "INTEGER", nullable: false),
                    DecidedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    DecisionSchemeVersion = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    SupersedesDecisionId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RestAllocationDecisions", x => x.DecisionId);
                    table.ForeignKey(
                        name: "FK_RestAllocationDecisions_DriverCards_DriverCardId",
                        column: x => x.DriverCardId,
                        principalTable: "DriverCards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RestAllocationDecisions_DriverCardId_RestBlockId_Status_DecidedAtUtc",
                table: "RestAllocationDecisions",
                columns: new[] { "DriverCardId", "RestBlockId", "Status", "DecidedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RestAllocationDecisions");
        }
    }
}
