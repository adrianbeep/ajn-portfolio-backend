using Application.Projects.Dtos;
using Application.Projects.Queries.GetAllProjectsForAdmin;
using Application.Tests.Fakes;
using Domain.Entities;
using FluentAssertions;
using Xunit;

namespace Application.Tests.Projects.GetAllProjectsForAdmin;

public class GetAllProjectsForAdminQueryHandlerTests
{
    [Fact]
    public async Task Handle_WithActiveAndDeletedProjects_ReturnsAll()
    {
        var p1 = new Project("A", "slug-a", "url1", "d1", "c1");
        var p2 = new Project("B", "slug-b", "url2", "d2", "c2");
        var p3 = new Project("C", "slug-c", "url3", "d3", "c3");
        p2.MarkAsDeleted();

        var fakeRepo = new FakeProjectRepository();
        fakeRepo.Seed(p1, p2, p3);
        var handler = new GetAllProjectsForAdminQueryHandler(fakeRepo);
        var query = new GetAllProjectsForAdminQuery();

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(3);
    }

    [Fact]
    public async Task Handle_WithNoProjects_ReturnsEmptyList()
    {
        var fakeRepo = new FakeProjectRepository();
        var handler = new GetAllProjectsForAdminQueryHandler(fakeRepo);
        var query = new GetAllProjectsForAdminQuery();

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_MapsAllFieldsCorrectly()
    {
        var active = new Project("Active", "slug-active", "url1", "d1", "c1");
        var deleted = new Project("Deleted", "slug-deleted", "url2", "d2", "c2");
        deleted.MarkAsDeleted();

        var fakeRepo = new FakeProjectRepository();
        fakeRepo.Seed(active, deleted);
        var handler = new GetAllProjectsForAdminQueryHandler(fakeRepo);
        var query = new GetAllProjectsForAdminQuery();

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);

        var activeDto = result.Value.Single(d => d.Slug == "slug-active");
        activeDto.Id.Should().Be(active.Id);
        activeDto.Title.Should().Be("Active");
        activeDto.CreatedAt.Should().Be(active.CreatedAt);
        activeDto.DeletedAt.Should().BeNull();

        var deletedDto = result.Value.Single(d => d.Slug == "slug-deleted");
        deletedDto.Id.Should().Be(deleted.Id);
        deletedDto.Title.Should().Be("Deleted");
        deletedDto.DeletedAt.Should().NotBeNull();
        deletedDto.DeletedAt.Should().Be(deleted.DeletedAt);
    }
}