namespace Application.Contributions.Dtos;

public sealed record ContributionAdminListDto(
    Guid Id,
    string Slug,
    string Title,
    DateTime CreatedAt,
    DateTime? DeletedAt);
