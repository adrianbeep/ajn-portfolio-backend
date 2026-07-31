using Application.Contributions.Commands.CreateContribution;
using Application.Tests.Fakes;
using FluentAssertions;
using Xunit;

namespace Application.Tests.Contributions.CreateContribution;

public class CreateContributionCommandHandlerTest
{
    [Fact]
    public async Task Handle_WithValidCommand_ReturnsSuccessWithNewId()
    {
        var fakeRepo = new FakeContributionRepository();
        var fakeUow = new FakeUnitOfWork();
        var handler = new CreateContributionCommandHandler(fakeRepo, fakeUow);
        var command = new CreateContributionCommand(
            "Title", "my-slug", "https://img.png", "https://img.png", "Desc", "Content");

        var result = await handler.Handle(command);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBe(Guid.Empty);
        fakeRepo.AddCallCount.Should().Be(1);
        fakeRepo.Contributions.Should().HaveCount(1);
        fakeUow.SaveChangesCallCount.Should().Be(1);
    }

    [Fact]
    public async Task Handle_WhenRepoFails_ReturnsFailureAndDoesNotSave()
    {
        var fakeRepo = new FakeContributionRepository();
        fakeRepo.AddShouldFail = true;
        var fakeUow = new FakeUnitOfWork();
        var handler = new CreateContributionCommandHandler(fakeRepo, fakeUow);
        var command = new CreateContributionCommand(
            "Title", "my-slug", "https://img.png", "https://img.png", "Desc", "Content");

        var result = await handler.Handle(command);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("AddFailed");
        fakeRepo.Contributions.Should().BeEmpty();
        fakeUow.SaveChangesCallCount.Should().Be(0);
    }
}
