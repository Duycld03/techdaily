using TechDaily.Application.Features.Library.DTOs;
using TechDaily.Domain.Enums;

namespace TechDaily.Application.Features.Library.UpdateBook;

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
