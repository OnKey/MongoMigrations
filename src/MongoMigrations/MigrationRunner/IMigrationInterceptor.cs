using MongoDB.Bson.Serialization;

namespace MongoMigrations.MigrationRunner
{
    public interface IMigrationInterceptor<TDocument> : IBsonSerializer
    {
    }
}