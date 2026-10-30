using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddQuestionBank : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BankQuestions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    QuestionTextAr = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    QuestionTextEn = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    QuestionType = table.Column<int>(type: "int", nullable: false),
                    Marks = table.Column<int>(type: "int", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Difficulty = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BankQuestions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BankQuestionOptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BankQuestionId = table.Column<int>(type: "int", nullable: false),
                    OptionTextAr = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OptionTextEn = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsCorrect = table.Column<bool>(type: "bit", nullable: false),
                    Order = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BankQuestionOptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BankQuestionOptions_BankQuestions_BankQuestionId",
                        column: x => x.BankQuestionId,
                        principalTable: "BankQuestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BankQuestionOptions_BankQuestionId",
                table: "BankQuestionOptions",
                column: "BankQuestionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BankQuestionOptions");

            migrationBuilder.DropTable(
                name: "BankQuestions");
        }
    }
}
