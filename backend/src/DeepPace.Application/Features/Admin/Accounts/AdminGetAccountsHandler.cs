using DeepPace.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using DeepPace.Application.Common;
using DeepPace.Application.Features.Admin.Accounts.DTOs;
using DeepPace.Application.Interfaces;
using DeepPace.Domain.Enums;

namespace DeepPace.Application.Features.Admin.Accounts;

public record AdminGetAccountsRequest(
    string? Search = null,
    AccountStatus? Status = null,
    int Page = 1,
    int PageSize = 20);

public class AdminGetAccountsHandler : IUseCase<AdminGetAccountsRequest, AdminPagedResult<AdminAccountSummaryDto>>
{
    private readonly IDeepPaceDbContext _dbContext;

    public AdminGetAccountsHandler(IDeepPaceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<AdminPagedResult<AdminAccountSummaryDto>>> ExecuteAsync(
        AdminGetAccountsRequest request,
        CancellationToken cancellationToken = default)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize < 1 ? 20 : (request.PageSize > 100 ? 100 : request.PageSize);

        var query = _dbContext.Users.AsNoTracking().Where(u => !u.IsDeleted);

        if (request.Status.HasValue)
        {
            query = query.Where(u => u.Status == request.Status.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            if (Guid.TryParse(search, out var searchGuid))
            {
                query = query.Where(u => u.Id == searchGuid || u.Email.ToLower().Contains(search) || u.Name.ToLower().Contains(search));
            }
            else
            {
                query = query.Where(u => u.Email.ToLower().Contains(search) || u.Name.ToLower().Contains(search));
            }
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(u => u.CreatedAt)
            .ThenByDescending(u => u.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new AdminAccountSummaryDto(
                u.Id,
                u.Email,
                u.Name,
                u.AvatarUrl,
                u.Status,
                u.CreatedAt,
                new RoleDescriptorDto(u.Role!.Id, u.Role.Key, u.Role.NameEn, u.Role.NameVi)))
            .ToListAsync(cancellationToken);

        return new AdminPagedResult<AdminAccountSummaryDto>(items, totalCount, page, pageSize);
    }
}
