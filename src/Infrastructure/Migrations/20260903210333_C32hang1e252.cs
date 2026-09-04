using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class C32hang1e252 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddForeignKey(
                name: "FK_TblApiToken_TblEnvironment_EnvironmentId",
                schema: "App",
                table: "TblApiToken",
                column: "EnvironmentId",
                principalSchema: "App",
                principalTable: "TblEnvironment",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TblChangeRequest_TblEnvironment_EnvironmentId",
                schema: "App",
                table: "TblChangeRequest",
                column: "EnvironmentId",
                principalSchema: "App",
                principalTable: "TblEnvironment",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TblChangeRequest_TblProject_ProjectId",
                schema: "App",
                table: "TblChangeRequest",
                column: "ProjectId",
                principalSchema: "App",
                principalTable: "TblProject",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TblApiToken_TblEnvironment_EnvironmentId",
                schema: "App",
                table: "TblApiToken");

            migrationBuilder.DropForeignKey(
                name: "FK_TblChangeRequest_TblEnvironment_EnvironmentId",
                schema: "App",
                table: "TblChangeRequest");

            migrationBuilder.DropForeignKey(
                name: "FK_TblChangeRequest_TblProject_ProjectId",
                schema: "App",
                table: "TblChangeRequest");
        }
    }
}
