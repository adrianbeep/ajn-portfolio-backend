using Application.Projects.Queries.GetProjectBySlug;
using Application.Tests.Fakes;
using Domain.Entities;
using FluentAssertions;
using Xunit;

namespace Application.Tests.Projects.GetProjectBySlug;

public class GetProjectBySlugQueryHandlerTests
{
    [Fact]
    public async Task Handle_WhenProjectExists_ReturnsDetail()
    {
        var existing = new Project("Title", "my-slug", "url", "desc", "content");
        var fakeRepo = new FakeProjectRepository();
        fakeRepo.Seed(existing);
        var handler = new GetProjectBySlugQueryHandler(fakeRepo);
        var query = new GetProjectBySlugQuery("my-slug");

        var result = await handler.Handle(query);

        result.IsSuccess.Should().BeTrue();
        result.Value.Slug.Should().Be("my-slug");
        result.Value.Title.Should().Be("Title");
        result.Value.Content.Should().Be("content");
    }

    [Fact]
    public async Task Handle_WhenProjectNotFound_ReturnsFailure()
    {
        var fakeRepo = new FakeProjectRepository();
        var handler = new GetProjectBySlugQueryHandler(fakeRepo);
        var query = new GetProjectBySlugQuery("no-existe");

        var result = await handler.Handle(query);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("ProjectNotFound");
    }

    [Fact]
    public async Task Handle_WithTechnologies_ReturnsThemInTheAuthorOrder()
    {
        var existing = new Project("Title", "my-slug", "url", "desc", "content", new[] { "Astro", "TypeScript" });
        var fakeRepo = new FakeProjectRepository();
        fakeRepo.Seed(existing);
        var handler = new GetProjectBySlugQueryHandler(fakeRepo);
        var query = new GetProjectBySlugQuery("my-slug");

        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Technologies.Should().Equal("Astro", "TypeScript");
    }

    [Fact]
    public async Task Handle_WithoutTechnologies_ReturnsAnEmptyList()
    {
        var existing = new Project("Title", "my-slug", "url", "desc", "content");
        var fakeRepo = new FakeProjectRepository();
        fakeRepo.Seed(existing);
        var handler = new GetProjectBySlugQueryHandler(fakeRepo);
        var query = new GetProjectBySlugQuery("my-slug");

        var result = await handler.Handle(query, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Technologies.Should().BeEmpty();
    }
}
