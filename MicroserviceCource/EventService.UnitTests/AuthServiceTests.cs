using EventService.Application.Abstractions.Auth;
using EventService.Application.Abstractions.Repositories;
using EventService.Application.Services;
using EventService.Domain.Entities;
using EventService.Domain.Enums;
using EventService.Domain.Exceptions;
using Moq;

namespace EventService.UnitTests;

public class AuthServiceTests
{
    private const string Login = "user1";
    private const string Password = "password123";
    private const string PasswordHash = "hashed-password";
    private const string Token = "jwt-token";
    private const string InvalidCredentialsMessage = "Invalid login or password.";

    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly Mock<IPasswordHasher> _passwordHasherMock = new();
    private readonly Mock<ITokenGenerator> _tokenGeneratorMock = new();
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        _passwordHasherMock
            .Setup(x => x.HashPassword(Password))
            .Returns(PasswordHash);

        _authService = new AuthService(
            _userRepositoryMock.Object,
            _passwordHasherMock.Object,
            _tokenGeneratorMock.Object);
    }

    #region Login

    [Fact]
    public async Task LoginAsync_Correct()
    {
        var user = new User(Login, PasswordHash);
        _userRepositoryMock
            .Setup(x => x.GetUserByLoginAsync(Login, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _tokenGeneratorMock
            .Setup(x => x.GenerateToken(user))
            .Returns(Token);

        var result = await _authService.LoginAsync(Login, Password);

        Assert.Equal(Token, result);
        _passwordHasherMock.Verify(x => x.HashPassword(Password), Times.Once);
        _tokenGeneratorMock.Verify(x => x.GenerateToken(user), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_UserNotFoundException()
    {
        _userRepositoryMock
            .Setup(x => x.GetUserByLoginAsync(Login, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var exception = await Assert.ThrowsAsync<UserNotFoundException>(async () =>
            await _authService.LoginAsync(Login, Password));

        Assert.Equal(InvalidCredentialsMessage, exception.Message);
        _tokenGeneratorMock.Verify(x => x.GenerateToken(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_InvalidPassword_UserNotFoundException()
    {
        var user = new User(Login, "other-hash");
        _userRepositoryMock
            .Setup(x => x.GetUserByLoginAsync(Login, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var exception = await Assert.ThrowsAsync<UserNotFoundException>(async () =>
            await _authService.LoginAsync(Login, Password));

        Assert.Equal(InvalidCredentialsMessage, exception.Message);
        _tokenGeneratorMock.Verify(x => x.GenerateToken(It.IsAny<User>()), Times.Never);
    }

    #endregion

    #region Register

    [Fact]
    public async Task RegisterAsync_Correct()
    {
        _userRepositoryMock
            .Setup(x => x.GetUserByLoginAsync(Login, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        _userRepositoryMock
            .Setup(x => x.RegisterAsync(Login, PasswordHash, UserRole.User, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User(Login, PasswordHash, UserRole.User));

        await _authService.RegisterAsync(Login, Password, UserRole.User);

        _passwordHasherMock.Verify(x => x.HashPassword(Password), Times.Once);
        _userRepositoryMock.Verify(
            x => x.RegisterAsync(Login, PasswordHash, UserRole.User, It.IsAny<CancellationToken>()),
            Times.Once);
        _tokenGeneratorMock.Verify(x => x.GenerateToken(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_LoginIsBusy_AuthenticationFailedException()
    {
        var existingUser = new User(Login, PasswordHash);
        _userRepositoryMock
            .Setup(x => x.GetUserByLoginAsync(Login, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);

        var exception = await Assert.ThrowsAsync<AuthenticationFailedException>(async () =>
            await _authService.RegisterAsync(Login, Password, UserRole.User));

        Assert.Equal("Login is already taken.", exception.Message);
        _userRepositoryMock.Verify(
            x => x.RegisterAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<UserRole>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _tokenGeneratorMock.Verify(x => x.GenerateToken(It.IsAny<User>()), Times.Never);
    }

    #endregion
}
