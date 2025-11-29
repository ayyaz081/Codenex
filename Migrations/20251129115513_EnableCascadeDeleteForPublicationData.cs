using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Neelsol.Migrations
{
    /// <inheritdoc />
    public partial class EnableCascadeDeleteForPublicationData : Migration
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
        }
    }
}
