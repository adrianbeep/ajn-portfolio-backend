using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using ProjectRepository = Infrastructure.Persistence.Repositories.ProjectRepository;
using Xunit;

namespace Infrastructure.Tests.Persistence;

public class ProjectRepositoryTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly ProjectRepository _repository;

    public ProjectRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite("DataSource=:memory:")
            .Options;

        _db = new AppDbContext(options);
        _db.Database.OpenConnection();
        _db.Database.EnsureCreated();

        // Clear seed data so each test starts with a clean slate
        _db.Projects.RemoveRange(_db.Projects);
        _db.SaveChanges();

        _repository = new ProjectRepository(_db);
    }

    public void Dispose()
    {
        _db.Database.CloseConnection();
        _db.Dispose();
    }

    [Fact]
    public async Task AddAsync_ValidProject_ReturnsSuccess()
    {
        var project = new Project(
            "Test Project",
            "test-project",
            "https://example.com/thumb.png",
            "A test description",
            "# Content"
        );

        var result = await _repository.AddAsync(project);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task AddAsync_Project_IsPersisted()
    {
        var project = new Project(
            "Persisted Project",
            "persisted-project",
            "https://example.com/thumb.png",
            "Description",
            "Content"
        );

        await _repository.AddAsync(project);
        await _db.SaveChangesAsync();

        var retrieved = await _repository.GetBySlugAsync("persisted-project");
        retrieved.Should().NotBeNull();
        retrieved!.Title.Should().Be("Persisted Project");
    }

    [Fact]
    public async Task GetBySlugAsync_ExistingProject_ReturnsProject()
    {
        var project = new Project(
            "Find Me",
            "find-me",
            "https://example.com/thumb.png",
            "Description",
            "Content"
        );

        await _repository.AddAsync(project);
        await _db.SaveChangesAsync();

        var result = await _repository.GetBySlugAsync("find-me");

        result.Should().NotBeNull();
        result!.Title.Should().Be("Find Me");
    }

    [Fact]
    public async Task GetBySlugAsync_NonExistingProject_ReturnsNull()
    {
        var result = await _repository.GetBySlugAsync("non-existing-slug");

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_ExistingProject_ReturnsProject()
    {
        var project = new Project(
            "By Id",
            "by-id",
            "https://example.com/thumb.png",
            "Description",
            "Content"
        );

        await _repository.AddAsync(project);
        await _db.SaveChangesAsync();

        var result = await _repository.GetByIdAsync(project.Id);

        result.Should().NotBeNull();
        result!.Title.Should().Be("By Id");
    }

    [Fact]
    public async Task SoftDeleteAsync_Project_SetsDeletedAt()
    {
        var project = new Project(
            "To Delete",
            "to-delete",
            "https://example.com/thumb.png",
            "Description",
            "Content"
        );

        await _repository.AddAsync(project);
        await _db.SaveChangesAsync();

        var result = await _repository.SoftDeleteAsync(project);
        await _db.SaveChangesAsync();

        result.IsSuccess.Should().BeTrue();
        project.DeletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task SoftDeleteAsync_IsIdempotent()
    {
        var project = new Project(
            "Idempotent Delete",
            "idempotent-delete",
            "https://example.com/thumb.png",
            "Description",
            "Content"
        );

        await _repository.AddAsync(project);
        await _db.SaveChangesAsync();

        await _repository.SoftDeleteAsync(project);
        await _db.SaveChangesAsync();
        var firstDeletedAt = project.DeletedAt;

        await _repository.SoftDeleteAsync(project);
        await _db.SaveChangesAsync();

        project.DeletedAt.Should().Be(firstDeletedAt);
    }

    [Fact]
    public async Task GetAllNotDeletedAsync_ExcludesSoftDeleted()
    {
        var active = new Project("Active", "active", "https://example.com/1.png", "D", "C");
        var deleted = new Project("Deleted", "deleted", "https://example.com/2.png", "D", "C");

        await _repository.AddAsync(active);
        await _repository.AddAsync(deleted);
        await _db.SaveChangesAsync();

        await _repository.SoftDeleteAsync(deleted);
        await _db.SaveChangesAsync();

        var result = await _repository.GetAllNotDeletedAsync();

        result.Should().HaveCount(1);
        result[0].Slug.Should().Be("active");
    }

    [Fact]
    public async Task GetAllAndDeletedAsync_IncludesSoftDeleted()
    {
        var active = new Project("Active", "active-include", "https://example.com/1.png", "D", "C");
        var deleted = new Project("Deleted", "deleted-include", "https://example.com/2.png", "D", "C");

        await _repository.AddAsync(active);
        await _repository.AddAsync(deleted);
        await _db.SaveChangesAsync();

        await _repository.SoftDeleteAsync(deleted);
        await _db.SaveChangesAsync();

        var result = await _repository.GetAllAndDeletedAsync();

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task RestoreAsync_Project_ClearsDeletedAt()
    {
        var project = new Project(
            "To Restore",
            "to-restore",
            "https://example.com/thumb.png",
            "Description",
            "Content"
        );

        await _repository.AddAsync(project);
        await _db.SaveChangesAsync();
        await _repository.SoftDeleteAsync(project);
        await _db.SaveChangesAsync();

        var result = await _repository.RestoreAsync(project);
        await _db.SaveChangesAsync();

        result.IsSuccess.Should().BeTrue();
        project.DeletedAt.Should().BeNull();
    }

    [Fact]
    public async Task RestoreAsync_IsIdempotent()
    {
        var project = new Project(
            "Idempotent Restore",
            "idempotent-restore",
            "https://example.com/thumb.png",
            "Description",
            "Content"
        );

        await _repository.AddAsync(project);
        await _db.SaveChangesAsync();
        await _repository.SoftDeleteAsync(project);
        await _db.SaveChangesAsync();
        await _repository.RestoreAsync(project);
        await _db.SaveChangesAsync();
        var firstRestoredAt = project.DeletedAt;

        await _repository.RestoreAsync(project);
        await _db.SaveChangesAsync();

        project.DeletedAt.Should().Be(firstRestoredAt);
    }

    [Fact]
    public async Task HardDeleteAsync_Project_IsRemoved()
    {
        var project = new Project(
            "To Hard Delete",
            "to-hard-delete",
            "https://example.com/thumb.png",
            "Description",
            "Content"
        );

        await _repository.AddAsync(project);
        await _db.SaveChangesAsync();

        var result = await _repository.HardDeleteAsync(project);
        await _db.SaveChangesAsync();

        result.IsSuccess.Should().BeTrue();
        var retrieved = await _repository.GetByIdAsync(project.Id);
        retrieved.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_Project_TracksChanges()
    {
        var project = new Project(
            "Original Title",
            "update-test",
            "https://example.com/thumb.png",
            "Original Description",
            "Content"
        );

        await _repository.AddAsync(project);
        await _db.SaveChangesAsync();

        project.Update("Updated Title", "Updated Description", "https://example.com/new.png", "New Content");

        var result = await _repository.UpdateAsync(project);
        await _db.SaveChangesAsync();

        result.IsSuccess.Should().BeTrue();

        var retrieved = await _repository.GetBySlugAsync("update-test");
        retrieved!.Title.Should().Be("Updated Title");
        retrieved.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task GetAllNotDeletedAsync_OrdersByCreatedAtDescending()
    {
        var first = new Project("First", "first", "https://example.com/1.png", "D", "C");
        var second = new Project("Second", "second", "https://example.com/2.png", "D", "C");

        await _repository.AddAsync(first);
        await _repository.AddAsync(second);
        await _db.SaveChangesAsync();

        var result = await _repository.GetAllNotDeletedAsync();

        result[0].Slug.Should().Be("second");
        result[1].Slug.Should().Be("first");
    }
}
