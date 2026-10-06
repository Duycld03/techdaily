using Microsoft.EntityFrameworkCore;
using TechDaily.Application.Common;
using TechDaily.Application.Interfaces;

namespace TechDaily.Application.Features.Library.SynthesizeAudio;

public class GetAudioQuotaHandler : IUseCase<GetAudioQuotaRequest, AudioQuotaResponse>
{
    private readonly ITechDailyDbContext _dbContext;

    public GetAudioQuotaHandler(ITechDailyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<AudioQuotaResponse>> ExecuteAsync(
        GetAudioQuotaRequest request,
        CancellationToken cancellationToken = default)
    {
        var startOfMonth = new DateTimeOffset(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, TimeSpan.Zero);
        var used = await _dbContext.DocumentChunkAudios
            .Where(a => a.CreatedAt >= startOfMonth)
            .SumAsync(a => (long)a.CharacterCount, cancellationToken);

        const long limit = GetOrSynthesizeChunkAudioHandler.MonthlyCharacterLimit;
        const long nearLimit = GetOrSynthesizeChunkAudioHandler.NearLimitThreshold;

        return new AudioQuotaResponse
        {
            MonthlyLimit = limit,
            UsedCharacters = used,
            RemainingCharacters = Math.Max(0, limit - used),
            IsNearLimit = used >= nearLimit,
            IsExhausted = used >= limit
        };
    }
}
