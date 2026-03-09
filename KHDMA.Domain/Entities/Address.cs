namespace KHDMA.Domain.Entities
{
    public class Address
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string CustomerId { get; set; }
        public string Label { get; set; } // e.g., "Home", "Work"
        public string AddresssLine { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }

        //Navigation properties
        public Customer Customer { get; set; }

    }
}