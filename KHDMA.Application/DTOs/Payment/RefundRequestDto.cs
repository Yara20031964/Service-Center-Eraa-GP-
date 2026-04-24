namespace Application.DTOs.Payment;

public class RefundRequestDto
{
    public Guid PaymentId { get; set; }
    public int AmountCents { get; set; } // لو partial refund
}