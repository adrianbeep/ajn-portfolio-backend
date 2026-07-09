using Application.Projects.Queries.GetProjects;
using Application.Tests.Fakes;
using Domain.Entities;
using FluentAssertions;
using Xunit;

namespace Application.Tests.Projects.GetProjects;

public class GetProjectsQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_WithMultipleProjects_ReturnsPaginatedList()
    {
        var p1 = new Project("A", "slug-a", "url1", "d1", "c1");
        var p2 = new Project("B", "slug-b", "url2", "d2", "c2");
        var p3 = new Project("C", "slug-c", "url3", "d3", "c3");
        var fakeRepo = new FakeProjectRepository();
        fakeRepo.Seed(p1, p2, p3);
        var handler = new GetProjectsQueryHandler(fakeRepo);
        var query = new GetProjectsQuery(Page: 1, PageSize: 20);

        var result = await handler.HandleAsync(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(3);
        result.Value.Total.Should().Be(3);
        result.Value.Page.Should().Be(1);
        result.Value.PageSize.Should().Be(20);
    }

    [Fact]
    public async Task HandleAsync_WithNoProjects_ReturnsEmptyList()
    {
        var fakeRepo = new FakeProjectRepository();
        var handler = new GetProjectsQueryHandler(fakeRepo);
        var query = new GetProjectsQuery(Page: 1, PageSize: 20);

        var result = await handler.HandleAsync(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
        result.Value.Total.Should().Be(0);
    }
}
