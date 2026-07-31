using Application.Contributions.Queries.GetAllContributionsForAdmin;
using Application.Tests.Fakes;
using Domain.Entities;
using FluentAssertions;
using Xunit;

namespace Application.Tests.Contributions.GetAllContributionsForAdmin;

public class GetAllContributionsForAdminQueryHandlerTests
{
    [Fact]
    public async Task Handle_WithActiveAndDeletedProjects_ReturnsAll()
    {
        var p1 = new Contribution("A", "slug-a", "url1", "url1", "d1", "c1");
        var p2 = new Contribution("B", "slug-b", "url2", "url1", "d2", "c2");
        var p3 = new Contribution("C", "slug-c", "url3", "url1", "d3", "c3");
        p2.MarkAsDeleted();

        var fakeRepo = new FakeContributionRepository();
        fakeRepo.Seed(p1, p2, p3);
        var handler = new GetAllContributionsForAdminQueryHandler(fakeRepo);
        var query = new GetAllContributionsForAdminQuery();

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(3);
    }

    [Fact]
    public async Task Handle_WithNoProjects_ReturnsEmptyList()
    {
        var fakeRepo = new FakeContributionRepository();
        var handler = new GetAllContributionsForAdminQueryHandler(fakeRepo);
        var query = new GetAllContributionsForAdminQuery();

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_MapsAllFieldsCorrectly()
    {
        var active = new Contribution("Active", "slug-active", "url1", "url1", "d1", "c1");
        var deleted = new Contribution("Deleted", "slug-deleted", "url2", "url1", "d2", "c2");
        deleted.MarkAsDeleted();

        var fakeRepo = new FakeContributionRepository();
        fakeRepo.Seed(active, deleted);
        var handler = new GetAllContributionsForAdminQueryHandler(fakeRepo);
        var query = new GetAllContributionsForAdminQuery();

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
