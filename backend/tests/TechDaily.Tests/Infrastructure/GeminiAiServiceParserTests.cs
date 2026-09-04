using FluentAssertions;
using System.Text.Json;
using TechDaily.Infrastructure.Services;
using Xunit;

namespace TechDaily.Tests.Infrastructure;

public class GeminiAiServiceParserTests
{
    [Fact]
    public void ExtractJsonArray_ShouldExtractExactArray_WhenSurroundedByCommentary()
    {
        var input = "Here are the questions:\n[{\"questionText\":\"Q1\"},{\"questionText\":\"Q2\"}]\nHope this helps!";
        var result = GeminiAiService.ExtractJsonArray(input);

        result.Should().Be("[{\"questionText\":\"Q1\"},{\"questionText\":\"Q2\"}]");
        var doc = JsonDocument.Parse(result);
        doc.RootElement.ValueKind.Should().Be(JsonValueKind.Array);
        doc.RootElement.GetArrayLength().Should().Be(2);
    }

    [Fact]
    public void ExtractJsonArray_ShouldIgnoreHallucinatedTrailingBrackets()
    {
        // This simulates Gemini's hallucinated trailing brackets like `] } ] ] } ]`
        var input = "[{\"questionText\":\"Q1\",\"options\":[\"A\",\"B\"]}] } ] ] } ]";
        var result = GeminiAiService.ExtractJsonArray(input);

        result.Should().Be("[{\"questionText\":\"Q1\",\"options\":[\"A\",\"B\"]}]");
        var doc = JsonDocument.Parse(result);
        doc.RootElement.ValueKind.Should().Be(JsonValueKind.Array);
        doc.RootElement.GetArrayLength().Should().Be(1);
    }

    [Fact]
    public void ExtractJsonArray_ShouldHandleBracketsAndEscapedQuotesInsideStrings()
    {
        var input = "[{\"text\":\"Bracket [inside] string and escaped quote \\\"hello\\\" ]\"}] trailing text ]";
        var result = GeminiAiService.ExtractJsonArray(input);

        result.Should().Be("[{\"text\":\"Bracket [inside] string and escaped quote \\\"hello\\\" ]\"}]");
        var doc = JsonDocument.Parse(result);
        doc.RootElement[0].GetProperty("text").GetString().Should().Be("Bracket [inside] string and escaped quote \"hello\" ]");
    }

    [Fact]
    public void ExtractJsonObject_ShouldExtractExactObject_WhenSurroundedByTrailingBraces()
    {
        var input = "```json\n{\"title\":\"Insight Title\",\"category\":1}\n```\n} } }";
        var result = GeminiAiService.ExtractJsonObject(input);

        result.Should().Be("{\"title\":\"Insight Title\",\"category\":1}");
        var doc = JsonDocument.Parse(result);
        doc.RootElement.ValueKind.Should().Be(JsonValueKind.Object);
        doc.RootElement.GetProperty("title").GetString().Should().Be("Insight Title");
    }

    [Fact]
    public void ExtractJsonObject_ShouldHandleBracesInsideStrings()
    {
        var input = "{\"code\":\"public void Foo() { int x = 1; }\"} extra text }";
        var result = GeminiAiService.ExtractJsonObject(input);

        result.Should().Be("{\"code\":\"public void Foo() { int x = 1; }\"}");
        var doc = JsonDocument.Parse(result);
        doc.RootElement.GetProperty("code").GetString().Should().Be("public void Foo() { int x = 1; }");
    }
}
