using TechDaily.Domain.Enums;

namespace TechDaily.Application.Features.Library.GetBooks;

public record GetBooksRequest(
    Category? Category = null,
    string? Search = null,
    int Page = 1,
    int PageSize = 12);
