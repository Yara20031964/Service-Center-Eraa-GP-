
namespace KHDMA.Domain.Entities
{
    public class Customer
    {
        public string ApplicationUserId { get; set; }

        //Navigation properties
        public ApplicationUser ApplicationUser { get; set; }
        public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
        public ICollection<Review> Reviews { get; set; } = new List<Review>();
        public ICollection<Address> Addresses { get; set; } = new List<Address>();

    }
}