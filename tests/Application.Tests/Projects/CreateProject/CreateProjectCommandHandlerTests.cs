using Application.Abstractions.Common;
using Application.Projects.Commands.CreateProject;
using Application.Tests.Fakes;
using FluentAssertions;
using Xunit;

namespace Application.Tests.Projects.CreateProject;

public class CreateProjectCommandHandlerTests
{
    [Fact]
    public async Task Handle_WithValidCommand_ReturnsSuccessWithNewId()
    {
        var fakeRepo = new FakeProjectRepository();
        var fakeUow = new FakeUnitOfWork();
        var handler = new CreateProjectCommandHandler(fakeRepo, fakeUow);
        var command = new CreateProjectCommand(
            "Title", "my-slug", "https://img.png", "Desc", "Content");

        var result = await handler.Handle(command);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBe(Guid.Empty);
        fakeRepo.AddCallCount.Should().Be(1);
        fakeRepo.Projects.Should().HaveCount(1);
        fakeUow.SaveChangesCallCount.Should().Be(1);
    }

    [Fact]
    public async Task Handle_WhenRepoFails_ReturnsFailureAndDoesNotSave()
    {
        var fakeRepo = new FakeProjectRepository();
        fakeRepo.AddShouldFail = true;
        var fakeUow = new FakeUnitOfWork();
        var handler = new CreateProjectCommandHandler(fakeRepo, fakeUow);
        var command = new CreateProjectCommand(
            "Title", "my-slug", "https://img.png", "Desc", "Content");

        var result = await handler.Handle(command);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("AddFailed");
        fakeRepo.Projects.Should().BeEmpty();
        fakeUow.SaveChangesCallCount.Should().Be(0);
    }
}
