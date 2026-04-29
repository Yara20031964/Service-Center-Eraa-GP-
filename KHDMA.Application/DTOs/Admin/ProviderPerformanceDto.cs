namespace KHDMA.Application.DTOs.Admin
{
    public class ProviderPerformanceDto
    {
        public string ProviderId { get; set; }
        public string ProviderName { get; set; }
        public double AverageRating { get; set; }
        public int TotalBookings { get; set; }
        public int CompletedBookings { get; set; }
        public int CancelledBookings { get; set; }
        public double CompletionRate { get; set; }
        public double CancellationRate { get; set; }
        public decimal TotalEarnings { get; set; }
        public decimal CurrentBalance { get; set; }
    }
}
