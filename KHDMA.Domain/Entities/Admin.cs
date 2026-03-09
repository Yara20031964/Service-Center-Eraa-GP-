namespace KHDMA.Domain.Entities
{
    public class Admin
    {
        public string ApplicationUserId { get; set; }
        //Navigation properties
        public ApplicationUser ApplicationUser { get; set; }
    }
}