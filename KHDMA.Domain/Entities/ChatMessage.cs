namespace KHDMA.Domain.Entities
{
    public class ChatMessage
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid BookingId { get; set; }
        public string SenderId { get; set; }
        public string MessageType { get; set; }
        public string? MessageText { get; set; }
        public string? AttachmentUrl { get; set; }
        public DateTime SentAt { get; set; } = DateTime.UtcNow;
        public bool IsRead { get; set; } = false;

        //Navigation properties
        public Booking Booking { get; set; }
        public ApplicationUser Sender { get; set; }
}
}