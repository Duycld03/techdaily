namespace TechDaily.Domain.ValueObjects;

public class MicroQuizVo
{
    public string Question { get; set; } = string.Empty;
    public List<string> Options { get; set; } = new();
    public int AnswerIndex { get; set; }
    public string Explanation { get; set; } = string.Empty;
}
