namespace Application.DTOs.Admin;

public class BannerDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int Order { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateBannerDto
{
    public string Title { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public int Order { get; set; } = 0;
}

public class CancellationPolicyDto
{
    public int FreeCancelWindowMinutes { get; set; }
    public decimal CancellationFee { get; set; }
    public DateTime LastUpdatedAt { get; set; }
    public string UpdatedBy { get; set; } = string.Empty;
}

public class UpdateCancellationPolicyDto
{
    public int FreeCancelWindowMinutes { get; set; }
    public decimal CancellationFee { get; set; }
}