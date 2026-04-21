using KHDMA.Domain.Enums;

namespace KHDMA.Application.DTOs.Auth.Request
{
    public class RegisterCustomerDto : BaseRegisterDto
    {
        public RegisterCustomerDto()
        {
            Role = UserRole.Customer;
        }
    }
}
