namespace KHDMA.Domain.Entities
{
    public class Address
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string UserId { get; set; }
        public string Label { get; set; }
        public string AddresssLine { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }

        //Navigation properties
        public ApplicationUser User { get; set; }

    }
}