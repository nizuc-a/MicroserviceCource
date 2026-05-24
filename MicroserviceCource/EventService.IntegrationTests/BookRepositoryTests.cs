using EventService.Api.Data;
using EventService.Api.Repository;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Xunit;

namespace EventService.IntegrationTests;

public class BookRepositoryTests: IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine")
        .Build();
    
    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
    }
    
    private AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;

        var context = new AppDbContext(options);
        context.Database.EnsureCreated();
        return context;
    } 
    
    [Fact]
    public async Task CreateBook_SavesBookToDatabase()
    {
        // Arrange
        await using var context = CreateContext();
        var repository = new BookingRepository(context);
        
        var book = new Book { Title = "Чистый код", Isbn = "978-5-4461-0960-9", Price = 1200m };

        // Act
        await repository.CreateAsync(book);

        // Assert — читаем из реальной БД через отдельный контекст
        await using var verifyContext = CreateContext();
        var saved = await verifyContext.Books
            .FirstOrDefaultAsync(b => b.Isbn == "978-5-4461-0960-9");

        Assert.NotNull(saved);
        Assert.Equal("Чистый код", saved.Title);
    } 
}