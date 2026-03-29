namespace Application.DTOs.Admin;

public class CommissionDto
{
    public decimal Rate { get; set; }
    public DateTime LastUpdatedAt { get; set; }
    public string UpdatedBy { get; set; } = string.Empty;
}

public class UpdateCommissionDto
{
    public decimal Rate { get; set; }
}