using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KHDMA.Application.DTOs
{
    public class NotificationPreferencesDto
    {
        public bool BookingAccepted { get; set; }
        public bool BookingCancelled { get; set; }
        public bool PaymentConfirmed { get; set; }
        public bool NewChatMessage { get; set; }
        public bool ProviderArrived { get; set; }
    }
}
