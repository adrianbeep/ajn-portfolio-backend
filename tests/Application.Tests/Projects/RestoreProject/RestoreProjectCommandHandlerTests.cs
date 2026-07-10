using Application.Projects.Commands.RestoreProject;
using Application.Tests.Fakes;
using Domain.Entities;
using FluentAssertions;
using Xunit;

namespace Application.Tests.Projects.RestoreProject;

public class RestoreProjectCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenProjectExists_CallsRestoreAndSaves()
    {
        var existing = new Project("Title", "my-slug", "url", "desc", "content");
        existing.MarkAsDeleted();
        var fakeRepo = new FakeProjectRepository();
        fakeRepo.Seed(existing);
        var fakeUow = new FakeUnitOfWork();
        var handler = new RestoreProjectCommandHandler(fakeRepo, fakeUow);
        var command = new RestoreProjectCommand("my-slug");

        var result = await handler.Handle(command);

        result.IsSuccess.Should().BeTrue();
        fakeRepo.RestoreCallCount.Should().Be(1);
        fakeUow.SaveChangesCallCount.Should().Be(1);
        existing.DeletedAt.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WhenProjectNotFound_ReturnsFailure()
    {
        var fakeRepo = new FakeProjectRepository();
        var fakeUow = new FakeUnitOfWork();
        var handler = new RestoreProjectCommandHandler(fakeRepo, fakeUow);
        var command = new RestoreProjectCommand("no-existe");

        var result = await handler.Handle(command);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("ProjectNotFound");
        fakeRepo.RestoreCallCount.Should().Be(0);
        fakeUow.SaveChangesCallCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_WhenRestoreFails_ReturnsFailureAndDoesNotSave()
    {
        var existing = new Project("Title", "my-slug", "url", "desc", "content");
        existing.MarkAsDeleted();
        var fakeRepo = new FakeProjectRepository();
        fakeRepo.Seed(existing);
        fakeRepo.RestoreShouldFail = true;
        var fakeUow = new FakeUnitOfWork();
        var handler = new RestoreProjectCommandHandler(fakeRepo, fakeUow);
        var command = new RestoreProjectCommand("my-slug");

        var result = await handler.Handle(command);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("RestoreFailed");
        fakeUow.SaveChangesCallCount.Should().Be(0);
    }
}
