using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Codenex.Migrations
{
    /// <inheritdoc />
    public partial class MakeUserIdNullableForDeletedUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CommentLikes_AspNetUsers_UserId",
                table: "CommentLikes");

            migrationBuilder.DropForeignKey(
                name: "FK_PublicationComments_AspNetUsers_UserId",
                table: "PublicationComments");

            migrationBuilder.DropForeignKey(
                name: "FK_PublicationRatings_AspNetUsers_UserId",
                table: "PublicationRatings");

            migrationBuilder.DropIndex(
                name: "IX_PublicationRatings_UserId_PublicationId",
                table: "PublicationRatings");

            migrationBuilder.DropIndex(
                name: "IX_CommentLikes_UserId_CommentId",
                table: "CommentLikes");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "PublicationRatings",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "PublicationComments",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "CommentLikes",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.CreateIndex(
                name: "IX_PublicationRatings_UserId_PublicationId",
                table: "PublicationRatings",
                columns: new[] { "UserId", "PublicationId" },
                unique: true,
                filter: "[UserId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CommentLikes_UserId_CommentId",
                table: "CommentLikes",
                columns: new[] { "UserId", "CommentId" },
                unique: true,
                filter: "[UserId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_CommentLikes_AspNetUsers_UserId",
                table: "CommentLikes",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_PublicationComments_AspNetUsers_UserId",
                table: "PublicationComments",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_PublicationRatings_AspNetUsers_UserId",
                table: "PublicationRatings",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CommentLikes_AspNetUsers_UserId",
                table: "CommentLikes");

            migrationBuilder.DropForeignKey(
                name: "FK_PublicationComments_AspNetUsers_UserId",
                table: "PublicationComments");

            migrationBuilder.DropForeignKey(
                name: "FK_PublicationRatings_AspNetUsers_UserId",
                table: "PublicationRatings");

            migrationBuilder.DropIndex(
                name: "IX_PublicationRatings_UserId_PublicationId",
                table: "PublicationRatings");

            migrationBuilder.DropIndex(
                name: "IX_CommentLikes_UserId_CommentId",
                table: "CommentLikes");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "PublicationRatings",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "PublicationComments",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "CommentLikes",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PublicationRatings_UserId_PublicationId",
                table: "PublicationRatings",
                columns: new[] { "UserId", "PublicationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CommentLikes_UserId_CommentId",
                table: "CommentLikes",
                columns: new[] { "UserId", "CommentId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_CommentLikes_AspNetUsers_UserId",
                table: "CommentLikes",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PublicationComments_AspNetUsers_UserId",
                table: "PublicationComments",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PublicationRatings_AspNetUsers_UserId",
                table: "PublicationRatings",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
