using System.Text;
using Microsoft.EntityFrameworkCore;
using TechDaily.Application.Common;
using TechDaily.Application.Interfaces;

namespace TechDaily.Application.Features.Library.ExportBookMarkdown;

public record ExportBookMarkdownRequest(Guid BookId, Guid UserId);

public class ExportBookMarkdownResponse
{
    public string MarkdownContent { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
}

public class ExportBookMarkdownHandler : IUseCase<ExportBookMarkdownRequest, ExportBookMarkdownResponse>
{
    private readonly ITechDailyDbContext _dbContext;

    public ExportBookMarkdownHandler(ITechDailyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<ExportBookMarkdownResponse>> ExecuteAsync(
        ExportBookMarkdownRequest request,
        CancellationToken cancellationToken = default)
    {
        var book = await _dbContext.DocumentBooks
            .AsNoTracking()
            .Include(b => b.Chunks)
            .FirstOrDefaultAsync(b => b.Id == request.BookId, cancellationToken);

        if (book == null)
        {
            return Error.NotFound;
        }

        var highlights = await _dbContext.UserHighlights
            .AsNoTracking()
            .Where(h => h.UserId == request.UserId && h.DocumentChunk.DocumentBookId == request.BookId)
            .ToListAsync(cancellationToken);

        var highlightsByChunk = highlights
            .GroupBy(h => h.DocumentChunkId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var orderedChunks = book.Chunks.OrderBy(c => c.ChunkOrder).ToList();

        var author = !string.IsNullOrWhiteSpace(book.AuthorOrSourceUrl) ? book.AuthorOrSourceUrl : "Unknown";
        var dateStr = DateTime.UtcNow.ToString("yyyy-MM-dd");
        var slug = !string.IsNullOrWhiteSpace(book.Slug) ? book.Slug : "book";

        var sb = new StringBuilder();
        sb.AppendLine("---");
        sb.AppendLine($"book: \"{book.Title.Replace("\"", "\\\"")}\"");
        sb.AppendLine($"author: \"{author.Replace("\"", "\\\"")}\"");
        sb.AppendLine($"exported_at: {dateStr}");
        sb.AppendLine($"total_chapters: {book.TotalChunks}");
        sb.AppendLine($"total_highlights: {highlights.Count}");
        sb.AppendLine("tags: [techdaily, architecture, notes]");
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine($"# {book.Title}");
        sb.AppendLine();
        sb.AppendLine($"*Exported from TechDaily on {dateStr}*");
        sb.AppendLine();

        foreach (var chunk in orderedChunks)
        {
            sb.AppendLine("---");
            sb.AppendLine();
            sb.AppendLine($"## Chapter {chunk.ChunkOrder}: {chunk.ChapterTitle}");
            sb.AppendLine();

            if (!string.IsNullOrWhiteSpace(chunk.SummaryMarkdown))
            {
                sb.AppendLine("### Executive Summary");
                sb.AppendLine(chunk.SummaryMarkdown.Trim());
                sb.AppendLine();
            }

            if (chunk.KeyTakeaways != null && chunk.KeyTakeaways.Count > 0)
            {
                sb.AppendLine("### Key Takeaways");
                foreach (var takeaway in chunk.KeyTakeaways)
                {
                    sb.AppendLine($"- {takeaway.Trim()}");
                }
                sb.AppendLine();
            }

            if (highlightsByChunk.TryGetValue(chunk.Id, out var chunkHighlights) && chunkHighlights.Count > 0)
            {
                sb.AppendLine("### Highlights & Engineering Reflections");
                sb.AppendLine();

                foreach (var highlight in chunkHighlights)
                {
                    sb.AppendLine($"> \"{highlight.SelectedText.Trim()}\"");
                    if (!string.IsNullOrWhiteSpace(highlight.Note))
                    {
                        sb.AppendLine(">");
                        sb.AppendLine($"> **Personal Note:** {highlight.Note.Trim()}");
                    }
                    if (highlight.Tags != null && highlight.Tags.Count > 0)
                    {
                        var tagTokens = highlight.Tags
                            .Where(t => !string.IsNullOrWhiteSpace(t))
                            .Select(t => t.Trim().StartsWith("#") ? $"`{t.Trim()}`" : $"`#{t.Trim()}`");
                        var tagLine = string.Join(" ", tagTokens);
                        if (!string.IsNullOrWhiteSpace(tagLine))
                        {
                            sb.AppendLine(">");
                            sb.AppendLine($"> *Tags: {tagLine}*");
                        }
                    }
                    sb.AppendLine();
                }
            }
        }

        var fileName = $"{slug}-notes.md";

        return new ExportBookMarkdownResponse
        {
            MarkdownContent = sb.ToString(),
            FileName = fileName
        };
    }
}
