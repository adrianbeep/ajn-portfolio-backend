using Domain.Entities;
using FluentAssertions;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using ContributionRepository = Infrastructure.Persistence.Repositories.ContributionRepository;
using Xunit;

namespace Infrastructure.Tests.Persistence;

public class ContributionRepositoryTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly ContributionRepository _repository;

    public ContributionRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite("DataSource=:memory:")
            .Options;

        _db = new AppDbContext(options);
        _db.Database.OpenConnection();
        _db.Database.EnsureCreated();

        _db.Contributions.RemoveRange(_db.Contributions);
        _db.SaveChanges();

        _repository = new ContributionRepository(_db);
    }

    public void Dispose()
    {
        _db.Database.CloseConnection();
        _db.Dispose();
    }

    [Fact]
    public async Task AddAsync_ValidContribution_ReturnsSuccess()
    {
        var contribution = new Contribution(
            "Test Contribution",
            "test-contribution",
            "https://example.com/thumb.png",
            "https://github.com/pr/1",
            "A test description",
            "# Content"
        );

        var result = await _repository.AddAsync(contribution);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task AddAsync_Contribution_IsPersisted()
    {
        var contribution = new Contribution(
            "Persisted",
            "persisted-contribution",
            "https://example.com/thumb.png",
            "https://github.com/pr/2",
            "Description",
            "Content"
        );

        await _repository.AddAsync(contribution);
        await _db.SaveChangesAsync();

        var retrieved = await _repository.GetBySlugAsync("persisted-contribution");
        retrieved.Should().NotBeNull();
        retrieved!.Title.Should().Be("Persisted");
        retrieved.UrlPrContribution.Should().Be("https://github.com/pr/2");
    }

    [Fact]
    public async Task GetBySlugAsync_ExistingContribution_ReturnsContribution()
    {
        var contribution = new Contribution(
            "Find Me",
            "find-me-contribution",
            "https://example.com/thumb.png",
            "https://github.com/pr/3",
            "Description",
            "Content"
        );

        await _repository.AddAsync(contribution);
        await _db.SaveChangesAsync();

        var result = await _repository.GetBySlugAsync("find-me-contribution");

        result.Should().NotBeNull();
        result!.Title.Should().Be("Find Me");
    }

    [Fact]
    public async Task GetBySlugAsync_NonExistingContribution_ReturnsNull()
    {
        var result = await _repository.GetBySlugAsync("non-existing-slug");

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_ExistingContribution_ReturnsContribution()
    {
        var contribution = new Contribution(
            "By Id",
            "by-id-contribution",
            "https://example.com/thumb.png",
            "https://github.com/pr/4",
            "Description",
            "Content"
        );

        await _repository.AddAsync(contribution);
        await _db.SaveChangesAsync();

        var result = await _repository.GetByIdAsync(contribution.Id);

        result.Should().NotBeNull();
        result!.Title.Should().Be("By Id");
    }

    [Fact]
    public async Task SoftDeleteAsync_Contribution_SetsDeletedAt()
    {
        var contribution = new Contribution(
            "To Delete",
            "to-delete-contribution",
            "https://example.com/thumb.png",
            "https://github.com/pr/5",
            "Description",
            "Content"
        );

        await _repository.AddAsync(contribution);
        await _db.SaveChangesAsync();

        var result = await _repository.SoftDeleteAsync(contribution);
        await _db.SaveChangesAsync();

        result.IsSuccess.Should().BeTrue();
        contribution.DeletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task SoftDeleteAsync_IsIdempotent()
    {
        var contribution = new Contribution(
            "Idempotent Delete",
            "idempotent-delete-contribution",
            "https://example.com/thumb.png",
            "https://github.com/pr/6",
            "Description",
            "Content"
        );

        await _repository.AddAsync(contribution);
        await _db.SaveChangesAsync();

        await _repository.SoftDeleteAsync(contribution);
        await _db.SaveChangesAsync();
        var firstDeletedAt = contribution.DeletedAt;

        await _repository.SoftDeleteAsync(contribution);
        await _db.SaveChangesAsync();

        contribution.DeletedAt.Should().Be(firstDeletedAt);
    }

    [Fact]
    public async Task GetAllNotDeletedAsync_ExcludesSoftDeleted()
    {
        var active = new Contribution("Active", "active-contribution", "https://example.com/1.png", "https://github.com/pr/7", "D", "C");
        var deleted = new Contribution("Deleted", "deleted-contribution", "https://example.com/2.png", "https://github.com/pr/8", "D", "C");

        await _repository.AddAsync(active);
        await _repository.AddAsync(deleted);
        await _db.SaveChangesAsync();

        await _repository.SoftDeleteAsync(deleted);
        await _db.SaveChangesAsync();

        var result = await _repository.GetAllNotDeletedAsync();

        result.Should().HaveCount(1);
        result[0].Slug.Should().Be("active-contribution");
    }

    [Fact]
    public async Task GetAllAndDeletedAsync_IncludesSoftDeleted()
    {
        var active = new Contribution("Active", "active-include-contribution", "https://example.com/1.png", "https://github.com/pr/9", "D", "C");
        var deleted = new Contribution("Deleted", "deleted-include-contribution", "https://example.com/2.png", "https://github.com/pr/10", "D", "C");

        await _repository.AddAsync(active);
        await _repository.AddAsync(deleted);
        await _db.SaveChangesAsync();

        await _repository.SoftDeleteAsync(deleted);
        await _db.SaveChangesAsync();

        var result = await _repository.GetAllAndDeletedAsync();

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task RestoreAsync_Contribution_ClearsDeletedAt()
    {
        var contribution = new Contribution(
            "To Restore",
            "to-restore-contribution",
            "https://example.com/thumb.png",
            "https://github.com/pr/11",
            "Description",
            "Content"
        );

        await _repository.AddAsync(contribution);
        await _db.SaveChangesAsync();
        await _repository.SoftDeleteAsync(contribution);
        await _db.SaveChangesAsync();

        var result = await _repository.RestoreAsync(contribution);
        await _db.SaveChangesAsync();

        result.IsSuccess.Should().BeTrue();
        contribution.DeletedAt.Should().BeNull();
    }

    [Fact]
    public async Task RestoreAsync_IsIdempotent()
    {
        var contribution = new Contribution(
            "Idempotent Restore",
            "idempotent-restore-contribution",
            "https://example.com/thumb.png",
            "https://github.com/pr/12",
            "Description",
            "Content"
        );

        await _repository.AddAsync(contribution);
        await _db.SaveChangesAsync();
        await _repository.SoftDeleteAsync(contribution);
        await _db.SaveChangesAsync();
        await _repository.RestoreAsync(contribution);
        await _db.SaveChangesAsync();
        var firstRestoredAt = contribution.DeletedAt;

        await _repository.RestoreAsync(contribution);
        await _db.SaveChangesAsync();

        contribution.DeletedAt.Should().Be(firstRestoredAt);
    }

    [Fact]
    public async Task HardDeleteAsync_Contribution_IsRemoved()
    {
        var contribution = new Contribution(
            "To Hard Delete",
            "to-hard-delete-contribution",
            "https://example.com/thumb.png",
            "https://github.com/pr/13",
            "Description",
            "Content"
        );

        await _repository.AddAsync(contribution);
        await _db.SaveChangesAsync();

        var result = await _repository.HardDeleteAsync(contribution);
        await _db.SaveChangesAsync();

        result.IsSuccess.Should().BeTrue();
        var retrieved = await _repository.GetByIdAsync(contribution.Id);
        retrieved.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_Contribution_TracksChanges()
    {
        var contribution = new Contribution(
            "Original Title",
            "update-contribution-test",
            "https://example.com/thumb.png",
            "https://github.com/pr/14",
            "Original Description",
            "Content"
        );

        await _repository.AddAsync(contribution);
        await _db.SaveChangesAsync();

        contribution.Update("Updated Title", "Updated Description", "https://example.com/new.png", "https://github.com/pr/14-new", "New Content");

        var result = await _repository.UpdateAsync(contribution);
        await _db.SaveChangesAsync();

        result.IsSuccess.Should().BeTrue();

        var retrieved = await _repository.GetBySlugAsync("update-contribution-test");
        retrieved!.Title.Should().Be("Updated Title");
        retrieved.UrlPrContribution.Should().Be("https://github.com/pr/14-new");
        retrieved.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task GetAllNotDeletedAsync_OrdersByCreatedAtDescending()
    {
        var first = new Contribution("First", "first-contribution", "https://example.com/1.png", "https://github.com/pr/15", "D", "C");
        var second = new Contribution("Second", "second-contribution", "https://example.com/2.png", "https://github.com/pr/16", "D", "C");

        await _repository.AddAsync(first);
        await _repository.AddAsync(second);
        await _db.SaveChangesAsync();

        var result = await _repository.GetAllNotDeletedAsync();

        result[0].Slug.Should().Be("second-contribution");
        result[1].Slug.Should().Be("first-contribution");
    }

    [Fact]
    public async Task GetPagedNotDeletedAsync_ReturnsPageAndRespectsSoftDelete()
    {
        var c1 = new Contribution("A", "c1-contribution", "https://example.com/1.png", "https://github.com/pr/17", "D", "C");
        var c2 = new Contribution("B", "c2-contribution", "https://example.com/2.png", "https://github.com/pr/18", "D", "C");
        var c3 = new Contribution("C", "c3-contribution", "https://example.com/3.png", "https://github.com/pr/19", "D", "C");

        await _repository.AddAsync(c1);
        await _repository.AddAsync(c2);
        await _repository.AddAsync(c3);
        await _db.SaveChangesAsync();

        await _repository.SoftDeleteAsync(c1);
        await _db.SaveChangesAsync();

        var result = await _repository.GetPagedNotDeletedAsync(1, 10);

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task CountNotDeletedAsync_ExcludesSoftDeleted()
    {
        var c1 = new Contribution("A", "count-c1", "https://example.com/1.png", "https://github.com/pr/20", "D", "C");
        var c2 = new Contribution("B", "count-c2", "https://example.com/2.png", "https://github.com/pr/21", "D", "C");

        await _repository.AddAsync(c1);
        await _repository.AddAsync(c2);
        await _db.SaveChangesAsync();

        await _repository.SoftDeleteAsync(c2);
        await _db.SaveChangesAsync();

        var count = await _repository.CountNotDeletedAsync();

        count.Should().Be(1);
    }
}
