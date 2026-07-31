using Application.Contributions.Queries.GetContributionBySlug;
using Application.Tests.Fakes;
using Domain.Entities;
using FluentAssertions;
using Xunit;

namespace Application.Tests.Contributions.GetContributionBySlug;

public class GetContributionBySlugQueryHandlerTests
{
    [Fact]
    public async Task Handle_WhenContributionExists_ReturnsDetail()
    {
        var existing = new Contribution("Title", "my-slug", "url", "url-pr", "desc", "content");
        var fakeRepo = new FakeContributionRepository();
        fakeRepo.Seed(existing);
        var handler = new GetContributionBySlugQueryHandler(fakeRepo);
        var query = new GetContributionBySlugQuery("my-slug");

        var result = await handler.Handle(query);

        result.IsSuccess.Should().BeTrue();
        result.Value.Slug.Should().Be("my-slug");
        result.Value.Title.Should().Be("Title");
        result.Value.Content.Should().Be("content");
        result.Value.UrlPrContribution.Should().Be("url-pr");
    }

    [Fact]
    public async Task Handle_WhenContributionNotFound_ReturnsFailure()
    {
        var fakeRepo = new FakeContributionRepository();
        var handler = new GetContributionBySlugQueryHandler(fakeRepo);
        var query = new GetContributionBySlugQuery("no-existe");

        var result = await handler.Handle(query);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("ContributionNotFound");
    }
}
