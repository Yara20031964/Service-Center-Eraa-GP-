using Microsoft.AspNetCore.Identity;
using KHDMA.Domain.Enums;


namespace KHDMA.Domain.Entities
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; }
        public string? ProfilePictureUrl { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public UserRole Role { get; set; }
        public UserStatus Status { get; set; }
        public DateTime CreateAt { get; set; } = DateTime.UtcNow;
        public bool IsDeleted { get; set; } = false;

        //Navigation properties
        public Customer? Customer { get; set; }
        public Provider? Provider { get; set; }
        public Admin? Admin { get; set; }
        public List<Notification> Notifications { get; set; } = new List<Notification>();
        public ICollection<Address> Addresses { get; set; } = new List<Address>();
    }
}