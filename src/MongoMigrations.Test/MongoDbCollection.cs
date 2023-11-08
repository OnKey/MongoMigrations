using Xunit;

namespace MongoMigrations.Test
{
    [CollectionDefinition("Mongo collection")]
    public class MongoDbCollection : ICollectionFixture<MongoTestContainer>
    {
        // no code. This is used to make a mongo db instance which is shared amount multiple test classes
    }
}