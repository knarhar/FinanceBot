using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanceBot.Migrations
{
    /// <inheritdoc />
    public partial class FixSpendingChatIdUniqueConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Spending_ChatId",
                table: "Spending");

            migrationBuilder.CreateIndex(
                name: "IX_Spending_ChatId",
                table: "Spending",
                column: "ChatId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Spending_ChatId",
                table: "Spending");

            migrationBuilder.CreateIndex(
                name: "IX_Spending_ChatId",
                table: "Spending",
                column: "ChatId",
                unique: true);
        }
    }
}
