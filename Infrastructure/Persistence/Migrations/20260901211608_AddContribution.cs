using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddContribution : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "contributions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    Slug = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    UrlThumbnail = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    UrlPrContribution = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    State = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_contributions", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "contributions",
                columns: new[] { "Id", "Content", "CreatedAt", "DeletedAt", "Description", "Slug", "State", "Title", "UpdatedAt", "UrlPrContribution", "UrlThumbnail" },
                values: new object[,]
                {
                    { new Guid("22222222-2222-2222-2222-222222222222"), "Contenido del proyecto de portfolio...", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Sitio web de portfolio personal", "mi-portfolio", "Active", "Mi Portfolio", null, "https://example.com/img.png", "https://example.com/img.png" },
                    { new Guid("33333333-3333-3333-3333-333333333333"), "Documentación y endpoints del proyecto...", new DateTime(2026, 2, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "API REST para gestión de proyectos", "api-gestion", "Active", "API de Gestión", null, "https://example.com/api.png", "https://example.com/api.png" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_contributions_Slug",
                table: "contributions",
                column: "Slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "contributions");
        }
    }
}
