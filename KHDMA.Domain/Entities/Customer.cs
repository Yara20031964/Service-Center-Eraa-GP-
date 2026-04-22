
namespace KHDMA.Domain.Entities
{
    public class Customer
    {
        public string ApplicationUserId { get; set; }

        //Navigation properties
        public ApplicationUser ApplicationUser { get; set; }
        public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
        public ICollection<Review> Reviews { get; set; } = new List<Review>();
        public ICollection<CustomerFavorite> Favorites { get; set; } = new List<CustomerFavorite>();
        public ICollection<CustomerFavoriteProvider> FavoriteProviders { get; set; } = new List<CustomerFavoriteProvider>();

    }
}