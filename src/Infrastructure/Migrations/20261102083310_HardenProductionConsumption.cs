using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class HardenProductionConsumption : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Raw refresh secrets cannot be recovered safely after rollout; affected accounts must log in again.
            migrationBuilder.Sql("""
                UPDATE "App"."TblUser" AS account
                SET "SecurityVersion" = md5(account."SecurityVersion"::text || ':20261102-refresh-token-hashing')::uuid
                WHERE EXISTS
                  (SELECT 1 FROM "public"."RefreshToken" AS token
                   WHERE token."UserId" = account."Id" AND token."Token" NOT LIKE 'sha256:%');

                UPDATE "public"."RefreshToken"
                SET "Token" = 'legacy-invalidated:' || "Id"::text, "IsRevoked" = true
                WHERE "Token" NOT LIKE 'sha256:%';
                """);

            migrationBuilder.DropIndex(
                name: "IX_EvaluationBatch_RecordedAt_Id",
                schema: "App",
                table: "EvaluationBatch");

            migrationBuilder.AddColumn<string>(
                name: "FailureReason",
                schema: "App",
                table: "EvaluationBatch",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "QuarantinedAt",
                schema: "App",
                table: "EvaluationBatch",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                schema: "App",
                table: "EvaluationBatch",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Pending");

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationBatch_QuarantinedAt",
                schema: "App",
                table: "EvaluationBatch",
                column: "QuarantinedAt");

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationBatch_Status_RecordedAt_Id",
                schema: "App",
                table: "EvaluationBatch",
                columns: new[] { "Status", "RecordedAt", "Id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_EvaluationBatch_QuarantinedAt",
                schema: "App",
                table: "EvaluationBatch");

            migrationBuilder.DropIndex(
                name: "IX_EvaluationBatch_Status_RecordedAt_Id",
                schema: "App",
                table: "EvaluationBatch");

            migrationBuilder.DropColumn(
                name: "FailureReason",
                schema: "App",
                table: "EvaluationBatch");

            migrationBuilder.DropColumn(
                name: "QuarantinedAt",
                schema: "App",
                table: "EvaluationBatch");

            migrationBuilder.DropColumn(
                name: "Status",
                schema: "App",
                table: "EvaluationBatch");

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationBatch_RecordedAt_Id",
                schema: "App",
                table: "EvaluationBatch",
                columns: new[] { "RecordedAt", "Id" });
        }
    }
}
