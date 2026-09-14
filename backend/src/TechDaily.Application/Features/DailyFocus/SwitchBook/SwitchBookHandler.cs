using FluentValidation;
using Microsoft.EntityFrameworkCore;
using TechDaily.Application.Common;
using TechDaily.Application.Features.DailyFocus.DTOs;
using TechDaily.Application.Interfaces;
using TechDaily.Domain.Entities;
using TechDaily.Domain.Enums;

namespace TechDaily.Application.Features.DailyFocus.SwitchBook;

public record SwitchBookRequest(Guid UserId, Guid BookId);

public class SwitchBookValidator : AbstractValidator<SwitchBookRequest>
{
    public SwitchBookValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.BookId).NotEmpty();
    }
}

public class SwitchBookHandler : IUseCase<SwitchBookRequest, PacerDto>
{
    private readonly ITechDailyDbContext _dbContext;
    private readonly IValidator<SwitchBookRequest> _validator;

    public SwitchBookHandler(
        ITechDailyDbContext dbContext,
        IValidator<SwitchBookRequest> validator)
    {
        _dbContext = dbContext;
        _validator = validator;
    }

    public async Task<Result<PacerDto>> ExecuteAsync(
        SwitchBookRequest request,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            var firstError = validationResult.Errors.First().ErrorMessage;
            return Error.Custom("Validation.Failed", firstError);
        }

        var targetBook = await _dbContext.DocumentBooks
            .FirstOrDefaultAsync(b => b.Id == request.BookId && b.Status == ProcessingStatus.Ready, cancellationToken);

        if (targetBook == null)
        {
            return Error.NotFound;
        }

        // Deactivate existing pacers for this user
        var existingPacers = await _dbContext.UserBookPacers
            .Where(p => p.UserId == request.UserId)
            .ToListAsync(cancellationToken);

        foreach (var p in existingPacers)
        {
            p.IsActive = false;
        }

        var activePacer = existingPacers.FirstOrDefault(p => p.DocumentBookId == targetBook.Id);
        if (activePacer == null)
        {
            activePacer = new UserBookPacer
            {
                UserId = request.UserId,
                DocumentBookId = targetBook.Id,
                CurrentChunkOrder = 1,
                DailyPaceChunks = 1,
                IsActive = true,
                LastReadDate = DateOnly.FromDateTime(DateTime.UtcNow)
            };
            await _dbContext.UserBookPacers.AddAsync(activePacer, cancellationToken);
            existingPacers.Add(activePacer);
        }
        else
        {
            activePacer.IsActive = true;
            activePacer.LastReadDate = DateOnly.FromDateTime(DateTime.UtcNow);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        // Fetch chunk title
        var activeChunk = await _dbContext.DocumentChunks
            .FirstOrDefaultAsync(c => c.DocumentBookId == targetBook.Id && c.ChunkOrder == activePacer.CurrentChunkOrder, cancellationToken);

        var allReadyBooks = await _dbContext.DocumentBooks
            .Where(b => b.Status == ProcessingStatus.Ready)
            .OrderByDescending(b => b.IsFeatured)
            .ThenBy(b => b.Title)
            .ToListAsync(cancellationToken);

        var pacerMap = existingPacers.ToDictionary(p => p.DocumentBookId);

        var availableBooks = allReadyBooks.Select(b =>
        {
            var p = pacerMap.GetValueOrDefault(b.Id);
            var currentOrder = p?.CurrentChunkOrder ?? 1;
            var pct = b.TotalChunks > 0 ? (int)Math.Round((double)currentOrder / b.TotalChunks * 100) : 0;
            return new PacerBookSummaryDto
            {
                Id = b.Id,
                Title = b.Title,
                TotalChunks = b.TotalChunks,
                CurrentChunkOrder = currentOrder,
                ProgressPercentage = Math.Clamp(pct, 0, 100),
                IsActive = b.Id == targetBook.Id
            };
        }).ToList();

        var total = targetBook.TotalChunks;
        var progressPct = total > 0 ? (int)Math.Round((double)activePacer.CurrentChunkOrder / total * 100) : 0;

        return new PacerDto
        {
            BookId = targetBook.Id,
            BookTitle = targetBook.Title,
            ChapterTitle = activeChunk?.ChapterTitle ?? "Chapter 1",
            CurrentChunkOrder = activePacer.CurrentChunkOrder,
            TotalChunks = total,
            ProgressPercentage = Math.Clamp(progressPct, 0, 100),
            HasPrevious = activePacer.CurrentChunkOrder > 1,
            HasNext = activePacer.CurrentChunkOrder < total,
            AvailableBooks = availableBooks
        };
    }
}
