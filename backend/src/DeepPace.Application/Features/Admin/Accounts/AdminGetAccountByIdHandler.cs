using Microsoft.EntityFrameworkCore;
using DeepPace.Application.Common;
using DeepPace.Application.Features.Admin.Accounts.DTOs;
using DeepPace.Application.Interfaces;

namespace DeepPace.Application.Features.Admin.Accounts;

public record AdminGetAccountByIdRequest(Guid Id);

public class AdminGetAccountByIdHandler : IUseCase<AdminGetAccountByIdRequest, AdminAccountDetailDto>
{
    private readonly IDeepPaceDbContext _dbContext;

    public AdminGetAccountByIdHandler(IDeepPaceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<AdminAccountDetailDto>> ExecuteAsync(
        AdminGetAccountByIdRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == request.Id && !u.IsDeleted, cancellationToken);

        if (user == null)
        {
            return Error.Custom("Account.NotFound", "Account not found.");
        }

        var dto = new AdminAccountDetailDto(
            user.Id,
            user.Email,
            user.Name,
            user.AvatarUrl,
            user.PreferredLocale,
            user.CreatedAt,
            user.Status,
            user.AdminControlVersion,
            !string.IsNullOrWhiteSpace(user.PasswordHash),
            !string.IsNullOrWhiteSpace(user.GoogleSubjectId),
            user.SuspendedByUserId,
            user.SuspendedAt,
            user.SuspensionReason);

        return dto;
    }
}
