using Application.Projects.Commands.UpdateProject;
using Application.Tests.Fakes;
using Domain.Entities;
using FluentAssertions;
using Xunit;

namespace Application.Tests.Projects.UpdateProject;

public class UpdateProjectCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenProjectExists_UpdatesAndSaves()
    {
        var existing = new Project("Old", "my-slug", "old-url", "old-desc", "old-content");
        var fakeRepo = new FakeProjectRepository();
        fakeRepo.Seed(existing);
        var fakeUow = new FakeUnitOfWork();
        var handler = new UpdateProjectCommandHandler(fakeRepo, fakeUow);
        var command = new UpdateProjectCommand(
            "my-slug", "New Title", "New Desc", "new-url", "new-content");

        var result = await handler.Handle(command);

        result.IsSuccess.Should().BeTrue();
        fakeRepo.UpdateCallCount.Should().Be(1);
        fakeUow.SaveChangesCallCount.Should().Be(1);
        existing.Title.Should().Be("New Title");
        existing.Description.Should().Be("New Desc");
        existing.UrlThumbnail.Should().Be("new-url");
        existing.Content.Should().Be("new-content");
    }

    [Fact]
    public async Task Handle_WhenProjectNotFound_ReturnsFailure()
    {
        var fakeRepo = new FakeProjectRepository();
        var fakeUow = new FakeUnitOfWork();
        var handler = new UpdateProjectCommandHandler(fakeRepo, fakeUow);
        var command = new UpdateProjectCommand(
            "no-existe", "Title", "Desc", "url", "content");

        var result = await handler.Handle(command);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("ProjectNotFound");
        fakeRepo.UpdateCallCount.Should().Be(0);
        fakeUow.SaveChangesCallCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_WhenUpdateFails_ReturnsFailureAndDoesNotSave()
    {
        var existing = new Project("Title", "my-slug", "url", "desc", "content");
        var fakeRepo = new FakeProjectRepository();
        fakeRepo.Seed(existing);
        fakeRepo.UpdateShouldFail = true;
        var fakeUow = new FakeUnitOfWork();
        var handler = new UpdateProjectCommandHandler(fakeRepo, fakeUow);
        var command = new UpdateProjectCommand(
            "my-slug", "New Title", "New Desc", "new-url", "new-content");

        var result = await handler.Handle(command);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("UpdateFailed");
        fakeUow.SaveChangesCallCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_WithTechnologies_ReplacesTheStoredOnesInTheAuthorOrder()
    {
        var existing = new Project("Old", "my-slug", "old-url", "old-desc", "old-content", new[] { "Old" });
        var fakeRepo = new FakeProjectRepository();
        fakeRepo.Seed(existing);
        var fakeUow = new FakeUnitOfWork();
        var handler = new UpdateProjectCommandHandler(fakeRepo, fakeUow);
        var command = new UpdateProjectCommand(
            "my-slug", "New Title", "New Desc", "new-url", "new-content",
            new[] { "Astro", "TypeScript" });

        var result = await handler.Handle(command, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        existing.Technologies.Should().Equal("Astro", "TypeScript");
    }

    [Fact]
    public async Task Handle_WithNullTechnologies_ClearsTheStoredOnes()
    {
        // El PUT reemplaza la representacion entera: omitir el campo deja el
        // proyecto sin tecnologias, no con las anteriores.
        var existing = new Project("Old", "my-slug", "old-url", "old-desc", "old-content", new[] { "Astro" });
        var fakeRepo = new FakeProjectRepository();
        fakeRepo.Seed(existing);
        var fakeUow = new FakeUnitOfWork();
        var handler = new UpdateProjectCommandHandler(fakeRepo, fakeUow);
        var command = new UpdateProjectCommand(
            "my-slug", "New Title", "New Desc", "new-url", "new-content");

        var result = await handler.Handle(command, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        existing.Technologies.Should().BeEmpty();
    }
}
