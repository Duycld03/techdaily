using Pgvector;
using DeepPace.Domain.Common;

namespace DeepPace.Domain.Entities;

public class TermExplanationCache : BaseEntity
{
    public string Term { get; set; } = string.Empty; // lowercase keyword
    public string ContextLabel { get; set; } = string.Empty; // free-text context (e.g. DotNet, Postgres, Vue, SystemDesign), max 255 chars
    public string Locale { get; set; } = "en"; // en, vi
    public string ExplanationText { get; set; } = string.Empty;
    public Vector? Embedding { get; set; }
    public int HitCount { get; set; } = 1;

    public void IncrementHit()
    {
        HitCount++;
        MarkUpdated();
    }
}
