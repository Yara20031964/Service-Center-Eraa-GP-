using Application.DTOs.Admin;
using Domain.Common;

namespace Application.Services.Admin;

public interface IAdminCustomerService
{
    Task<PagedResponse<CustomerDto>> GetAllCustomersAsync(
        string? search, int page, int pageSize);

    Task<ApiResponse<CustomerDto>> GetCustomerByIdAsync(string id);

    Task<ApiResponse<string>> SuspendCustomerAsync(string id);
    Task<ApiResponse<string>> BanCustomerAsync(string id);
    Task<ApiResponse<string>> RestoreCustomerAsync(string id);
}