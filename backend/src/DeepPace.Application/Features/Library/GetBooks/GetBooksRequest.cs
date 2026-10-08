using DeepPace.Domain.Enums;

namespace DeepPace.Application.Features.Library.GetBooks;

public record GetBooksRequest(
    Category? Category = null,
    string? Search = null,
    int Page = 1,
    int PageSize = 12,
    Guid? UserId = null);
