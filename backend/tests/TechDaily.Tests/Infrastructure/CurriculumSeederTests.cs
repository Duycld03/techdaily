using FluentAssertions;
using TechDaily.Domain.Enums;
using TechDaily.Infrastructure.Persistence.Seeders;
using Xunit;

namespace TechDaily.Tests.Infrastructure;

public class CurriculumSeederTests
{
    [Fact]
    public void GetCurriculumItems_ShouldReturnExactly30DaysInOrder()
    {
        var bookId = Guid.NewGuid();
        var items = CurriculumSeeder.GetCurriculumItems(bookId);

        items.Should().HaveCount(30);

        for (int i = 0; i < 30; i++)
        {
            var dayOrder = i + 1;
            var (topic, question, chunk) = items[i];

            topic.DayOrder.Should().Be(dayOrder);
            topic.Slug.Should().NotBeNullOrWhiteSpace();
            topic.Title.Should().NotBeNullOrWhiteSpace();
            topic.Summary.Should().NotBeNullOrWhiteSpace();
            topic.DeepDiveMarkdown.Should().NotBeNullOrWhiteSpace();

            question.TopicId.Should().Be(topic.Id);
            question.QuestionText.Should().NotBeNullOrWhiteSpace();
            question.Options.Should().HaveCount(4);
            question.CorrectOptionIndex.Should().BeInRange(0, 3);
            question.ExplanationMarkdown.Should().NotBeNullOrWhiteSpace();
            question.ExpectedKeyPoints.Should().NotBeEmpty();

            chunk.DocumentBookId.Should().Be(bookId);
            chunk.ChunkOrder.Should().Be(dayOrder);
            chunk.OriginalTextMarkdown.Should().NotBeNullOrWhiteSpace();
            chunk.SummaryMarkdown.Should().NotBeNullOrWhiteSpace();
            chunk.KeyTakeaways.Should().NotBeEmpty();
            chunk.MicroQuiz.Should().NotBeNull();
            chunk.MicroQuiz.Question.Should().NotBeNullOrWhiteSpace();
            chunk.MicroQuiz.Options.Should().HaveCount(4);
            chunk.MicroQuiz.AnswerIndex.Should().BeInRange(0, 3);
            chunk.MicroQuiz.Explanation.Should().NotBeNullOrWhiteSpace();
        }
    }

    [Fact]
    public void Curriculum_ShouldCoverAllFourRequiredTechnicalCategories()
    {
        var bookId = Guid.NewGuid();
        var items = CurriculumSeeder.GetCurriculumItems(bookId);

        // Group 1: Modern Frontend (Days 1 - 7)
        items.Where(x => x.topic.DayOrder >= 1 && x.topic.DayOrder <= 7)
            .Should().OnlyContain(x => x.topic.Category == Category.FrontendWeb);

        // Group 2: .NET 10 Internals (Days 8 - 15)
        items.Where(x => x.topic.DayOrder >= 8 && x.topic.DayOrder <= 15)
            .Should().OnlyContain(x => x.topic.Category == Category.BackendDotNet);

        // Group 3: Database & Storage (Days 16 - 22)
        items.Where(x => x.topic.DayOrder >= 16 && x.topic.DayOrder <= 22)
            .Should().OnlyContain(x => x.topic.Category == Category.DatabaseStorage);

        // Group 4: System Design & Distributed (Days 23 - 30)
        items.Where(x => x.topic.DayOrder >= 23 && x.topic.DayOrder <= 30)
            .Should().OnlyContain(x => x.topic.Category == Category.SystemDesign);
    }
}
