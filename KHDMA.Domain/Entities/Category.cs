namespace KHDMA.Domain.Entities
{
    public class Category
    {
        public Guid id { get; set; } = Guid.NewGuid();
        public string NameEn { get; set; }
        public string NameAr { get; set; }
        public string? Description { get; set; }
        public string ? IconUrl { get; set; }
        public bool IsActive { get; set; } = true;

        //Navigation properties
        public ICollection<Service> Services { get; set; } = new List<Service>();
    }
}