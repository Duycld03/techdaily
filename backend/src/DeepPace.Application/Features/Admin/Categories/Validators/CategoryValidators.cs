using System.Text.RegularExpressions;
using FluentValidation;
using DeepPace.Application.Features.Admin.Categories.DTOs;

namespace DeepPace.Application.Features.Admin.Categories.Validators;

public class CreateCategoryValidator : AbstractValidator<CreateCategoryRequest>
{
    private static readonly Regex SlugRegex = new("^[a-z0-9]+(-[a-z0-9]+)*$", RegexOptions.Compiled);

    public CreateCategoryValidator()
    {
        RuleFor(x => x.Slug)
            .NotEmpty().WithMessage("Slug is required.")
            .MaximumLength(100).WithMessage("Slug cannot exceed 100 characters.")
            .Must(s => s != null && SlugRegex.IsMatch(s.Trim()))
            .WithMessage("Slug must contain only trimmed lowercase ASCII letters, digits, and single separating hyphens.");

        RuleFor(x => x.NameEn)
            .NotEmpty().WithMessage("English name is required.")
            .MaximumLength(100).WithMessage("English name cannot exceed 100 characters.");

        RuleFor(x => x.NameVi)
            .NotEmpty().WithMessage("Vietnamese name is required.")
            .MaximumLength(100).WithMessage("Vietnamese name cannot exceed 100 characters.");

        RuleFor(x => x.SortOrder)
            .InclusiveBetween(0, 10000).WithMessage("Sort order must be between 0 and 10000.");
    }
}

public class UpdateCategoryValidator : AbstractValidator<UpdateCategoryRequest>
{
    public UpdateCategoryValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Category ID is required.");

        RuleFor(x => x.NameEn)
            .NotEmpty().WithMessage("English name is required.")
            .MaximumLength(100).WithMessage("English name cannot exceed 100 characters.");

        RuleFor(x => x.NameVi)
            .NotEmpty().WithMessage("Vietnamese name is required.")
            .MaximumLength(100).WithMessage("Vietnamese name cannot exceed 100 characters.");

        RuleFor(x => x.SortOrder)
            .InclusiveBetween(0, 10000).WithMessage("Sort order must be between 0 and 10000.");

        RuleFor(x => x.ExpectedVersion)
            .GreaterThan(0).WithMessage("Expected version must be a positive integer.");
    }
}

public class DeleteCategoryValidator : AbstractValidator<DeleteCategoryRequest>
{
    public DeleteCategoryValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Category ID is required.");

        RuleFor(x => x.ExpectedVersion)
            .GreaterThan(0).WithMessage("Expected version must be a positive integer.");
    }
}

public class GetAdminCategoriesValidator : AbstractValidator<GetAdminCategoriesRequest>
{
    public GetAdminCategoriesValidator()
    {
        RuleFor(x => x.Search)
            .MaximumLength(200).WithMessage("Search term cannot exceed 200 characters.");

        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1).WithMessage("Page must be at least 1.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100).WithMessage("Page size must be between 1 and 100.");
    }
}
