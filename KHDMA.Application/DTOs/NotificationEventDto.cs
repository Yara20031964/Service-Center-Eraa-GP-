using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KHDMA.Application.DTOs
{
    public class NotificationEventDto
    {
        public string EventType { get; set; }    //     "booking.accepted"
        public string TargetUserId { get; set; }
        public FCMPayloadDto Payload { get; set; }
        public string? Topic { get; set; }
    }
}
