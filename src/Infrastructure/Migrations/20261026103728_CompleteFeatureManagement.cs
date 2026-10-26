using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CompleteFeatureManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SecurityVersion",
                schema: "App",
                table: "TblUser",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "ProjectId",
                schema: "App",
                table: "TblSegment",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "Revision",
                schema: "App",
                table: "TblSegment",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "Revision",
                schema: "App",
                table: "TblProject",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<bool>(
                name: "Enabled",
                schema: "App",
                table: "TblFeatureStrategy",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<long>(
                name: "Revision",
                schema: "App",
                table: "TblFeatureStrategy",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "Revision",
                schema: "App",
                table: "TblFeatureEnvironment",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "Prerequisites",
                schema: "App",
                table: "TblFeature",
                type: "jsonb",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<long>(
                name: "Revision",
                schema: "App",
                table: "TblFeature",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "Revision",
                schema: "App",
                table: "TblEnvironment",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AlterColumn<Guid>(
                name: "ProjectId",
                schema: "App",
                table: "TblChangeRequest",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<int>(
                name: "Attempts",
                schema: "App",
                table: "TblChangeRequest",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "CapturedRevisions",
                schema: "App",
                table: "TblChangeRequest",
                type: "jsonb",
                nullable: false,
                defaultValue: "{}");

            migrationBuilder.AddColumn<string>(
                name: "FailureReason",
                schema: "App",
                table: "TblChangeRequest",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NextAttemptAt",
                schema: "App",
                table: "TblChangeRequest",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PredecessorId",
                schema: "App",
                table: "TblChangeRequest",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                schema: "App",
                table: "TblChangeRequest",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RequiredApprovals",
                schema: "App",
                table: "TblChangeRequest",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ResultRevisions",
                schema: "App",
                table: "TblChangeRequest",
                type: "jsonb",
                nullable: false,
                defaultValue: "{}");

            migrationBuilder.AddColumn<long>(
                name: "Revision",
                schema: "App",
                table: "TblChangeRequest",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<Guid>(
                name: "ScheduledBy",
                schema: "App",
                table: "TblChangeRequest",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ScopeFingerprint",
                schema: "App",
                table: "TblChangeRequest",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "StrategyId",
                table: "StrategyConstraints",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<int>(
                name: "SegmentId",
                table: "StrategyConstraints",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.CreateTable(
                name: "ConfigurationHistory",
                schema: "App",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: true),
                    EnvironmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    ActorId = table.Column<Guid>(type: "uuid", nullable: true),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ResourceType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ResourceId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Operation = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Revision = table.Column<long>(type: "bigint", nullable: false),
                    Before = table.Column<string>(type: "jsonb", maxLength: 50, nullable: true),
                    After = table.Column<string>(type: "jsonb", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfigurationHistory", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FlagUsage",
                schema: "App",
                columns: table => new
                {
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    EnvironmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    AppName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    FeatureName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Variant = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false),
                    Impression = table.Column<bool>(type: "boolean", nullable: false),
                    Hour = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastSeenAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Count = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FlagUsage", x => new { x.ProjectId, x.EnvironmentId, x.AppName, x.FeatureName, x.Variant, x.Enabled, x.Impression, x.Hour });
                });

            migrationBuilder.CreateTable(
                name: "ImpressionBatch",
                schema: "App",
                columns: table => new
                {
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    EnvironmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    BatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReceivedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImpressionBatch", x => new { x.ProjectId, x.EnvironmentId, x.BatchId });
                });

            migrationBuilder.CreateTable(
                name: "ProjectEnvironmentPolicy",
                schema: "App",
                columns: table => new
                {
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    EnvironmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Revision = table.Column<long>(type: "bigint", nullable: false),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false),
                    RequiredApprovals = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectEnvironmentPolicy", x => new { x.ProjectId, x.EnvironmentId });
                    table.ForeignKey(
                        name: "FK_ProjectEnvironmentPolicy_TblEnvironment_EnvironmentId",
                        column: x => x.EnvironmentId,
                        principalSchema: "App",
                        principalTable: "TblEnvironment",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProjectEnvironmentPolicy_TblProject_ProjectId",
                        column: x => x.ProjectId,
                        principalSchema: "App",
                        principalTable: "TblProject",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql("""
                UPDATE "StrategyConstraints" SET "SegmentId" = NULL WHERE "SegmentId" = 0;
                UPDATE "StrategyConstraints" SET "StrategyId" = NULL WHERE "StrategyId" = '00000000-0000-0000-0000-000000000000';
                DO $$ BEGIN
                    IF EXISTS (SELECT 1 FROM "StrategyConstraints" WHERE ("SegmentId" IS NULL) = ("StrategyId" IS NULL)) THEN
                        RAISE EXCEPTION 'مالک قیود قدیمی مبهم است؛ پیش از ارتقا مالک هر قید را مشخص کنید.';
                    END IF;
                    IF EXISTS (SELECT 1 FROM "App"."TblVariant" WHERE ("FeatureEnvironmentId" IS NULL) = ("StrategyId" IS NULL)) THEN
                        RAISE EXCEPTION 'مالک گونه‌های قدیمی مبهم است؛ پیش از ارتقا مالک هر گونه را مشخص کنید.';
                    END IF;
                END $$;
                INSERT INTO "App"."TblFeatureEnvironment" ("Id", "BusinessId", "FeatureId", "EnvironmentId", "Enabled", "Revision")
                SELECT gen_random_uuid(), gen_random_uuid(), feature."Id", environment."Id", false, 0
                FROM "App"."TblFeature" feature CROSS JOIN "App"."TblEnvironment" environment
                WHERE feature."ArchivedAt" IS NULL
                ON CONFLICT ("FeatureId", "EnvironmentId") DO NOTHING;
                UPDATE "App"."TblChangeRequest" SET "RequiredApprovals" = 1,
                    "Status" = CASE WHEN "Status" IN ('Approved', 'Scheduled', 'InReview') THEN 'Draft' ELSE "Status" END;
                """);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Variant_OneOwner",
                schema: "App",
                table: "TblVariant",
                sql: "(\"FeatureEnvironmentId\" IS NULL) <> (\"StrategyId\" IS NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Constraint_OneOwner",
                table: "StrategyConstraints",
                sql: "(\"SegmentId\" IS NULL) <> (\"StrategyId\" IS NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_ConfigurationHistory_ProjectId_Timestamp",
                schema: "App",
                table: "ConfigurationHistory",
                columns: new[] { "ProjectId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_FlagUsage_Hour",
                schema: "App",
                table: "FlagUsage",
                column: "Hour");

            migrationBuilder.CreateIndex(
                name: "IX_ImpressionBatch_ReceivedAt",
                schema: "App",
                table: "ImpressionBatch",
                column: "ReceivedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectEnvironmentPolicy_EnvironmentId",
                schema: "App",
                table: "ProjectEnvironmentPolicy",
                column: "EnvironmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_StrategyConstraints_TblFeatureStrategy_StrategyId",
                table: "StrategyConstraints",
                column: "StrategyId",
                principalSchema: "App",
                principalTable: "TblFeatureStrategy",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TblVariant_TblFeatureStrategy_StrategyId",
                schema: "App",
                table: "TblVariant",
                column: "StrategyId",
                principalSchema: "App",
                principalTable: "TblFeatureStrategy",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StrategyConstraints_TblFeatureStrategy_StrategyId",
                table: "StrategyConstraints");

            migrationBuilder.DropForeignKey(
                name: "FK_TblVariant_TblFeatureStrategy_StrategyId",
                schema: "App",
                table: "TblVariant");

            migrationBuilder.DropTable(
                name: "ConfigurationHistory",
                schema: "App");

            migrationBuilder.DropTable(
                name: "FlagUsage",
                schema: "App");

            migrationBuilder.DropTable(
                name: "ImpressionBatch",
                schema: "App");

            migrationBuilder.DropTable(
                name: "ProjectEnvironmentPolicy",
                schema: "App");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Variant_OneOwner",
                schema: "App",
                table: "TblVariant");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Constraint_OneOwner",
                table: "StrategyConstraints");

            migrationBuilder.DropColumn(
                name: "SecurityVersion",
                schema: "App",
                table: "TblUser");

            migrationBuilder.DropColumn(
                name: "ProjectId",
                schema: "App",
                table: "TblSegment");

            migrationBuilder.DropColumn(
                name: "Revision",
                schema: "App",
                table: "TblSegment");

            migrationBuilder.DropColumn(
                name: "Revision",
                schema: "App",
                table: "TblProject");

            migrationBuilder.DropColumn(
                name: "Enabled",
                schema: "App",
                table: "TblFeatureStrategy");

            migrationBuilder.DropColumn(
                name: "Revision",
                schema: "App",
                table: "TblFeatureStrategy");

            migrationBuilder.DropColumn(
                name: "Revision",
                schema: "App",
                table: "TblFeatureEnvironment");

            migrationBuilder.DropColumn(
                name: "Prerequisites",
                schema: "App",
                table: "TblFeature");

            migrationBuilder.DropColumn(
                name: "Revision",
                schema: "App",
                table: "TblFeature");

            migrationBuilder.DropColumn(
                name: "Revision",
                schema: "App",
                table: "TblEnvironment");

            migrationBuilder.DropColumn(
                name: "Attempts",
                schema: "App",
                table: "TblChangeRequest");

            migrationBuilder.DropColumn(
                name: "CapturedRevisions",
                schema: "App",
                table: "TblChangeRequest");

            migrationBuilder.DropColumn(
                name: "FailureReason",
                schema: "App",
                table: "TblChangeRequest");

            migrationBuilder.DropColumn(
                name: "NextAttemptAt",
                schema: "App",
                table: "TblChangeRequest");

            migrationBuilder.DropColumn(
                name: "PredecessorId",
                schema: "App",
                table: "TblChangeRequest");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                schema: "App",
                table: "TblChangeRequest");

            migrationBuilder.DropColumn(
                name: "RequiredApprovals",
                schema: "App",
                table: "TblChangeRequest");

            migrationBuilder.DropColumn(
                name: "ResultRevisions",
                schema: "App",
                table: "TblChangeRequest");

            migrationBuilder.DropColumn(
                name: "Revision",
                schema: "App",
                table: "TblChangeRequest");

            migrationBuilder.DropColumn(
                name: "ScheduledBy",
                schema: "App",
                table: "TblChangeRequest");

            migrationBuilder.DropColumn(
                name: "ScopeFingerprint",
                schema: "App",
                table: "TblChangeRequest");

            migrationBuilder.AlterColumn<Guid>(
                name: "ProjectId",
                schema: "App",
                table: "TblChangeRequest",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "StrategyId",
                table: "StrategyConstraints",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "SegmentId",
                table: "StrategyConstraints",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);
        }
    }
}
