using UserService.IntegrationTests.DatabaseFixtures;
using Xunit;

namespace UserService.IntegrationTests.DatabaseFixtures;

[CollectionDefinition("Database")]
public class DatabaseCollection : ICollectionFixture<PostgreSqlContainerFixture>;
