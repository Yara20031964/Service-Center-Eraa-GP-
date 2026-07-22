using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KHDMA.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBookingLifecycleAndDispatch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Notifications_Bookings_BookingId",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_ProviderServices_ServiceId",
                table: "ProviderServices");

            migrationBuilder.AddColumn<string>(
                name: "ProviderReply",
                table: "Reviews",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ProviderReplyAt",
                table: "Reviews",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Balance",
                table: "Providers",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalEarnings",
                table: "Providers",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ServiceFee",
                table: "Payments",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "VatAmount",
                table: "Payments",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AlterColumn<Guid>(
                name: "BookingId",
                table: "Notifications",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AlterColumn<string>(
                name: "ProviderId",
                table: "Bookings",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AddColumn<DateTime>(
                name: "AcceptedAt",
                table: "Bookings",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ArrivedAt",
                table: "Bookings",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CancelledAt",
                table: "Bookings",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedAt",
                table: "Bookings",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DispatchDeadline",
                table: "Bookings",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "DispatchRadiusKm",
                table: "Bookings",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<int>(
                name: "DispatchRoundCount",
                table: "Bookings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "EnRouteAt",
                table: "Bookings",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ProviderNotifiedAt",
                table: "Bookings",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartedAt",
                table: "Bookings",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "AuditLogs",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateTable(
                name: "BookingStatusHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BookingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FromStatus = table.Column<int>(type: "int", nullable: false),
                    ToStatus = table.Column<int>(type: "int", nullable: false),
                    ChangedByUserId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ChangedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookingStatusHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BookingStatusHistories_Bookings_BookingId",
                        column: x => x.BookingId,
                        principalTable: "Bookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Payouts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProviderId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PaidAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Reference = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payouts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Payouts_Providers_ProviderId",
                        column: x => x.ProviderId,
                        principalTable: "Providers",
                        principalColumn: "ApplicationUserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProviderServices_ServiceId_IsActive",
                table: "ProviderServices",
                columns: new[] { "ServiceId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_Providers_CurrentLatitude_CurrentLongitude",
                table: "Providers",
                columns: new[] { "CurrentLatitude", "CurrentLongitude" });

            migrationBuilder.CreateIndex(
                name: "IX_Providers_State_AvailabilityStatus",
                table: "Providers",
                columns: new[] { "State", "AvailabilityStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_Payments_PaymentStatus_PaidAt",
                table: "Payments",
                columns: new[] { "PaymentStatus", "PaidAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId",
                table: "Notifications",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_BookingId_SentAt",
                table: "ChatMessages",
                columns: new[] { "BookingId", "SentAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_CustomerId_Status",
                table: "Bookings",
                columns: new[] { "CustomerId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_ProviderId_Status",
                table: "Bookings",
                columns: new[] { "ProviderId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_ScheduledTime",
                table: "Bookings",
                column: "ScheduledTime");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_Status",
                table: "Bookings",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_Status_CreateAt",
                table: "Bookings",
                columns: new[] { "Status", "CreateAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_Status_DispatchDeadline",
                table: "Bookings",
                columns: new[] { "Status", "DispatchDeadline" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_UserId_Timestamp",
                table: "AuditLogs",
                columns: new[] { "UserId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_BookingStatusHistories_BookingId_ChangedAt",
                table: "BookingStatusHistories",
                columns: new[] { "BookingId", "ChangedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Payouts_ProviderId_Status",
                table: "Payouts",
                columns: new[] { "ProviderId", "Status" });

            migrationBuilder.AddForeignKey(
                name: "FK_Notifications_Bookings_BookingId",
                table: "Notifications",
                column: "BookingId",
                principalTable: "Bookings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Notifications_Bookings_BookingId",
                table: "Notifications");

            migrationBuilder.DropTable(
                name: "BookingStatusHistories");

            migrationBuilder.DropTable(
                name: "Payouts");

            migrationBuilder.DropIndex(
                name: "IX_ProviderServices_ServiceId_IsActive",
                table: "ProviderServices");

            migrationBuilder.DropIndex(
                name: "IX_Providers_CurrentLatitude_CurrentLongitude",
                table: "Providers");

            migrationBuilder.DropIndex(
                name: "IX_Providers_State_AvailabilityStatus",
                table: "Providers");

            migrationBuilder.DropIndex(
                name: "IX_Payments_PaymentStatus_PaidAt",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_UserId",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_ChatMessages_BookingId_SentAt",
                table: "ChatMessages");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_CustomerId_Status",
                table: "Bookings");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_ProviderId_Status",
                table: "Bookings");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_ScheduledTime",
                table: "Bookings");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_Status",
                table: "Bookings");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_Status_CreateAt",
                table: "Bookings");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_Status_DispatchDeadline",
                table: "Bookings");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_UserId_Timestamp",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "ProviderReply",
                table: "Reviews");

            migrationBuilder.DropColumn(
                name: "ProviderReplyAt",
                table: "Reviews");

            migrationBuilder.DropColumn(
                name: "Balance",
                table: "Providers");

            migrationBuilder.DropColumn(
                name: "TotalEarnings",
                table: "Providers");

            migrationBuilder.DropColumn(
                name: "ServiceFee",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "VatAmount",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "AcceptedAt",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "ArrivedAt",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "CancelledAt",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "CompletedAt",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "DispatchDeadline",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "DispatchRadiusKm",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "DispatchRoundCount",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "EnRouteAt",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "ProviderNotifiedAt",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "StartedAt",
                table: "Bookings");

            migrationBuilder.AlterColumn<Guid>(
                name: "BookingId",
                table: "Notifications",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ProviderId",
                table: "Bookings",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "AuditLogs",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.CreateIndex(
                name: "IX_ProviderServices_ServiceId",
                table: "ProviderServices",
                column: "ServiceId");

            migrationBuilder.AddForeignKey(
                name: "FK_Notifications_Bookings_BookingId",
                table: "Notifications",
                column: "BookingId",
                principalTable: "Bookings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
