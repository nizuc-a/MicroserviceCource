using BookingService.IntegrationTests.DatabaseFixtures;
using Xunit;

namespace BookingService.IntegrationTests.DatabaseFixtures;

[CollectionDefinition("Database")]
public class DatabaseCollection : ICollectionFixture<PostgreSqlContainerFixture>;
