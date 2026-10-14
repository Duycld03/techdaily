using FluentValidation;
using Microsoft.EntityFrameworkCore;
using DeepPace.Application.Common;
using DeepPace.Application.Features.Library.DTOs;
using DeepPace.Application.Interfaces;

namespace DeepPace.Application.Features.Library.GetBookStatus;

public record GetBookStatusRequest(Guid BookId, Guid? UserId = null);

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
            .FirstOrDefaultAsync(b => b.Id == request.BookId, cancellationToken);

        if (book == null)
        {
            return Error.NotFound;
        }

        if (request.UserId.HasValue && book.CreatedByUserId != null && book.CreatedByUserId != request.UserId.Value)
        {
            return Error.NotFound;
        }

        return new BookIngestionStatusDto
        {
            Id = book.Id,
            Status = book.Status,
            ProgressPercentage = book.ProgressPercentage,
            StatusMessage = book.StatusMessage,
            ErrorMessage = book.ErrorMessage,
            TotalChunks = book.TotalChunks
        };
    }
}
