using Application.Projects.Commands.DeleteProject;
using Application.Tests.Fakes;
using Domain.Entities;
using FluentAssertions;
using Xunit;

namespace Application.Tests.Projects.DeleteProject;

public class DeleteProjectCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenProjectExists_CallsSoftDeleteAndSaves()
    {
        var existing = new Project("Title", "my-slug", "url", "desc", "content");
        var fakeRepo = new FakeProjectRepository();
        fakeRepo.Seed(existing);
        var fakeUow = new FakeUnitOfWork();
        var handler = new DeleteProjectCommandHandler(fakeRepo, fakeUow);
        var command = new DeleteProjectCommand("my-slug");

        var result = await handler.HandleAsync(command);

        result.IsSuccess.Should().BeTrue();
        fakeRepo.SoftDeleteCallCount.Should().Be(1);
        fakeUow.SaveChangesCallCount.Should().Be(1);
        existing.DeletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task HandleAsync_WhenProjectNotFound_ReturnsFailure()
    {
        var fakeRepo = new FakeProjectRepository();
        var fakeUow = new FakeUnitOfWork();
        var handler = new DeleteProjectCommandHandler(fakeRepo, fakeUow);
        var command = new DeleteProjectCommand("no-existe");

        var result = await handler.HandleAsync(command);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("ProjectNotFound");
        fakeRepo.SoftDeleteCallCount.Should().Be(0);
        fakeUow.SaveChangesCallCount.Should().Be(0);
    }

    [Fact]
    public async Task HandleAsync_WhenSoftDeleteFails_ReturnsFailureAndDoesNotSave()
    {
        var existing = new Project("Title", "my-slug", "url", "desc", "content");
        var fakeRepo = new FakeProjectRepository();
        fakeRepo.Seed(existing);
        fakeRepo.SoftDeleteShouldFail = true;
        var fakeUow = new FakeUnitOfWork();
        var handler = new DeleteProjectCommandHandler(fakeRepo, fakeUow);
        var command = new DeleteProjectCommand("my-slug");

        var result = await handler.HandleAsync(command);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("SoftDeleteFailed");
        fakeUow.SaveChangesCallCount.Should().Be(0);
    }
}
