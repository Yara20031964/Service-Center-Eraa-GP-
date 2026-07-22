namespace Application.DTOs.Payment;

public class PaymentInitDto
{
    public Guid BookingId { get; set; }
}

public class PaymentKeyResponseDto
{
    public string PaymentKey { get; set; } = string.Empty;
    public string IframeUrl { get; set; } = string.Empty;
}