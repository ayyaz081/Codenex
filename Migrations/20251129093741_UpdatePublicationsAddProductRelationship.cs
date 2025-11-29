using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Neelsol.Migrations
{
    /// <inheritdoc />
    public partial class UpdatePublicationsAddProductRelationship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "SolutionId",
                table: "Publications",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "ProductId",
                table: "Publications",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Publications_ProductId",
                table: "Publications",
                column: "ProductId");

            migrationBuilder.AddForeignKey(
                name: "FK_Publications_Products_ProductId",
                table: "Publications",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Publications_Products_ProductId",
                table: "Publications");

            migrationBuilder.DropIndex(
                name: "IX_Publications_ProductId",
                table: "Publications");

            migrationBuilder.DropColumn(
                name: "ProductId",
                table: "Publications");

            migrationBuilder.AlterColumn<int>(
                name: "SolutionId",
                table: "Publications",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
        }
    }
}
