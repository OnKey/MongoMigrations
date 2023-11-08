using System;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace MongoMigrations.Test
{
    /// <summary>
    /// Spins up a local MongoDB in a container for integration testing
    /// </summary>
    public class MongoTestContainer : IDisposable
    {
        private readonly TestcontainersContainer container;
        public IMongoDatabase Db { get; }
        private const int MongoPort = 27017;

        public MongoTestContainer()
        {
            this.container = new TestcontainersBuilder<TestcontainersContainer>()
                .WithImage("mongo:5.0")
                .WithPortBinding(MongoTestContainer.MongoPort, MongoTestContainer.MongoPort)
                .Build();

            this.container.StartAsync().Wait();

            this.Db = this.GetDb();
        }
        public void Dispose()
        {
            this.container.DisposeAsync().AsTask().Wait();
        }

        private IMongoDatabase GetDb()
        {
            var settings = MongoClientSettings.FromConnectionString("mongodb://localhost:27017");
            settings.LinqProvider = LinqProvider.V3;

            var client = new MongoClient(settings);
            var pack = new ConventionPack
            {
                new EnumRepresentationConvention(BsonType.String)
            };

            ConventionRegistry.Register("CustomConventions", pack, t => true);

            return client.GetDatabase("testDb");
        }
    }
}