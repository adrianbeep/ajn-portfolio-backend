using Application.Contributions.Commands.UpdateContribution;
using Application.Tests.Fakes;
using Domain.Entities;
using FluentAssertions;
using Xunit;

namespace Application.Tests.Contributions.UpdateContribution;

public class UpdateContributionCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenContributionExists_UpdatesAndSaves()
    {
        var existing = new Contribution("Old", "my-slug", "old-url", "old-pr", "old-desc", "old-content");
        var fakeRepo = new FakeContributionRepository();
        fakeRepo.Seed(existing);
        var fakeUow = new FakeUnitOfWork();
        var handler = new UpdateContributionCommandHandler(fakeRepo, fakeUow);
        var command = new UpdateContributionCommand(
            "my-slug", "New Title", "New Desc", "new-url", "new-pr", "new-content");

        var result = await handler.Handle(command);

        result.IsSuccess.Should().BeTrue();
        fakeRepo.UpdateCallCount.Should().Be(1);
        fakeUow.SaveChangesCallCount.Should().Be(1);
        existing.Title.Should().Be("New Title");
        existing.Description.Should().Be("New Desc");
        existing.UrlThumbnail.Should().Be("new-url");
        existing.UrlPrContribution.Should().Be("new-pr");
        existing.Content.Should().Be("new-content");
    }

    [Fact]
    public async Task Handle_WhenContributionNotFound_ReturnsFailure()
    {
        var fakeRepo = new FakeContributionRepository();
        var fakeUow = new FakeUnitOfWork();
        var handler = new UpdateContributionCommandHandler(fakeRepo, fakeUow);
        var command = new UpdateContributionCommand(
            "no-existe", "Title", "Desc", "url", "pr", "content");

        var result = await handler.Handle(command);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("ContributionNotFound");
        fakeRepo.UpdateCallCount.Should().Be(0);
        fakeUow.SaveChangesCallCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_WhenUpdateFails_ReturnsFailureAndDoesNotSave()
    {
        var existing = new Contribution("Title", "my-slug", "url", "pr", "desc", "content");
        var fakeRepo = new FakeContributionRepository();
        fakeRepo.Seed(existing);
        fakeRepo.UpdateShouldFail = true;
        var fakeUow = new FakeUnitOfWork();
        var handler = new UpdateContributionCommandHandler(fakeRepo, fakeUow);
        var command = new UpdateContributionCommand(
            "my-slug", "New Title", "New Desc", "new-url", "new-pr", "new-content");

        var result = await handler.Handle(command);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("UpdateFailed");
        fakeUow.SaveChangesCallCount.Should().Be(0);
    }
}
