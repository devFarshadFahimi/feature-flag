using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CompleteApplicationReliability : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ConfigurationVersion",
                schema: "App",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Revision = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfigurationVersion", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EvaluationBatch",
                schema: "App",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    EnvironmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    AppName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Counts = table.Column<string>(type: "jsonb", maxLength: 50, nullable: false),
                    RecordedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvaluationBatch", x => x.Id);
                });

            migrationBuilder.InsertData(
                schema: "App",
                table: "ConfigurationVersion",
                columns: new[] { "Id", "Revision" },
                values: new object[] { 1, 0L });

            migrationBuilder.CreateIndex(
                name: "IX_EvaluationBatch_RecordedAt_Id",
                schema: "App",
                table: "EvaluationBatch",
                columns: new[] { "RecordedAt", "Id" });

            migrationBuilder.Sql("""
                CREATE FUNCTION "App"."AdvanceConfigurationVersion"() RETURNS trigger
                LANGUAGE plpgsql AS $$
                BEGIN
                    UPDATE "App"."ConfigurationVersion" SET "Revision" = "Revision" + 1 WHERE "Id" = 1;
                    RETURN NULL;
                END;
                $$;
                """);

            foreach (var table in new[] { "TblProject", "TblEnvironment", "TblFeature", "TblFeatureStrategy", "TblSegment", "TblVariant", "ProjectEnvironmentPolicy" })
            {
                migrationBuilder.Sql($"""
                    CREATE TRIGGER "ConfigurationChanged" AFTER INSERT OR UPDATE OR DELETE ON "App"."{table}"
                    FOR EACH STATEMENT EXECUTE FUNCTION "App"."AdvanceConfigurationVersion"();
                    """);
            }

            // LastSeenAt is telemetry, so updating it must not invalidate cached configurations.
            migrationBuilder.Sql("""
                CREATE TRIGGER "ConfigurationChanged" AFTER INSERT OR DELETE OR UPDATE OF "Enabled", "Revision"
                ON "App"."TblFeatureEnvironment" FOR EACH STATEMENT EXECUTE FUNCTION "App"."AdvanceConfigurationVersion"();
                CREATE TRIGGER "ConfigurationChanged" AFTER INSERT OR UPDATE OR DELETE ON "StrategyConstraints"
                FOR EACH STATEMENT EXECUTE FUNCTION "App"."AdvanceConfigurationVersion"();
                """);

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var table in new[] { "TblProject", "TblEnvironment", "TblFeature", "TblFeatureStrategy", "TblSegment", "TblVariant", "ProjectEnvironmentPolicy", "TblFeatureEnvironment" })
            {
                migrationBuilder.Sql($"DROP TRIGGER \"ConfigurationChanged\" ON \"App\".\"{table}\";");
            }

            migrationBuilder.Sql("""
                DROP TRIGGER "ConfigurationChanged" ON "StrategyConstraints";
                DROP FUNCTION "App"."AdvanceConfigurationVersion"();
                """);

            migrationBuilder.DropTable(
                name: "ConfigurationVersion",
                schema: "App");

            migrationBuilder.DropTable(
                name: "EvaluationBatch",
                schema: "App");
        }
    }
}
