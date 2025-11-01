using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeNex.Migrations
{
    /// <inheritdoc />
    public partial class AddProductRepositoryAndSolutionPublicationRelationships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Delete existing data to avoid foreign key conflicts
            // Order matters: delete child records first
            migrationBuilder.Sql("DELETE FROM [UserPurchases]");
            migrationBuilder.Sql("DELETE FROM [Payments]");
            migrationBuilder.Sql("DELETE FROM [PublicationRatings]");
            migrationBuilder.Sql("DELETE FROM [CommentLikes]");
            migrationBuilder.Sql("DELETE FROM [PublicationComments]");
            migrationBuilder.Sql("DELETE FROM [Repositories]");
            migrationBuilder.Sql("DELETE FROM [Publications]");

            migrationBuilder.AddColumn<int>(
                name: "ProductId",
                table: "Repositories",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SolutionId",
                table: "Publications",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Repositories_ProductId",
                table: "Repositories",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_Publications_SolutionId",
                table: "Publications",
                column: "SolutionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Publications_Solutions_SolutionId",
                table: "Publications",
                column: "SolutionId",
                principalTable: "Solutions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Repositories_Products_ProductId",
                table: "Repositories",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Publications_Solutions_SolutionId",
                table: "Publications");

            migrationBuilder.DropForeignKey(
                name: "FK_Repositories_Products_ProductId",
                table: "Repositories");

            migrationBuilder.DropIndex(
                name: "IX_Repositories_ProductId",
                table: "Repositories");

            migrationBuilder.DropIndex(
                name: "IX_Publications_SolutionId",
                table: "Publications");

            migrationBuilder.DropColumn(
                name: "ProductId",
                table: "Repositories");

            migrationBuilder.DropColumn(
                name: "SolutionId",
                table: "Publications");
        }
    }
}
