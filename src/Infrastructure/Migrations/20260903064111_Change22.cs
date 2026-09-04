using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Change22 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddForeignKey(
                name: "FK_StrategyConstraints_TblSegment_SegmentId",
                table: "StrategyConstraints",
                column: "SegmentId",
                principalSchema: "App",
                principalTable: "TblSegment",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StrategyConstraints_TblSegment_SegmentId",
                table: "StrategyConstraints");
        }
    }
}
