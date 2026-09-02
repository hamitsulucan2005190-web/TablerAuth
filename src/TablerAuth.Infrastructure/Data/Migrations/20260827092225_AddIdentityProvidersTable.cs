using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TablerAuth.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddIdentityProvidersTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "IdentityProviders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Scheme = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    ClientId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ClientSecretKey = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Authority = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Scopes = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    CallbackPath = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    Enabled = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdentityProviders", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IdentityProviders_Scheme",
                table: "IdentityProviders",
                column: "Scheme",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IdentityProviders");
        }
    }
}
