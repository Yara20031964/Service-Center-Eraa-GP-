namespace KHDMA.Domain.Entities
{
    public class CustomerFavorite
    {
        public string CustomerId { get; set; }
        public Guid ServiceId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Customer Customer { get; set; }
        public Service Service { get; set; }
    }
}
