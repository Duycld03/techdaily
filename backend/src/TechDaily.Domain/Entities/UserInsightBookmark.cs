using TechDaily.Domain.Common;

namespace TechDaily.Domain.Entities;

public class UserInsightBookmark : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid InsightId { get; set; }

    // Navigation properties
    public User User { get; set; } = null!;
    public TechInsight Insight { get; set; } = null!;
}
