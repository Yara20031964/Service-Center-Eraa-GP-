namespace KHDMA.Domain.Entities
{
    public class ProviderService
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string ProviderId { get; set; }
        public Guid ServiceId { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreateAt { get; set; } = DateTime.UtcNow;

        //Navigation properties
        public Provider Provider { get; set; }
        public Service Service { get; set; }
    }
}