using Xunit;

namespace EventService.IntegrationTests.DatabaseFixtures;

[CollectionDefinition("Database")]
public class DatabaseCollection : ICollectionFixture<PostgreSqlContainerFixture>;