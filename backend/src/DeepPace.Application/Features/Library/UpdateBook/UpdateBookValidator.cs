using FluentValidation;

namespace DeepPace.Application.Features.Library.UpdateBook;

public class UpdateBookValidator : AbstractValidator<UpdateBookRequest>
{
    public UpdateBookValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(255).WithMessage("Title cannot exceed 255 characters.");

        RuleFor(x => x.AuthorOrSourceUrl)
            .MaximumLength(500).WithMessage("Author or source URL cannot exceed 500 characters.");

        RuleFor(x => x.CategoryId)
            .NotEmpty().WithMessage("Category ID is required.");

        RuleFor(x => x.Language)
            .Must(l => string.IsNullOrWhiteSpace(l) || l == "en" || l == "vi")
            .WithMessage("Language must be either 'en' or 'vi'.");
    }
}
