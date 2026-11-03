using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PersistRateLimitRejectionHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RateLimitHistoryCheckpoint",
                schema: "App",
                columns: table => new
                {
                    StreamKey = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    LastStreamId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    LastProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DiscardedEntries = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RateLimitHistoryCheckpoint", x => x.StreamKey);
                });

            migrationBuilder.CreateTable(
                name: "RateLimitRejectionSummary",
                schema: "App",
                columns: table => new
                {
                    BucketKey = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Minute = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: false),
                    Application = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Environment = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Method = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Route = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    RejectedRequests = table.Column<long>(type: "bigint", nullable: false),
                    FirstRejectedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastRejectedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RateLimitRejectionSummary", x => new { x.BucketKey, x.Minute });
                });

            migrationBuilder.CreateIndex(
                name: "IX_RateLimitRejectionSummary_Application_Environment_Minute",
                schema: "App",
                table: "RateLimitRejectionSummary",
                columns: new[] { "Application", "Environment", "Minute" });

            migrationBuilder.CreateIndex(
                name: "IX_RateLimitRejectionSummary_IpAddress_Minute",
                schema: "App",
                table: "RateLimitRejectionSummary",
                columns: new[] { "IpAddress", "Minute" });

            migrationBuilder.CreateIndex(
                name: "IX_RateLimitRejectionSummary_Minute",
                schema: "App",
                table: "RateLimitRejectionSummary",
                column: "Minute");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RateLimitHistoryCheckpoint",
                schema: "App");

            migrationBuilder.DropTable(
                name: "RateLimitRejectionSummary",
                schema: "App");
        }
    }
}
