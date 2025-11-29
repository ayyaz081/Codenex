using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Neelsol.Migrations
{
    /// <inheritdoc />
    public partial class RemoveAllCascadeDeletes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CommentLikes_PublicationComments_CommentId",
                table: "CommentLikes");

            migrationBuilder.DropForeignKey(
                name: "FK_PublicationComments_Publications_PublicationId",
                table: "PublicationComments");

            migrationBuilder.DropForeignKey(
                name: "FK_PublicationRatings_Publications_PublicationId",
                table: "PublicationRatings");

            migrationBuilder.DropForeignKey(
                name: "FK_Publications_Products_ProductId",
                table: "Publications");

            migrationBuilder.DropForeignKey(
                name: "FK_Publications_Solutions_SolutionId",
                table: "Publications");

            migrationBuilder.DropForeignKey(
                name: "FK_Repositories_Products_ProductId",
                table: "Repositories");

            migrationBuilder.AddForeignKey(
                name: "FK_CommentLikes_PublicationComments_CommentId",
                table: "CommentLikes",
                column: "CommentId",
                principalTable: "PublicationComments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PublicationComments_Publications_PublicationId",
                table: "PublicationComments",
                column: "PublicationId",
                principalTable: "Publications",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PublicationRatings_Publications_PublicationId",
                table: "PublicationRatings",
                column: "PublicationId",
                principalTable: "Publications",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Publications_Products_ProductId",
                table: "Publications",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Publications_Solutions_SolutionId",
                table: "Publications",
                column: "SolutionId",
                principalTable: "Solutions",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Repositories_Products_ProductId",
                table: "Repositories",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CommentLikes_PublicationComments_CommentId",
                table: "CommentLikes");

            migrationBuilder.DropForeignKey(
                name: "FK_PublicationComments_Publications_PublicationId",
                table: "PublicationComments");

            migrationBuilder.DropForeignKey(
                name: "FK_PublicationRatings_Publications_PublicationId",
                table: "PublicationRatings");

            migrationBuilder.DropForeignKey(
                name: "FK_Publications_Products_ProductId",
                table: "Publications");

            migrationBuilder.DropForeignKey(
                name: "FK_Publications_Solutions_SolutionId",
                table: "Publications");

            migrationBuilder.DropForeignKey(
                name: "FK_Repositories_Products_ProductId",
                table: "Repositories");

            migrationBuilder.AddForeignKey(
                name: "FK_CommentLikes_PublicationComments_CommentId",
                table: "CommentLikes",
                column: "CommentId",
                principalTable: "PublicationComments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PublicationComments_Publications_PublicationId",
                table: "PublicationComments",
                column: "PublicationId",
                principalTable: "Publications",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PublicationRatings_Publications_PublicationId",
                table: "PublicationRatings",
                column: "PublicationId",
                principalTable: "Publications",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Publications_Products_ProductId",
                table: "Publications",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id");

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
    }
}
