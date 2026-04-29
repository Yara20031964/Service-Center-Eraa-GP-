namespace KHDMA.Application.DTOs.Admin
{
    public class ChatTranscriptDto
    {
        public Guid MessageId { get; set; }
        public string SenderName { get; set; }
        public string MessageText { get; set; }
        public string MessageType { get; set; }
        public DateTime SentAt { get; set; }
        public string? AttachmentUrl { get; set; }
    }
}
