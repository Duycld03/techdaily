using DeepPace.Application.Features.Library.DTOs;

namespace DeepPace.Application.Features.Library.UpdateBook;

public record UpdateBookRequest(
    Guid BookId,
    Guid UserId,
    string Title,
    string? AuthorOrSourceUrl,
    Guid CategoryId,
    string? Language = null);

public class UpdateBookResponse
{
    public BookDto Book { get; set; } = null!;
}
