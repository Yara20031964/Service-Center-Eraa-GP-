using Application.DTOs.Admin;
using Domain.Common;
using KHDMA.Application.Interfaces.Repositories;
using KHDMA.Domain.Entities;

namespace Application.Services.Admin;

public class CommissionService : ICommissionService
{
    private readonly IUnitOfWork _unitOfWork;

    public CommissionService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ApiResponse<CommissionDto>> GetCurrentRateAsync()
    {
        var settings = await _unitOfWork.Repository<CommissionSettings>()
            .GetOneAsync(c => c.Id == 1);

        if (settings is null)
            return ApiResponse<CommissionDto>.NotFound("Commission settings not found");

        return ApiResponse<CommissionDto>.Ok(new CommissionDto
        {
            Rate = settings.Rate,
            LastUpdatedAt = settings.LastUpdatedAt,
            UpdatedBy = settings.UpdatedBy
        });
    }

    public async Task<ApiResponse<CommissionDto>> UpdateRateAsync(
        UpdateCommissionDto dto, string updatedByAdminId)
    {
        if (dto.Rate <= 0 || dto.Rate >= 1)
            return ApiResponse<CommissionDto>.Fail(
                "Rate must be between 0 and 1 (e.g. 0.15 for 15%)");

        var settings = await _unitOfWork.Repository<CommissionSettings>()
            .GetOneAsync(c => c.Id == 1);

        if (settings is null)
            return ApiResponse<CommissionDto>.NotFound("Commission settings not found");

        settings.Rate = dto.Rate;
        settings.LastUpdatedAt = DateTime.UtcNow;
        settings.UpdatedBy = updatedByAdminId;

        _unitOfWork.Repository<CommissionSettings>().Update(settings);
        await _unitOfWork.CommitAsync();

        return ApiResponse<CommissionDto>.Ok(new CommissionDto
        {
            Rate = settings.Rate,
            LastUpdatedAt = settings.LastUpdatedAt,
            UpdatedBy = settings.UpdatedBy
        });
    }
}