using System.Text.Json.Serialization;
using DeepPace.Application.Features.Library.DTOs;

namespace DeepPace.Application.Features.Library.GetBooks;

public record GetBooksResponse
{
    public List<BookDto> Books { get; init; } = new();
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalPages { get; init; }

    [JsonConstructor]
    public GetBooksResponse(
        List<BookDto> books,
        int totalCount,
        int page,
        int pageSize,
        int totalPages)
    {
        Books = books;
        TotalCount = totalCount;
        Page = page;
        PageSize = pageSize;
        TotalPages = totalPages;
    }

    public GetBooksResponse(List<BookDto> books, int totalCount, int page, int pageSize)
        : this(books, totalCount, page, pageSize, totalCount == 0 ? 0 : (int)Math.Ceiling((double)totalCount / (pageSize > 0 ? pageSize : 12)))
    {
    }
}
