using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KHDMA.Domain.Entities
{
    public class RefreshToken
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string UserId { get; set; }
        public string Token { get; set; }
        public DateTime Expires { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        //Revoked token can not be used anymore, even if it is not expired yet. This is useful for example when user logs out or when we want to invalidate a token for security reasons.
        bool IsRevoked { get; set; } = false;

        //Navigation
        public ApplicationUser User { get; set; }
    }
}
