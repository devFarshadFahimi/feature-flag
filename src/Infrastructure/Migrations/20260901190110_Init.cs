using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "App");

            migrationBuilder.EnsureSchema(
                name: "Base");

            migrationBuilder.CreateTable(
                name: "StrategyConstraints",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ContextName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Operator = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Values = table.Column<string>(type: "jsonb", nullable: false),
                    Inverted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CaseInsensitive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    SegmentId = table.Column<int>(type: "integer", nullable: false),
                    StrategyId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StrategyConstraints", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TblApiToken",
                schema: "App",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EnvironmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    TokenType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    TokenHash = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastUsedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsRevoked = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    BusinessId = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TblApiToken", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TblChangeRequest",
                schema: "App",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    EnvironmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, defaultValue: "Draft"),
                    Title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    ScheduledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReviewedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    BusinessId = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TblChangeRequest", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TblChangeRequestItem",
                schema: "App",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ChangeRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    FeatureId = table.Column<Guid>(type: "uuid", nullable: true),
                    Payload = table.Column<string>(type: "jsonb", maxLength: 50, nullable: true),
                    BusinessId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TblChangeRequestItem", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TblDataProtectionKey",
                schema: "Base",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FriendlyName = table.Column<string>(type: "character varying(32767)", maxLength: 32767, nullable: true),
                    Xml = table.Column<string>(type: "character varying(32767)", maxLength: 32767, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TblDataProtectionKey", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TblEnvironment",
                schema: "App",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    Protected = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    BusinessId = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TblEnvironment", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TblFeature",
                schema: "App",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Lifecycle = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, defaultValue: "Planned"),
                    IsStale = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    ImpressionDataEnabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ArchivedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    BusinessId = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TblFeature", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TblFeatureEnvironment",
                schema: "App",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FeatureId = table.Column<Guid>(type: "uuid", nullable: false),
                    EnvironmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    LastSeenAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    BusinessId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TblFeatureEnvironment", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TblFeatureStrategy",
                schema: "App",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FeatureId = table.Column<Guid>(type: "uuid", nullable: false),
                    EnvironmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    RolloutPercentage = table.Column<int>(type: "integer", nullable: true),
                    Stickiness = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    GroupId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    UserIds = table.Column<string>(type: "jsonb", nullable: false),
                    IpAddresses = table.Column<string>(type: "jsonb", nullable: false),
                    ApplicationNames = table.Column<string>(type: "jsonb", nullable: false),
                    CustomParameters = table.Column<string>(type: "jsonb", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    SegmentIds = table.Column<string>(type: "jsonb", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TblFeatureStrategy", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TblProject",
                schema: "App",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    DefaultStickiness = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false, defaultValue: "default"),
                    FeatureLimitEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    FeatureLimit = table.Column<int>(type: "integer", nullable: true),
                    BusinessId = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TblProject", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TblProjectMember",
                schema: "App",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TblProjectMember", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TblSegment",
                schema: "App",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    IsPublic = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastUsedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    BusinessId = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TblSegment", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TblUser",
                schema: "App",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    PasswordHash = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Role = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, defaultValue: "Viewer"),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastLoginAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    BusinessId = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TblUser", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TblVariant",
                schema: "App",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Weight = table.Column<int>(type: "integer", nullable: false),
                    Stickiness = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Payload = table.Column<string>(type: "jsonb", nullable: false),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    FeatureEnvironmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    StrategyId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TblVariant", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StrategyConstraints_ContextName",
                table: "StrategyConstraints",
                column: "ContextName");

            migrationBuilder.CreateIndex(
                name: "IX_StrategyConstraints_SegmentId",
                table: "StrategyConstraints",
                column: "SegmentId");

            migrationBuilder.CreateIndex(
                name: "IX_StrategyConstraints_StrategyId",
                table: "StrategyConstraints",
                column: "StrategyId");

            migrationBuilder.CreateIndex(
                name: "IX_TblApiToken_EnvironmentId",
                schema: "App",
                table: "TblApiToken",
                column: "EnvironmentId");

            migrationBuilder.CreateIndex(
                name: "IX_TblApiToken_IsRevoked",
                schema: "App",
                table: "TblApiToken",
                column: "IsRevoked");

            migrationBuilder.CreateIndex(
                name: "IX_TblApiToken_TokenHash",
                schema: "App",
                table: "TblApiToken",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TblApiToken_TokenHash_IsRevoked",
                schema: "App",
                table: "TblApiToken",
                columns: new[] { "TokenHash", "IsRevoked" });

            migrationBuilder.CreateIndex(
                name: "IX_TblApiToken_TokenType",
                schema: "App",
                table: "TblApiToken",
                column: "TokenType");

            migrationBuilder.CreateIndex(
                name: "IX_TblChangeRequest_CreatedBy",
                schema: "App",
                table: "TblChangeRequest",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_TblChangeRequest_EnvironmentId",
                schema: "App",
                table: "TblChangeRequest",
                column: "EnvironmentId");

            migrationBuilder.CreateIndex(
                name: "IX_TblChangeRequest_ProjectId",
                schema: "App",
                table: "TblChangeRequest",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_TblChangeRequest_ProjectId_Status",
                schema: "App",
                table: "TblChangeRequest",
                columns: new[] { "ProjectId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_TblChangeRequest_ScheduledAt",
                schema: "App",
                table: "TblChangeRequest",
                column: "ScheduledAt",
                filter: "\"ScheduledAt\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_TblChangeRequest_Status",
                schema: "App",
                table: "TblChangeRequest",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_TblChangeRequestItem_Action",
                schema: "App",
                table: "TblChangeRequestItem",
                column: "Action");

            migrationBuilder.CreateIndex(
                name: "IX_TblChangeRequestItem_ChangeRequestId",
                schema: "App",
                table: "TblChangeRequestItem",
                column: "ChangeRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_TblChangeRequestItem_FeatureId",
                schema: "App",
                table: "TblChangeRequestItem",
                column: "FeatureId");

            migrationBuilder.CreateIndex(
                name: "IX_TblEnvironment_Name",
                schema: "App",
                table: "TblEnvironment",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TblEnvironment_SortOrder",
                schema: "App",
                table: "TblEnvironment",
                column: "SortOrder");

            migrationBuilder.CreateIndex(
                name: "IX_TblEnvironment_Type",
                schema: "App",
                table: "TblEnvironment",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_TblFeature_Lifecycle",
                schema: "App",
                table: "TblFeature",
                column: "Lifecycle");

            migrationBuilder.CreateIndex(
                name: "IX_TblFeature_Name",
                schema: "App",
                table: "TblFeature",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TblFeature_ProjectId",
                schema: "App",
                table: "TblFeature",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_TblFeature_ProjectId_Name",
                schema: "App",
                table: "TblFeature",
                columns: new[] { "ProjectId", "Name" },
                unique: true,
                filter: "\"ArchivedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_TblFeatureEnvironment_EnvironmentId",
                schema: "App",
                table: "TblFeatureEnvironment",
                column: "EnvironmentId");

            migrationBuilder.CreateIndex(
                name: "IX_TblFeatureEnvironment_FeatureId_EnvironmentId",
                schema: "App",
                table: "TblFeatureEnvironment",
                columns: new[] { "FeatureId", "EnvironmentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TblFeatureStrategy_FeatureId_EnvironmentId",
                schema: "App",
                table: "TblFeatureStrategy",
                columns: new[] { "FeatureId", "EnvironmentId" });

            migrationBuilder.CreateIndex(
                name: "IX_TblFeatureStrategy_Type",
                schema: "App",
                table: "TblFeatureStrategy",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_TblProject_Name",
                schema: "App",
                table: "TblProject",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TblProjectMember_ProjectId_UserId",
                schema: "App",
                table: "TblProjectMember",
                columns: new[] { "ProjectId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TblProjectMember_UserId",
                schema: "App",
                table: "TblProjectMember",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_TblSegment_IsPublic",
                schema: "App",
                table: "TblSegment",
                column: "IsPublic");

            migrationBuilder.CreateIndex(
                name: "IX_TblSegment_Name",
                schema: "App",
                table: "TblSegment",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TblUser_Email",
                schema: "App",
                table: "TblUser",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TblUser_IsActive",
                schema: "App",
                table: "TblUser",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_TblUser_Role",
                schema: "App",
                table: "TblUser",
                column: "Role");

            migrationBuilder.CreateIndex(
                name: "IX_TblVariant_FeatureEnvironmentId",
                schema: "App",
                table: "TblVariant",
                column: "FeatureEnvironmentId");

            migrationBuilder.CreateIndex(
                name: "IX_TblVariant_StrategyId",
                schema: "App",
                table: "TblVariant",
                column: "StrategyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StrategyConstraints");

            migrationBuilder.DropTable(
                name: "TblApiToken",
                schema: "App");

            migrationBuilder.DropTable(
                name: "TblChangeRequest",
                schema: "App");

            migrationBuilder.DropTable(
                name: "TblChangeRequestItem",
                schema: "App");

            migrationBuilder.DropTable(
                name: "TblDataProtectionKey",
                schema: "Base");

            migrationBuilder.DropTable(
                name: "TblEnvironment",
                schema: "App");

            migrationBuilder.DropTable(
                name: "TblFeature",
                schema: "App");

            migrationBuilder.DropTable(
                name: "TblFeatureEnvironment",
                schema: "App");

            migrationBuilder.DropTable(
                name: "TblFeatureStrategy",
                schema: "App");

            migrationBuilder.DropTable(
                name: "TblProject",
                schema: "App");

            migrationBuilder.DropTable(
                name: "TblProjectMember",
                schema: "App");

            migrationBuilder.DropTable(
                name: "TblSegment",
                schema: "App");

            migrationBuilder.DropTable(
                name: "TblUser",
                schema: "App");

            migrationBuilder.DropTable(
                name: "TblVariant",
                schema: "App");
        }
    }
}
