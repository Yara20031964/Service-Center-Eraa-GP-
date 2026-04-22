namespace KHDMA.Domain.Entities
{
    public class ProviderPortfolioImage
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string ProviderId { get; set; }
        public string ImageUrl { get; set; }

        public Provider Provider { get; set; }
    }
}
