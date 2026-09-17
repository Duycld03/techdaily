using TechDaily.Application.Features.Library.DTOs;

namespace TechDaily.Application.Features.Library.GetBooks;

public record GetBooksResponse(
    List<BookDto> Books,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages)
{
    public GetBooksResponse(List<BookDto> books, int totalCount, int page, int pageSize)
        : this(books, totalCount, page, pageSize, totalCount == 0 ? 0 : (int)Math.Ceiling((double)totalCount / (pageSize > 0 ? pageSize : 12)))
    {
    }
}
