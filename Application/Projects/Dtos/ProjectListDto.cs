namespace Application.Projects.Dtos
{
    public sealed record ProjectListDto(
        IReadOnlyList<ProjectSummaryDto> Items,
        int Total,
        int Page,
        int PageSize);
}
