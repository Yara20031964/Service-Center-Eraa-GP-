namespace KHDMA.Domain.Entities;

public class CommissionSettings
{
    public int Id { get; set; } = 1; 
    public decimal Rate { get; set; } = 0.15m;
    public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;
    public string UpdatedBy { get; set; } = string.Empty; 
}