namespace DeepPace.Application.Features.Library.GetBooks;

public record GetBooksRequest(
    Guid? CategoryId = null,
    string? Search = null,
    int Page = 1,
    int PageSize = 12,
    Guid? UserId = null,
    string? Language = null);
