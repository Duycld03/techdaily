using DeepPace.Application.Features.Library.DTOs;
using DeepPace.Domain.Enums;

namespace DeepPace.Application.Features.Library.UpdateBook;

public record UpdateBookRequest(
    Guid BookId,
    Guid UserId,
    string Title,
    string? AuthorOrSourceUrl,
    Category Category,
    string? Language = null);

public class UpdateBookResponse
{
    public BookDto Book { get; set; } = null!;
}
