namespace KHDMA.Domain.Entities;

public class CancellationPolicy
{
    public int Id { get; set; } = 1; // single row
    public int FreeCancelWindowMinutes { get; set; } = 10;
    public decimal CancellationFee { get; set; } = 20;
    public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;
    public string UpdatedBy { get; set; } = string.Empty;
}