using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class C32hange252 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<List<Guid>>(
                name: "Reviewers",
                schema: "App",
                table: "TblChangeRequest",
                type: "uuid[]",
                nullable: false);

            migrationBuilder.AddForeignKey(
                name: "FK_TblChangeRequestItem_TblChangeRequest_ChangeRequestId",
                schema: "App",
                table: "TblChangeRequestItem",
                column: "ChangeRequestId",
                principalSchema: "App",
                principalTable: "TblChangeRequest",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TblChangeRequestItem_TblChangeRequest_ChangeRequestId",
                schema: "App",
                table: "TblChangeRequestItem");

            migrationBuilder.DropColumn(
                name: "Reviewers",
                schema: "App",
                table: "TblChangeRequest");
        }
    }
}
