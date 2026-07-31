using Application.Contributions.Queries.GetContributions;
using Application.Tests.Fakes;
using Domain.Entities;
using FluentAssertions;
using Xunit;

namespace Application.Tests.Contributions.GetContributions;

public class GetContributionsQueryHandlerTests
{
    [Fact]
    public async Task Handle_WithMultipleContributions_ReturnsPaginatedList()
    {
        var c1 = new Contribution("A", "slug-a", "url1", "pr1", "d1", "c1");
        var c2 = new Contribution("B", "slug-b", "url2", "pr2", "d2", "c2");
        var c3 = new Contribution("C", "slug-c", "url3", "pr3", "d3", "c3");
        var fakeRepo = new FakeContributionRepository();
        fakeRepo.Seed(c1, c2, c3);
        var handler = new GetContributionsQueryHandler(fakeRepo);
        var query = new GetContributionsQuery(Page: 1, PageSize: 20);

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(3);
        result.Value.Total.Should().Be(3);
        result.Value.Page.Should().Be(1);
        result.Value.PageSize.Should().Be(20);
    }

    [Fact]
    public async Task Handle_WithNoContributions_ReturnsEmptyList()
    {
        var fakeRepo = new FakeContributionRepository();
        var handler = new GetContributionsQueryHandler(fakeRepo);
        var query = new GetContributionsQuery(Page: 1, PageSize: 20);

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
        result.Value.Total.Should().Be(0);
    }
}
