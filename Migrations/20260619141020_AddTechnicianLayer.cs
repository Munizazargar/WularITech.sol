using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WularItech_solutions.Migrations
{
    /// <inheritdoc />
    public partial class AddTechnicianLayer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "TechnicianId",
                table: "Bookings",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Technicians",
                columns: table => new
                {
                    TechnicianId = table.Column<Guid>(type: "TEXT", nullable: false),
                    FullName = table.Column<string>(type: "TEXT", nullable: false),
                    Phone = table.Column<string>(type: "TEXT", nullable: false),
                    Skill = table.Column<string>(type: "TEXT", nullable: false),
                    Area = table.Column<string>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Technicians", x => x.TechnicianId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_TechnicianId",
                table: "Bookings",
                column: "TechnicianId");

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_Technicians_TechnicianId",
                table: "Bookings",
                column: "TechnicianId",
                principalTable: "Technicians",
                principalColumn: "TechnicianId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_Technicians_TechnicianId",
                table: "Bookings");

            migrationBuilder.DropTable(
                name: "Technicians");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_TechnicianId",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "TechnicianId",
                table: "Bookings");
        }
    }
}
