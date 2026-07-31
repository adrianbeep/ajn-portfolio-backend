using Application.Contributions.Commands.RestoreContribution;
using Application.Tests.Fakes;
using Domain.Entities;
using FluentAssertions;
using Xunit;

namespace Application.Tests.Contributions.RestoreContribution;

public class RestoreContributionCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenContributionExists_CallsRestoreAndSaves()
    {
        var existing = new Contribution("Title", "my-slug", "url", "url-pr", "desc", "content");
        existing.MarkAsDeleted();
        var fakeRepo = new FakeContributionRepository();
        fakeRepo.Seed(existing);
        var fakeUow = new FakeUnitOfWork();
        var handler = new RestoreContributionCommandHandler(fakeRepo, fakeUow);
        var command = new RestoreContributionCommand("my-slug");

        var result = await handler.Handle(command);

        result.IsSuccess.Should().BeTrue();
        fakeRepo.RestoreCallCount.Should().Be(1);
        fakeUow.SaveChangesCallCount.Should().Be(1);
        existing.DeletedAt.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WhenContributionNotFound_ReturnsFailure()
    {
        var fakeRepo = new FakeContributionRepository();
        var fakeUow = new FakeUnitOfWork();
        var handler = new RestoreContributionCommandHandler(fakeRepo, fakeUow);
        var command = new RestoreContributionCommand("no-existe");

        var result = await handler.Handle(command);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("ContributionNotFound");
        fakeRepo.RestoreCallCount.Should().Be(0);
        fakeUow.SaveChangesCallCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_WhenRestoreFails_ReturnsFailureAndDoesNotSave()
    {
        var existing = new Contribution("Title", "my-slug", "url", "url-pr", "desc", "content");
        existing.MarkAsDeleted();
        var fakeRepo = new FakeContributionRepository();
        fakeRepo.Seed(existing);
        fakeRepo.RestoreShouldFail = true;
        var fakeUow = new FakeUnitOfWork();
        var handler = new RestoreContributionCommandHandler(fakeRepo, fakeUow);
        var command = new RestoreContributionCommand("my-slug");

        var result = await handler.Handle(command);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("RestoreFailed");
        fakeUow.SaveChangesCallCount.Should().Be(0);
    }
}
