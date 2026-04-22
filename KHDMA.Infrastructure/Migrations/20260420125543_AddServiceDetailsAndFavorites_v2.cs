using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KHDMA.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddServiceDetailsAndFavorites_v2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "CommissionSettings",
                keyColumn: "Id",
                keyValue: 1,
                column: "LastUpdatedAt",
                value: new DateTime(2026, 3, 29, 0, 0, 0, 0, DateTimeKind.Utc));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "CommissionSettings",
                keyColumn: "Id",
                keyValue: 1,
                column: "LastUpdatedAt",
                value: new DateTime(2026, 4, 20, 12, 53, 21, 815, DateTimeKind.Utc).AddTicks(537));
        }
    }
}
