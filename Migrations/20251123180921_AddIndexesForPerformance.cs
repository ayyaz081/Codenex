using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Codenex.Migrations
{
    /// <inheritdoc />
    public partial class AddIndexesForPerformance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_TeamMembers_DisplayOrder",
                table: "TeamMembers",
                column: "DisplayOrder");

            migrationBuilder.CreateIndex(
                name: "IX_TeamMembers_IsActive",
                table: "TeamMembers",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_ClientTestimonials_DisplayOrder",
                table: "ClientTestimonials",
                column: "DisplayOrder");

            migrationBuilder.CreateIndex(
                name: "IX_ClientTestimonials_IsActive",
                table: "ClientTestimonials",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_ClientTestimonials_IsApproved",
                table: "ClientTestimonials",
                column: "IsApproved");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TeamMembers_DisplayOrder",
                table: "TeamMembers");

            migrationBuilder.DropIndex(
                name: "IX_TeamMembers_IsActive",
                table: "TeamMembers");

            migrationBuilder.DropIndex(
                name: "IX_ClientTestimonials_DisplayOrder",
                table: "ClientTestimonials");

            migrationBuilder.DropIndex(
                name: "IX_ClientTestimonials_IsActive",
                table: "ClientTestimonials");

            migrationBuilder.DropIndex(
                name: "IX_ClientTestimonials_IsApproved",
                table: "ClientTestimonials");
        }
    }
}
