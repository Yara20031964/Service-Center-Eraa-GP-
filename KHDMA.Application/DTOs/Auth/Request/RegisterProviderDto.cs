using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KHDMA.Application.DTOs.Auth.Request
{
    public class RegisterProviderDto
    {
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string PhoneNumber { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public decimal? HourlyRate { get; set; }
        public string? ServiceArea { get; set; }
    }
}
