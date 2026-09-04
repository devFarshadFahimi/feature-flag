using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Change252 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "FeatureEnvironmentId",
                schema: "App",
                table: "TblFeatureStrategy",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "Tags",
                schema: "App",
                table: "TblFeature",
                type: "jsonb",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_TblFeatureStrategy_EnvironmentId",
                schema: "App",
                table: "TblFeatureStrategy",
                column: "EnvironmentId");

            migrationBuilder.CreateIndex(
                name: "IX_TblFeatureStrategy_FeatureEnvironmentId",
                schema: "App",
                table: "TblFeatureStrategy",
                column: "FeatureEnvironmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_TblFeatureEnvironment_TblFeature_FeatureId",
                schema: "App",
                table: "TblFeatureEnvironment",
                column: "FeatureId",
                principalSchema: "App",
                principalTable: "TblFeature",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TblFeatureStrategy_TblEnvironment_EnvironmentId",
                schema: "App",
                table: "TblFeatureStrategy",
                column: "EnvironmentId",
                principalSchema: "App",
                principalTable: "TblEnvironment",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TblFeatureStrategy_TblFeatureEnvironment_FeatureEnvironment~",
                schema: "App",
                table: "TblFeatureStrategy",
                column: "FeatureEnvironmentId",
                principalSchema: "App",
                principalTable: "TblFeatureEnvironment",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TblFeatureStrategy_TblFeature_FeatureId",
                schema: "App",
                table: "TblFeatureStrategy",
                column: "FeatureId",
                principalSchema: "App",
                principalTable: "TblFeature",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TblVariant_TblFeatureEnvironment_FeatureEnvironmentId",
                schema: "App",
                table: "TblVariant",
                column: "FeatureEnvironmentId",
                principalSchema: "App",
                principalTable: "TblFeatureEnvironment",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TblFeatureEnvironment_TblFeature_FeatureId",
                schema: "App",
                table: "TblFeatureEnvironment");

            migrationBuilder.DropForeignKey(
                name: "FK_TblFeatureStrategy_TblEnvironment_EnvironmentId",
                schema: "App",
                table: "TblFeatureStrategy");

            migrationBuilder.DropForeignKey(
                name: "FK_TblFeatureStrategy_TblFeatureEnvironment_FeatureEnvironment~",
                schema: "App",
                table: "TblFeatureStrategy");

            migrationBuilder.DropForeignKey(
                name: "FK_TblFeatureStrategy_TblFeature_FeatureId",
                schema: "App",
                table: "TblFeatureStrategy");

            migrationBuilder.DropForeignKey(
                name: "FK_TblVariant_TblFeatureEnvironment_FeatureEnvironmentId",
                schema: "App",
                table: "TblVariant");

            migrationBuilder.DropIndex(
                name: "IX_TblFeatureStrategy_EnvironmentId",
                schema: "App",
                table: "TblFeatureStrategy");

            migrationBuilder.DropIndex(
                name: "IX_TblFeatureStrategy_FeatureEnvironmentId",
                schema: "App",
                table: "TblFeatureStrategy");

            migrationBuilder.DropColumn(
                name: "FeatureEnvironmentId",
                schema: "App",
                table: "TblFeatureStrategy");

            migrationBuilder.DropColumn(
                name: "Tags",
                schema: "App",
                table: "TblFeature");
        }
    }
}
