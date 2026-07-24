using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KHDMA.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SplitProviderWorkingLocation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "WorkingLatitude",
                table: "Providers",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "WorkingLongitude",
                table: "Providers",
                type: "float",
                nullable: true);

            // Every existing row's Current* pair was serving as the working point,
            // so carry it over. Without this each provider silently drops out of
            // dispatch the moment this deploys, until they next go online.
            migrationBuilder.Sql(@"
                UPDATE [Providers]
                SET [WorkingLatitude] = [CurrentLatitude],
                    [WorkingLongitude] = [CurrentLongitude]
                WHERE [CurrentLatitude] IS NOT NULL
                  AND [CurrentLongitude] IS NOT NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WorkingLatitude",
                table: "Providers");

            migrationBuilder.DropColumn(
                name: "WorkingLongitude",
                table: "Providers");
        }
    }
}
