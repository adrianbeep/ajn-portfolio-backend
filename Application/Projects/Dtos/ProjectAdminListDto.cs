namespace Application.Projects.Dtos;

public sealed record ProjectAdminListDto(
    Guid Id,
    string Slug,
    string Title,
    DateTime CreatedAt,
    DateTime? DeletedAt);