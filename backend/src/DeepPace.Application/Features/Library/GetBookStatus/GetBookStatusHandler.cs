using FluentValidation;
using Microsoft.EntityFrameworkCore;
using DeepPace.Application.Common;
using DeepPace.Application.Features.Library.DTOs;
using DeepPace.Application.Interfaces;

namespace DeepPace.Application.Features.Library.GetBookStatus;

public record GetBookStatusRequest(Guid BookId);

public class GetBookStatusValidator : AbstractValidator<GetBookStatusRequest>
{
    public GetBookStatusValidator()
    {
        RuleFor(x => x.BookId).NotEmpty();
    }
}

public class GetBookStatusHandler : IUseCase<GetBookStatusRequest, BookIngestionStatusDto>
{
    private readonly IDeepPaceDbContext _dbContext;
    private readonly IValidator<GetBookStatusRequest> _validator;

    public GetBookStatusHandler(
        IDeepPaceDbContext dbContext,
        IValidator<GetBookStatusRequest> validator)
    {
        _dbContext = dbContext;
        _validator = validator;
    }

    public async Task<Result<BookIngestionStatusDto>> ExecuteAsync(
        GetBookStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            var firstError = validationResult.Errors.First().ErrorMessage;
            return Error.Custom("Validation.Failed", firstError);
        }

        var book = await _dbContext.DocumentBooks
            .AsNoTracking()
            .Where(b => b.Id == request.BookId)
            .Select(b => new BookIngestionStatusDto
            {
                Id = b.Id,
                Status = b.Status,
                ProgressPercentage = b.ProgressPercentage,
                StatusMessage = b.StatusMessage,
                ErrorMessage = b.ErrorMessage,
                TotalChunks = b.TotalChunks
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (book == null)
        {
            return Error.NotFound;
        }

        return book;
    }
}
