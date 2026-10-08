using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using DeepPace.Application.Common;
using DeepPace.Application.Interfaces;
using DeepPace.Domain.Entities;

namespace DeepPace.Application.Features.Library.SynthesizeAudio;

public class GetOrSynthesizeChunkAudioHandler : IUseCase<SynthesizeChunkAudioRequest, SynthesizeChunkAudioResponse>
{
    public const long MonthlyCharacterLimit = 950_000;
    public const long NearLimitThreshold = 900_000;

    private readonly IDeepPaceDbContext _dbContext;
    private readonly IGoogleCloudTtsService _googleCloudTtsService;
    private readonly ILogger<GetOrSynthesizeChunkAudioHandler> _logger;

    public GetOrSynthesizeChunkAudioHandler(
        IDeepPaceDbContext dbContext,
        IGoogleCloudTtsService googleCloudTtsService,
        ILogger<GetOrSynthesizeChunkAudioHandler> logger)
    {
        _dbContext = dbContext;
        _googleCloudTtsService = googleCloudTtsService;
        _logger = logger;
    }

    public async Task<Result<SynthesizeChunkAudioResponse>> ExecuteAsync(
        SynthesizeChunkAudioRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.ChunkId == Guid.Empty)
        {
            return Error.Custom("INVALID_CHUNK_ID", "Chunk ID cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(request.VoiceId))
        {
            return Error.Custom("INVALID_VOICE_ID", "Voice ID cannot be empty.");
        }

        var chunk = await _dbContext.DocumentChunks
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == request.ChunkId, cancellationToken);

        if (chunk == null)
        {
            return Error.NotFound;
        }
        var isViChunk = (chunk.Language ?? "").StartsWith("vi", StringComparison.OrdinalIgnoreCase);
        var isViVoice = request.VoiceId.Trim().StartsWith("vi-", StringComparison.OrdinalIgnoreCase);
        if (isViChunk != isViVoice)
        {
            return Error.Custom("VOICE_LANGUAGE_MISMATCH",
                $"Voice '{request.VoiceId}' is incompatible with chunk language '{chunk.Language}'.");
        }


        var textToSynthesize = !string.IsNullOrWhiteSpace(request.NarrationScript)
            ? request.NarrationScript.Trim()
            : ExtractNarrationScript(chunk.OriginalTextMarkdown);

        if (string.IsNullOrWhiteSpace(textToSynthesize))
        {
            return Error.Custom("EMPTY_NARRATION_TEXT", "No narration text could be extracted from chunk.");
        }

        var contentHash = !string.IsNullOrWhiteSpace(request.ContentHash)
            ? request.ContentHash.Trim().ToLowerInvariant()
            : ComputeSha256Hex(textToSynthesize);

        // 1. Check database cache hit
        var cached = await _dbContext.DocumentChunkAudios
            .AsNoTracking()
            .FirstOrDefaultAsync(
                a => a.DocumentChunkId == chunk.Id && a.ContentHash == contentHash && a.VoiceId == request.VoiceId,
                cancellationToken);

        if (cached != null)
        {
            return new SynthesizeChunkAudioResponse
            {
                AudioBytes = cached.AudioData,
                MimeType = cached.MimeType,
                ContentHash = cached.ContentHash,
                VoiceId = cached.VoiceId,
                CharacterCount = cached.CharacterCount,
                DurationSeconds = cached.DurationSeconds,
                IsCacheHit = true
            };
        }

        // 2. Monthly character quota guard check
        var startOfMonth = new DateTimeOffset(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, TimeSpan.Zero);
        var usedCharacters = await _dbContext.DocumentChunkAudios
            .Where(a => a.CreatedAt >= startOfMonth)
            .SumAsync(a => (long)a.CharacterCount, cancellationToken);

        if (usedCharacters >= MonthlyCharacterLimit || (usedCharacters + textToSynthesize.Length) > MonthlyCharacterLimit)
        {
            _logger.LogWarning("Audio narration quota exhausted: used {Used} characters against limit {Limit}",
                usedCharacters, MonthlyCharacterLimit);
            return Error.AudioQuotaExhausted;
        }

        // 3. Synthesize via Google Cloud TTS
        var ttsResult = await _googleCloudTtsService.SynthesizeAsync(
            textToSynthesize,
            request.VoiceId,
            chunk.Language,
            cancellationToken);

        if (ttsResult.IsFailure)
        {
            return ttsResult.Error;
        }

        // 4. Save new audio to PostgreSQL
        var audioEntity = new DocumentChunkAudio
        {
            DocumentChunkId = chunk.Id,
            ContentHash = contentHash,
            VoiceId = request.VoiceId,
            MimeType = "audio/mpeg",
            AudioData = ttsResult.Value.AudioBytes,
            CharacterCount = ttsResult.Value.CharacterCount,
            DurationSeconds = ttsResult.Value.DurationSeconds
        };

        try
        {
            _dbContext.DocumentChunkAudios.Add(audioEntity);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogInformation(ex, "Concurrent insert collision for chunk audio {ChunkId}, {VoiceId}", chunk.Id, request.VoiceId);
            var existing = await _dbContext.DocumentChunkAudios
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    a => a.DocumentChunkId == chunk.Id && a.ContentHash == contentHash && a.VoiceId == request.VoiceId,
                    cancellationToken);
            if (existing != null)
            {
                audioEntity = existing;
            }
        }

        return new SynthesizeChunkAudioResponse
        {
            AudioBytes = audioEntity.AudioData,
            MimeType = audioEntity.MimeType,
            ContentHash = audioEntity.ContentHash,
            VoiceId = audioEntity.VoiceId,
            CharacterCount = audioEntity.CharacterCount,
            DurationSeconds = audioEntity.DurationSeconds,
            IsCacheHit = false
        };
    }

    public static string ComputeSha256Hex(string text)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(text));
        return Convert.ToHexStringLower(bytes);
    }

    public static string ExtractNarrationScript(string markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown)) return string.Empty;

        var text = markdown;
        text = Regex.Replace(text, @"```[\s\S]*?```|~~~[\s\S]*?~~~", " ");
        text = Regex.Replace(text, @"`([^`]*)`", "$1");
        text = Regex.Replace(text, @"!\[([^\]]*)\]\([^)]*\)|\[([^\]]*)\]\([^)]*\)", "$1");
        text = Regex.Replace(text, @"\[!(?:NOTE|TIP|WARNING|IMPORTANT|CAUTION)\]", " ", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"(?m)^\s{0,3}#{1,6}\s+|^\s*>\s?|^\s*[-*+]\s+|^\s*\d+\.\s+", "");
        text = Regex.Replace(text, @"(?m)^\s*([-*_])\1{2,}\s*$", " ");
        text = Regex.Replace(text, @"(\*\*|__|~~|\*|_)", "");
        text = Regex.Replace(text, @"\||<[^>]+>", " ");
        text = Regex.Replace(text, @"[ \t]+", " ");
        text = Regex.Replace(text, @"\n{2,}", "\n");
        text = Regex.Replace(text, @"[ \t]*\n[ \t]*", "\n");

        return text.Trim();
    }
}
