namespace Application.DTOs.Payments;

public class TransactionDto
{
    public Guid Id { get; set; }
    public Guid BookingId { get; set; }
    public decimal Amount { get; set; }
    public decimal CommissionAmount { get; set; }
    public decimal ProviderEarning { get; set; }
    public string PaymentStatus { get; set; } = string.Empty;
    public string? TransactionReference { get; set; }
    public DateTime? PaidAt { get; set; }
}

public class RevenueReportDto
{
    public decimal TotalRevenue { get; set; }
    public decimal TotalCommission { get; set; }
    public decimal TotalProviderEarnings { get; set; }
    public int TotalTransactions { get; set; }
    public List<RevenueByPeriodDto> Breakdown { get; set; } = new();
}

public class RevenueByPeriodDto
{
    public string Period { get; set; } = string.Empty;
    public decimal Revenue { get; set; }
    public decimal Commission { get; set; }
    public int Transactions { get; set; }
}