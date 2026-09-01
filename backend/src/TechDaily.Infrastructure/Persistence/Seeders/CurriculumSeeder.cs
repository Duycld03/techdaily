using System.Reflection;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TechDaily.Domain.Entities;
using TechDaily.Domain.Enums;
using TechDaily.Domain.ValueObjects;

namespace TechDaily.Infrastructure.Persistence.Seeders;

public static class CurriculumSeeder
{
    public static async Task SeedAsync(TechDailyDbContext context)
    {
        var masterBookId = Guid.Parse("10000000-0000-0000-0000-000000000001");
        var masterBook = await context.DocumentBooks
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(b => b.Id == masterBookId);

        if (masterBook == null)
        {
            masterBook = new DocumentBook
            {
                Id = masterBookId,
                Title = "30-Day Senior Fullstack Curriculum",
                Slug = "30-day-senior-curriculum",
                SourceType = SourceType.MarkdownSeries,
                Category = Category.BackendDotNet,
                TotalChunks = 30,
                AuthorOrSourceUrl = "https://techdaily.dev/curriculum",
                IsPublished = true,
                IsDeleted = false
            };
            await context.DocumentBooks.AddAsync(masterBook);
        }
        else
        {
            masterBook.IsDeleted = false;
            masterBook.IsPublished = true;
            masterBook.TotalChunks = 30;
        }
        await context.SaveChangesAsync();

        var curriculumData = GetCurriculumItems(masterBook.Id);

        // Ensure default development test user exists
        var devUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var devUser = await context.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == devUserId);

        if (devUser == null)
        {
            devUser = new User
            {
                Id = devUserId,
                Email = "senior.dev@techdaily.local",
                Name = "Senior Engineer (Dev)",
                PreferredLocale = "en",
                IsDeleted = false
            };
            await context.Users.AddAsync(devUser);
            var streak = StreakRecord.Create(devUser.Id);
            await context.StreakRecords.AddAsync(streak);
        }
        else
        {
            devUser.IsDeleted = false;
        }

        // Upsert all 30 topics, interview questions, and document chunks
        foreach (var (seededTopic, seededQuestion, seededChunk) in curriculumData)
        {
            var existingTopic = await context.Topics
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(t => t.DayOrder == seededTopic.DayOrder || t.Slug == seededTopic.Slug);

            if (existingTopic == null)
            {
                seededTopic.IsDeleted = false;
                seededQuestion.IsDeleted = false;
                seededChunk.IsDeleted = false;
                await context.Topics.AddAsync(seededTopic);
                await context.InterviewQuestions.AddAsync(seededQuestion);
                await context.DocumentChunks.AddAsync(seededChunk);
            }
            else
            {
                // Update topic metadata and restore if deleted
                existingTopic.IsDeleted = false;
                existingTopic.Slug = seededTopic.Slug;
                existingTopic.Title = seededTopic.Title;
                existingTopic.Summary = seededTopic.Summary;
                existingTopic.DeepDiveMarkdown = seededTopic.DeepDiveMarkdown;
                existingTopic.Category = seededTopic.Category;
                existingTopic.Difficulty = seededTopic.Difficulty;
                existingTopic.DayOrder = seededTopic.DayOrder;

                // Sync InterviewQuestion
                var existingQuestion = await context.InterviewQuestions
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(q => q.TopicId == existingTopic.Id);

                if (existingQuestion == null)
                {
                    seededQuestion.TopicId = existingTopic.Id;
                    seededQuestion.IsDeleted = false;
                    await context.InterviewQuestions.AddAsync(seededQuestion);
                }
                else
                {
                    existingQuestion.IsDeleted = false;
                    existingQuestion.QuestionText = seededQuestion.QuestionText;
                    existingQuestion.Options = seededQuestion.Options;
                    existingQuestion.CorrectOptionIndex = seededQuestion.CorrectOptionIndex;
                    existingQuestion.ExplanationMarkdown = seededQuestion.ExplanationMarkdown;
                    existingQuestion.ExpectedKeyPoints = seededQuestion.ExpectedKeyPoints;
                    existingQuestion.ModelAnswerMarkdown = seededQuestion.ModelAnswerMarkdown;
                    existingQuestion.Difficulty = seededQuestion.Difficulty;
                }

                // Sync DocumentChunk
                var existingChunk = await context.DocumentChunks
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(c => c.DocumentBookId == masterBook.Id && c.ChunkOrder == seededTopic.DayOrder);

                if (existingChunk == null)
                {
                    seededChunk.IsDeleted = false;
                    await context.DocumentChunks.AddAsync(seededChunk);
                }
                else
                {
                    existingChunk.IsDeleted = false;
                    existingChunk.ChapterTitle = seededChunk.ChapterTitle;
                    existingChunk.OriginalTextMarkdown = seededChunk.OriginalTextMarkdown;
                    existingChunk.SummaryMarkdown = seededChunk.SummaryMarkdown;
                    existingChunk.KeyTakeaways = seededChunk.KeyTakeaways;
                    existingChunk.MicroQuiz = seededChunk.MicroQuiz;
                    existingChunk.Language = seededChunk.Language;
                    existingChunk.EstimatedReadMinutes = seededChunk.EstimatedReadMinutes;
                }
            }
        }

        await context.SaveChangesAsync();
    }

    public static List<(Topic topic, InterviewQuestion question, DocumentChunk chunk)> GetCurriculumItems(Guid bookId)
    {
        var rawJson = LoadCurriculumJson();
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        var dtoList = JsonSerializer.Deserialize<List<CurriculumDayDto>>(rawJson, options)
            ?? throw new InvalidOperationException("Failed to deserialize curriculum-30-days.json");

        var list = new List<(Topic, InterviewQuestion, DocumentChunk)>();

        foreach (var dto in dtoList.OrderBy(d => d.DayOrder))
        {
            var day = dto.DayOrder;
            var topicId = Guid.Parse($"20000000-0000-0000-0000-{day:D12}");
            var questionId = Guid.Parse($"30000000-0000-0000-0000-{day:D12}");
            var chunkId = Guid.Parse($"40000000-0000-0000-0000-{day:D12}");

            var category = Enum.TryParse<Category>(dto.Category, true, out var cat) ? cat : Category.FrontendWeb;
            var difficulty = Enum.TryParse<Difficulty>(dto.Difficulty, true, out var diff) ? diff : Difficulty.Senior;

            var topic = new Topic
            {
                Id = topicId,
                Slug = dto.Slug,
                Title = dto.Title,
                Category = category,
                Difficulty = difficulty,
                DayOrder = day,
                Summary = dto.Summary,
                DeepDiveMarkdown = dto.DeepDiveMarkdown
            };

            var q = dto.InterviewQuestion ?? new CurriculumQuestionDto();
            var question = new InterviewQuestion
            {
                Id = questionId,
                TopicId = topicId,
                QuestionText = q.QuestionText,
                Options = q.Options ?? new List<string>(),
                CorrectOptionIndex = q.CorrectOptionIndex,
                ExplanationMarkdown = q.ExplanationMarkdown,
                ExpectedKeyPoints = q.ExpectedKeyPoints ?? new List<string>(),
                ModelAnswerMarkdown = q.ModelAnswerMarkdown,
                Difficulty = difficulty
            };

            var c = dto.DocumentChunk ?? new CurriculumChunkDto();
            var chunk = new DocumentChunk
            {
                Id = chunkId,
                DocumentBookId = bookId,
                ChunkOrder = day,
                ChapterTitle = string.IsNullOrWhiteSpace(c.ChapterTitle) ? dto.Title : c.ChapterTitle,
                OriginalTextMarkdown = c.OriginalTextMarkdown,
                SummaryMarkdown = c.SummaryMarkdown,
                KeyTakeaways = c.KeyTakeaways ?? new List<string>(),
                MicroQuiz = c.MicroQuiz ?? new MicroQuizVo
                {
                    Question = "What is the key takeaway for this topic?",
                    Options = new List<string> { "Option A", "Option B", "Option C", "Option D" },
                    AnswerIndex = 0,
                    Explanation = "Refer to the reading materials."
                },
                Language = string.IsNullOrWhiteSpace(c.Language) ? "en" : c.Language,
                EstimatedReadMinutes = c.EstimatedReadMinutes > 0 ? c.EstimatedReadMinutes : 3
            };

            list.Add((topic, question, chunk));
        }

        return list;
    }

    private static string LoadCurriculumJson()
    {
        var assemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? AppContext.BaseDirectory;
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "Data", "curriculum-30-days.json"),
            Path.Combine(assemblyLocation, "Data", "curriculum-30-days.json"),
            Path.Combine(AppContext.BaseDirectory, "curriculum-30-days.json"),
            Path.Combine(Directory.GetCurrentDirectory(), "backend", "src", "TechDaily.Infrastructure", "Data", "curriculum-30-days.json"),
            Path.Combine(Directory.GetCurrentDirectory(), "..", "TechDaily.Infrastructure", "Data", "curriculum-30-days.json"),
            Path.Combine(Directory.GetCurrentDirectory(), "Data", "curriculum-30-days.json")
        };

        foreach (var path in candidates)
        {
            if (File.Exists(path))
            {
                return File.ReadAllText(path);
            }
        }

        // Fallback to EmbeddedResource
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = assembly.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith("curriculum-30-days.json", StringComparison.OrdinalIgnoreCase));

        if (resourceName != null)
        {
            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream != null)
            {
                using var reader = new StreamReader(stream);
                return reader.ReadToEnd();
            }
        }

        throw new FileNotFoundException("Could not find curriculum-30-days.json in file system or embedded resources.");
    }
}

public class CurriculumDayDto
{
    public int DayOrder { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Difficulty { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string DeepDiveMarkdown { get; set; } = string.Empty;
    public string? BenchmarkSnippet { get; set; }
    public CurriculumQuestionDto? InterviewQuestion { get; set; }
    public CurriculumChunkDto? DocumentChunk { get; set; }
}

public class CurriculumQuestionDto
{
    public string QuestionText { get; set; } = string.Empty;
    public List<string> Options { get; set; } = new();
    public int CorrectOptionIndex { get; set; }
    public string ExplanationMarkdown { get; set; } = string.Empty;
    public List<string> ExpectedKeyPoints { get; set; } = new();
    public string ModelAnswerMarkdown { get; set; } = string.Empty;
}

public class CurriculumChunkDto
{
    public string ChapterTitle { get; set; } = string.Empty;
    public string OriginalTextMarkdown { get; set; } = string.Empty;
    public string SummaryMarkdown { get; set; } = string.Empty;
    public List<string> KeyTakeaways { get; set; } = new();
    public MicroQuizVo? MicroQuiz { get; set; }
    public string Language { get; set; } = "en";
    public int EstimatedReadMinutes { get; set; } = 3;
}
