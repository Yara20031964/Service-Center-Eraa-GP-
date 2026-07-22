using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KHDMA.Application.DTOs
{
    public class SendNotificationDto
    {
        public string UserId { get; set; }

        /// <summary>Optional - omit for account-level notices not tied to a booking.</summary>
        public Guid? BookingId { get; set; }

        public string Title { get; set; }

        public string Body { get; set; }

        public string Type { get; set; }
    }
}
