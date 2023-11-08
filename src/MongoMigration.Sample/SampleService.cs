using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoMigration.Sample.Entites;
using MongoMigrations.Collections;

namespace MongoMigration.Sample
{
    public class SampleService : IHostedService
    {
        private ILogger<SampleService> log;
        private ICollectionNameResolver collectionNameResolver;
        private readonly IMongoDatabase db;

        public SampleService(ILogger<SampleService> log, IMongoDatabase db, ICollectionNameResolver collectionNameResolver)
        {
            this.log = log;
            this.collectionNameResolver = collectionNameResolver;
            this.db = db;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            this.log.LogInformation("Started application");
            await this.SaveOldUser();
            
            // Migrations can be applied either at app startup or on access to docs
            var user = await this.GetUserFromMongo();
            this.log.LogInformation("User's full name is: {fullname}", user.FullName);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        private async Task SaveOldUser()
        {
            var oldUser = new BsonDocument
            {
                {"_id", Guid.Parse("3BDE2E44-4D7A-4432-BF2B-3993EE2C389B")},
                {"FirstName", "Bob"},
                {"LastName", "Smith"}
            };
            
            var collection = db.GetCollection<BsonDocument>(collectionNameResolver.GetCollectionName<User>());
            await collection.ReplaceOneAsync(doc => doc["_id"] == oldUser["_id"], oldUser, new ReplaceOptions {IsUpsert = true});
        }

        private async Task<User> GetUserFromMongo()
        {
            var collection = db.GetCollection<User>(collectionNameResolver.GetCollectionName<User>());
            var user = await collection
                .Find(x => x.Id == Guid.Parse("3BDE2E44-4D7A-4432-BF2B-3993EE2C389B"))
                .FirstOrDefaultAsync();

            if (user == null)
            {
                throw new Exception("Could find user with expected ID");
            }
            
            return user;
        }
    }
}