namespace DeepPace.Domain.Entities;

public static class CategoryDefaults
{
    public static readonly Guid FrontendWebId = Guid.Parse("10000000-0000-4000-8000-000000000000");
    public static readonly Guid BackendRuntimeId = Guid.Parse("10000000-0000-4000-8000-000000000001");
    public static readonly Guid DatabaseStorageId = Guid.Parse("10000000-0000-4000-8000-000000000002");
    public static readonly Guid SystemDesignId = Guid.Parse("10000000-0000-4000-8000-000000000003");
    public static readonly Guid EngineeringCraftId = Guid.Parse("10000000-0000-4000-8000-000000000004");
    public static readonly Guid MentalModelsId = Guid.Parse("10000000-0000-4000-8000-000000000005");
    public static readonly Guid HabitsProductivityId = Guid.Parse("10000000-0000-4000-8000-000000000006");

    public const string FrontendWebSlug = "frontend-web";
    public const string BackendRuntimeSlug = "backend-runtime";
    public const string DatabaseStorageSlug = "database-storage";
    public const string SystemDesignSlug = "system-design";
    public const string EngineeringCraftSlug = "engineering-craft";
    public const string MentalModelsSlug = "mental-models";
    public const string HabitsProductivitySlug = "habits-productivity";
}
