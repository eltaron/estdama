using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTraineeJobFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HireDate",
                table: "Trainees");

            migrationBuilder.AddColumn<string>(
                name: "Directorate",
                table: "Trainees",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "JobGrade",
                table: "Trainees",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "JobTitle",
                table: "Trainees",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Organization",
                table: "Trainees",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Directorate",
                table: "Trainees");

            migrationBuilder.DropColumn(
                name: "JobGrade",
                table: "Trainees");

            migrationBuilder.DropColumn(
                name: "JobTitle",
                table: "Trainees");

            migrationBuilder.DropColumn(
                name: "Organization",
                table: "Trainees");

            migrationBuilder.AddColumn<DateTime>(
                name: "HireDate",
                table: "Trainees",
                type: "datetime2",
                nullable: true);
        }
    }
}
