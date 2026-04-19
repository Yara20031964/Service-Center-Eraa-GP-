using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KHDMA.Application.DTOs.Auth.Response
{
    public class AuthResponseDto
    {
        public TokenResponseDto Token { get; set; }
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }

        public static AuthResponseDto Ok (TokenResponseDto token) => new()
        { 
            IsSuccess = true, Token = token 
        };

        public static AuthResponseDto Fail (string errorMessage) => new()
        { 
            IsSuccess = false, ErrorMessage = errorMessage 
        };
    }
}
