using Application.Contributions.Commands.DeleteContribution;
using Application.Tests.Fakes;
using Domain.Entities;
using FluentAssertions;
using Xunit;

namespace Application.Tests.Contributions.DeleteContribution;

public class DeleteContributionCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenProjectExists_CallsSoftDeleteAndSaves()
    {
        var existing = new Contribution("Title", "my-slug", "url", "url" ,"desc", "content");
        var fakeRepo = new FakeContributionRepository();
        fakeRepo.Seed(existing);
        var fakeUow = new FakeUnitOfWork();
        var handler = new DeleteContributionCommandHandler(fakeRepo, fakeUow);
        var command = new DeleteContributionCommand("my-slug");

        var result = await handler.Handle(command);

        result.IsSuccess.Should().BeTrue();
        fakeRepo.SoftDeleteCallCount.Should().Be(1);
        fakeUow.SaveChangesCallCount.Should().Be(1);
        existing.DeletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_WhenProjectNotFound_ReturnsFailure()
    {
        var fakeRepo = new FakeContributionRepository();
        var fakeUow = new FakeUnitOfWork();
        var handler = new DeleteContributionCommandHandler(fakeRepo, fakeUow);
        var command = new DeleteContributionCommand("no-existe");

        var result = await handler.Handle(command);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("ContributionNotFound");
        fakeRepo.SoftDeleteCallCount.Should().Be(0);
        fakeUow.SaveChangesCallCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_WhenSoftDeleteFails_ReturnsFailureAndDoesNotSave()
    {
        var existing = new Contribution("Title", "my-slug", "url", "url", "desc", "content");
        var fakeRepo = new FakeContributionRepository();
        fakeRepo.Seed(existing);
        fakeRepo.SoftDeleteShouldFail = true;
        var fakeUow = new FakeUnitOfWork();
        var handler = new DeleteContributionCommandHandler(fakeRepo, fakeUow);
        var command = new DeleteContributionCommand("my-slug");

        var result = await handler.Handle(command);

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("SoftDeleteFailed");
        fakeUow.SaveChangesCallCount.Should().Be(0);
    }
}
