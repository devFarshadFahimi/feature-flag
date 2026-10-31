using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveEnvironmentApiTokens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TblApiToken",
                schema: "App");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TblApiToken",
                schema: "App",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EnvironmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsRevoked = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    LastUsedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    TokenHash = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    TokenType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TblApiToken", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TblApiToken_TblEnvironment_EnvironmentId",
                        column: x => x.EnvironmentId,
                        principalSchema: "App",
                        principalTable: "TblEnvironment",
                        principalColumn: "Id");
                });

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
        }
    }
}
