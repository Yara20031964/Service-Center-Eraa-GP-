namespace KHDMA.Domain.Entities
{
    public class CustomerFavoriteProvider
    {
        public string CustomerId { get; set; }
        public string ProviderId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Customer Customer { get; set; }
        public Provider Provider { get; set; }
    }
}
