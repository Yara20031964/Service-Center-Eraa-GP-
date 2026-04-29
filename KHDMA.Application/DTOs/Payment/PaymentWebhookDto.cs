namespace Application.DTOs.Payment;

public class PaymentWebhookDto
{
    public PaymobObj Obj { get; set; } = new();
    public string Type { get; set; } = string.Empty;
}

public class PaymobObj
{
    public bool Success { get; set; }
    public bool Is_Refund { get; set; }
    public string Order { get; set; } = string.Empty;
    public string Id { get; set; } = string.Empty;
    public PaymobAmountCents Amount_Cents { get; set; } = new();
}

public class PaymobAmountCents
{
    public int Amount_Cents { get; set; }
}