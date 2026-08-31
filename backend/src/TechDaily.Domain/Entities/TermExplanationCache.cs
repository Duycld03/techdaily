using Pgvector;
using TechDaily.Domain.Common;

namespace TechDaily.Domain.Entities;

public class TermExplanationCache : BaseEntity
{
    public string Term { get; set; } = string.Empty; // lowercase keyword
    public string Category { get; set; } = string.Empty; // DotNet, Postgres, Vue, SystemDesign
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
