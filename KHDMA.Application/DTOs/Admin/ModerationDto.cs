namespace Application.DTOs.Admin;

public class NotificationTemplateDto
{
    public Guid Id { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string TitleEn { get; set; } = string.Empty;
    public string TitleAr { get; set; } = string.Empty;
    public string BodyEn { get; set; } = string.Empty;
    public string BodyAr { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class UpdateNotificationTemplateDto
{
    public string TitleEn { get; set; } = string.Empty;
    public string TitleAr { get; set; } = string.Empty;
    public string BodyEn { get; set; } = string.Empty;
    public string BodyAr { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class HideReviewDto
{
    public string Reason { get; set; } = string.Empty;
}

public class BulkProviderActionDto
{
    public List<string> ProviderIds { get; set; } = new();
    public string? Reason { get; set; }
}