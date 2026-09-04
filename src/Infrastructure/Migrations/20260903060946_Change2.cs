using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Change2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Lifecycle",
                schema: "App",
                table: "TblFeature",
                newName: "LifeCycle");

            migrationBuilder.RenameIndex(
                name: "IX_TblFeature_Lifecycle",
                schema: "App",
                table: "TblFeature",
                newName: "IX_TblFeature_LifeCycle");

            migrationBuilder.AddForeignKey(
                name: "FK_TblFeature_TblProject_ProjectId",
                schema: "App",
                table: "TblFeature",
                column: "ProjectId",
                principalSchema: "App",
                principalTable: "TblProject",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TblProjectMember_TblProject_ProjectId",
                schema: "App",
                table: "TblProjectMember",
                column: "ProjectId",
                principalSchema: "App",
                principalTable: "TblProject",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TblFeature_TblProject_ProjectId",
                schema: "App",
                table: "TblFeature");

            migrationBuilder.DropForeignKey(
                name: "FK_TblProjectMember_TblProject_ProjectId",
                schema: "App",
                table: "TblProjectMember");

            migrationBuilder.RenameColumn(
                name: "LifeCycle",
                schema: "App",
                table: "TblFeature",
                newName: "Lifecycle");

            migrationBuilder.RenameIndex(
                name: "IX_TblFeature_LifeCycle",
                schema: "App",
                table: "TblFeature",
                newName: "IX_TblFeature_Lifecycle");
        }
    }
}
