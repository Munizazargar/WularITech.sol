using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WularItech_solutions.Migrations
{
    /// <inheritdoc />
    public partial class AddTechnicianPassword : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PasswordHash",
                table: "Technicians",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PasswordHash",
                table: "Technicians");
        }
    }
}
