using System.Threading.Tasks;
using MongoDB.Driver;
using MongoMigration.Sample.Entites;
using MongoMigrations.Collections;
using MongoMigrations.Migrations;

namespace MongoMigration.Sample.Migrations
{
    public class Db_AddIndex : IDatabaseMigration
    {
        private const string IndexName = "Users_FirstName";
        private IMongoDatabase db;
        private ICollectionNameResolver nameResolver;

        public Db_AddIndex(IMongoDatabase db, ICollectionNameResolver nameResolver)
        {
            this.db = db;
            this.nameResolver = nameResolver;
        }

        public int Version { get; } = 1;
        public async Task Up()
        {
            var collection = this.db.GetCollection<User>(this.nameResolver.GetCollectionName<User>());

            var indexKeysDefinition = Builders<User>.IndexKeys.Ascending(x => x.FirstName);

            await collection.Indexes
                .CreateOneAsync(new CreateIndexModel<User>(indexKeysDefinition,
                    new CreateIndexOptions { Unique = true, Name = IndexName }));
        }

        public async Task Down()
        {
            var collection = this.db.GetCollection<User>(this.nameResolver.GetCollectionName<User>());
            await collection.Indexes.DropOneAsync(IndexName);
        }
    }
}