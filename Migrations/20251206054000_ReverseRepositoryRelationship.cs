using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Codenex.Migrations
{
    /// <inheritdoc />
    public partial class ReverseRepositoryRelationship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Repositories_Products_ProductId",
                table: "Repositories");

            migrationBuilder.DropIndex(
                name: "IX_Repositories_ProductId",
                table: "Repositories");

            migrationBuilder.DropColumn(
                name: "ProductId",
                table: "Repositories");

            migrationBuilder.AddColumn<int>(
                name: "RepositoryId",
                table: "Solutions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RepositoryId",
                table: "Products",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Solutions_RepositoryId",
                table: "Solutions",
                column: "RepositoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_RepositoryId",
                table: "Products",
                column: "RepositoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_Products_Repositories_RepositoryId",
                table: "Products",
                column: "RepositoryId",
                principalTable: "Repositories",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Solutions_Repositories_RepositoryId",
                table: "Solutions",
                column: "RepositoryId",
                principalTable: "Repositories",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Products_Repositories_RepositoryId",
                table: "Products");

            migrationBuilder.DropForeignKey(
                name: "FK_Solutions_Repositories_RepositoryId",
                table: "Solutions");

            migrationBuilder.DropIndex(
                name: "IX_Solutions_RepositoryId",
                table: "Solutions");

            migrationBuilder.DropIndex(
                name: "IX_Products_RepositoryId",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "RepositoryId",
                table: "Solutions");

            migrationBuilder.DropColumn(
                name: "RepositoryId",
                table: "Products");

            migrationBuilder.AddColumn<int>(
                name: "ProductId",
                table: "Repositories",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Repositories_ProductId",
                table: "Repositories",
                column: "ProductId");

            migrationBuilder.AddForeignKey(
                name: "FK_Repositories_Products_ProductId",
                table: "Repositories",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
