using EventService.Domain.Enums;
using EventService.Infrastructure.Repository;
using EventService.IntegrationTests.DatabaseFixtures;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EventService.IntegrationTests;

[Collection("Database")]
public class UserRepositoryTests
{
    private readonly PostgreSqlContainerFixture _container;

    public UserRepositoryTests(PostgreSqlContainerFixture container)
    {
        _container = container;
    }

    private async Task ResetDatabaseAsync() => await _container.ResetDatabaseAsync();

    [Fact]
    public async Task RegisterAsync_CreatesUser()
    {
        await ResetDatabaseAsync();

        await using var context = _container.CreateContext();
        var repository = new UserRepository(context);

        var user = await repository.RegisterAsync("newuser", "hash123", UserRole.User);

        Assert.NotEqual(Guid.Empty, user.Id);
        Assert.Equal("newuser", user.Login);
        Assert.Equal("hash123", user.PasswordHash);
        Assert.Equal(UserRole.User, user.Role);

        await using var verifyContext = _container.CreateContext();
        var savedUser = await verifyContext.Users.FindAsync(user.Id);

        Assert.NotNull(savedUser);
        Assert.Equal("newuser", savedUser.Login);
    }

    [Fact]
    public async Task RegisterAsync_DuplicateLogin_ThrowsDbUpdateException()
    {
        await ResetDatabaseAsync();

        await using var context = _container.CreateContext();
        var repository = new UserRepository(context);

        await repository.RegisterAsync("duplicate", "hash1", UserRole.User);

        await using var verifyContext = _container.CreateContext();
        var verifyRepository = new UserRepository(verifyContext);

        await Assert.ThrowsAsync<DbUpdateException>(() =>
            verifyRepository.RegisterAsync("duplicate", "hash2", UserRole.Admin));
    }

    [Fact]
    public async Task GetUserByIdAsync_ExistingUser_ReturnsUser()
    {
        await ResetDatabaseAsync();

        await using var context = _container.CreateContext();
        var repository = new UserRepository(context);
        var registered = await repository.RegisterAsync("findbyid", "hash", UserRole.Admin);

        await using var verifyContext = _container.CreateContext();
        var verifyRepository = new UserRepository(verifyContext);

        var user = await verifyRepository.GetUserByIdAsync(registered.Id);

        Assert.NotNull(user);
        Assert.Equal(registered.Id, user.Id);
        Assert.Equal("findbyid", user.Login);
        Assert.Equal(UserRole.Admin, user.Role);
    }

    [Fact]
    public async Task GetUserByIdAsync_NonExisting_ReturnsNull()
    {
        await ResetDatabaseAsync();

        await using var context = _container.CreateContext();
        var repository = new UserRepository(context);

        var user = await repository.GetUserByIdAsync(Guid.NewGuid());

        Assert.Null(user);
    }

    [Fact]
    public async Task GetUserByLoginAsync_ExistingLogin_ReturnsUser()
    {
        await ResetDatabaseAsync();

        await using var context = _container.CreateContext();
        var repository = new UserRepository(context);
        var registered = await repository.RegisterAsync("findbylogin", "hash", UserRole.User);

        await using var verifyContext = _container.CreateContext();
        var verifyRepository = new UserRepository(verifyContext);

        var user = await verifyRepository.GetUserByLoginAsync("findbylogin");

        Assert.NotNull(user);
        Assert.Equal(registered.Id, user.Id);
        Assert.Equal("findbylogin", user.Login);
    }

    [Fact]
    public async Task GetUserByLoginAsync_NonExisting_ReturnsNull()
    {
        await ResetDatabaseAsync();

        await using var context = _container.CreateContext();
        var repository = new UserRepository(context);

        var user = await repository.GetUserByLoginAsync("nonexistent");

        Assert.Null(user);
    }
}
