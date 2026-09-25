using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Circuit.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Tournaments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Slug = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    Season = table.Column<int>(type: "INTEGER", nullable: false),
                    City = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    StartsOn = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    EndsOn = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    IsComplete = table.Column<bool>(type: "INTEGER", nullable: false),
                    SourceUrl = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    SnapshotOn = table.Column<DateOnly>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tournaments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Teams",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TournamentId = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    ShortName = table.Column<string>(type: "TEXT", maxLength: 12, nullable: false),
                    Region = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    AccentColor = table.Column<string>(type: "TEXT", nullable: false),
                    MarkUrl = table.Column<string>(type: "TEXT", maxLength: 250, nullable: true),
                    Placement = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Teams", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Teams_Tournaments_TournamentId",
                        column: x => x.TournamentId,
                        principalTable: "Tournaments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Matches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TournamentId = table.Column<int>(type: "INTEGER", nullable: false),
                    Code = table.Column<string>(type: "TEXT", maxLength: 12, nullable: false),
                    Lane = table.Column<string>(type: "TEXT", nullable: false),
                    Round = table.Column<int>(type: "INTEGER", nullable: false),
                    Slot = table.Column<int>(type: "INTEGER", nullable: false),
                    Label = table.Column<string>(type: "TEXT", maxLength: 70, nullable: false),
                    TeamAId = table.Column<int>(type: "INTEGER", nullable: true),
                    TeamBId = table.Column<int>(type: "INTEGER", nullable: true),
                    ScoreA = table.Column<int>(type: "INTEGER", nullable: true),
                    ScoreB = table.Column<int>(type: "INTEGER", nullable: true),
                    BestOf = table.Column<int>(type: "INTEGER", nullable: false),
                    MatchDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    NextWinCode = table.Column<string>(type: "TEXT", maxLength: 12, nullable: true),
                    NextLossCode = table.Column<string>(type: "TEXT", maxLength: 12, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Matches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Matches_Teams_TeamAId",
                        column: x => x.TeamAId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Matches_Teams_TeamBId",
                        column: x => x.TeamBId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Matches_Tournaments_TournamentId",
                        column: x => x.TournamentId,
                        principalTable: "Tournaments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Matches_TeamAId",
                table: "Matches",
                column: "TeamAId");

            migrationBuilder.CreateIndex(
                name: "IX_Matches_TeamBId",
                table: "Matches",
                column: "TeamBId");

            migrationBuilder.CreateIndex(
                name: "IX_Matches_TournamentId_Code",
                table: "Matches",
                columns: new[] { "TournamentId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Teams_TournamentId_ShortName",
                table: "Teams",
                columns: new[] { "TournamentId", "ShortName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tournaments_Slug",
                table: "Tournaments",
                column: "Slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Matches");

            migrationBuilder.DropTable(
                name: "Teams");

            migrationBuilder.DropTable(
                name: "Tournaments");
        }
    }
}
