namespace KHDMA.Domain.Entities
{
    public class ServiceImage
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ServiceId { get; set; }
        public string ImageUrl { get; set; }

        public Service Service { get; set; }
    }
}
