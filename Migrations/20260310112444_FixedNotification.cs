using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PCOMS.Migrations
{
    /// <inheritdoc />
    public partial class FixedNotification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "RejectedAt",
                table: "Timesheets",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectedBy",
                table: "Timesheets",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "Timesheets",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Timesheets_UserId",
                table: "Timesheets",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Timesheets_AspNetUsers_UserId",
                table: "Timesheets",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Timesheets_AspNetUsers_UserId",
                table: "Timesheets");

            migrationBuilder.DropIndex(
                name: "IX_Timesheets_UserId",
                table: "Timesheets");

            migrationBuilder.DropColumn(
                name: "RejectedAt",
                table: "Timesheets");

            migrationBuilder.DropColumn(
                name: "RejectedBy",
                table: "Timesheets");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "Timesheets");
        }
    }
}
