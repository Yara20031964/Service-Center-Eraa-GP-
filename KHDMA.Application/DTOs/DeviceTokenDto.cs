using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KHDMA.Application.DTOs
{
    public class DeviceTokenDto
    {
        public string UserId { get; set; }
        public string Token { get; set; }
        public string Platform { get; set; } // "android" | "ios" | "web"
    }
}
