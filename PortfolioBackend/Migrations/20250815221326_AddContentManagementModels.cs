using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortfolioBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddContentManagementModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AboutPageContents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    FoundedYear = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    CompanyLocation = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    MissionStatement = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    VisionStatement = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    CompanyDescription = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    ValuesTitle = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Value1Title = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Value1Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Value2Title = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Value2Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Value3Title = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Value3Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AboutPageContents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "HomepageContents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ServicesTitle = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ServicesSubtitle = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ServicesDescription = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    WhyChooseTitle = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    WhyChooseContent = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    KeyPoint1 = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    KeyPoint2 = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    KeyPoint3 = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HomepageContents", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AboutPageContents");

            migrationBuilder.DropTable(
                name: "HomepageContents");
        }
    }
}
