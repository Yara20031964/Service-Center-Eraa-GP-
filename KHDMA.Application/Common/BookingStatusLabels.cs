using KHDMA.Domain.Enums;

namespace KHDMA.Application.Common
{
    /// <summary>
    /// Single source of truth for the bilingual labels shipped alongside every
    /// status value (docs/API_CONTRACTS.md section 5).
    /// </summary>
    /// <remarks>
    /// The machine value and the label are sent as separate fields on purpose:
    /// the client switches on <c>status</c> and displays the label. Duplicating
    /// this table in Dart is how the two clients drift apart.
    /// </remarks>
    public static class BookingStatusLabels
    {
        private static readonly Dictionary<BookingStatus, (string En, string Ar)> Map = new()
        {
            // "Paid" rather than "Pending": by the time the customer sees a booking
            // in this state, payment has been taken and dispatch has not yet started.
            [BookingStatus.Pending]         = ("Paid",              "تم الدفع"),
            [BookingStatus.Dispatching]     = ("Searching...",      "جاري البحث"),
            [BookingStatus.Accepted]        = ("Accepted",          "تم القبول"),
            [BookingStatus.EnRoute]         = ("En Route",          "في الطريق"),
            [BookingStatus.Arrived]         = ("Arrived",           "وصل"),
            [BookingStatus.InProgress]      = ("In Progress",       "جاري التنفيذ"),
            [BookingStatus.Completed]       = ("Completed",         "مكتمل"),
            [BookingStatus.Cancelled]       = ("Cancelled",         "ملغي"),
            [BookingStatus.NoProviderFound] = ("No Provider Found", "لا يوجد مزود"),
            [BookingStatus.Failed]          = ("Failed",            "فشل"),
        };

        public static string En(BookingStatus status)
            => Map.TryGetValue(status, out var v) ? v.En : status.ToString();

        public static string Ar(BookingStatus status)
            => Map.TryGetValue(status, out var v) ? v.Ar : status.ToString();
    }
}
