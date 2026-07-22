using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KHDMA.Application.DTOs
{
    public class SendBroadcastDto
    {
        public string Title { get; set; }
        public string Body { get; set; }
        public string? ImageUrl { get; set; }
    }
}
