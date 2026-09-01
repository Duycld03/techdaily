using TechDaily.Domain.Enums;

namespace TechDaily.Application.Features.Curriculum.DTOs;

public class CurriculumRoadmapResponse
{
    public int TotalDays { get; set; } = 30;
    public int CompletedDaysCount { get; set; }
    public int CurrentActiveDay { get; set; } = 1;
    public decimal OverallProgressPercentage { get; set; }
    public List<CurriculumModuleDto> Modules { get; set; } = new();
}

public class CurriculumModuleDto
{
    public Category Category { get; set; }
    public string ModuleTitle { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int StartDay { get; set; }
    public int EndDay { get; set; }
    public int CompletedCount { get; set; }
    public int TotalCount { get; set; }
    public List<RoadmapDayNodeDto> Days { get; set; } = new();
}

public class RoadmapDayNodeDto
{
    public int DayOrder { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public Difficulty Difficulty { get; set; }
    public bool IsCompleted { get; set; }
    public bool IsActiveToday { get; set; }
    public bool IsUnlocked { get; set; }
    public int? DrillScore { get; set; }
}
