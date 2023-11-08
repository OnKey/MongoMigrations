using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoMigrations.Collections;
using MongoMigrations.MigrationRunner;
using MongoMigrations.Migrations;
using NSubstitute;
using Xunit;

namespace MongoMigrations.Test.MigrationRunner
{
    [Collection("Mongo collection")]
    public class DocumentMigrationRunnerTest
    {
        private List<IDocumentMigration> migrations = new();
        private readonly IMongoCollection<TestEntity> dbCollection;
        private readonly IVersionLocator versionLocator;
        private readonly IMongoCollection<BsonDocument> bsonCollection;
        private readonly IMongoDatabase db;
        private readonly ICollectionNameResolver nameResolver;

        public DocumentMigrationRunnerTest(MongoTestContainer mongo)
        {
            this.db = mongo.Db;
            db.DropCollection("DocumentMigration", CancellationToken.None);
            this.nameResolver = new CollectionNameResolver()
                .AddType(typeof(TestEntity), "DocumentMigration")
                .AddType(typeof(TestUser), "User");

            this.versionLocator = Substitute.For<IVersionLocator>();
            this.versionLocator.VersionFieldName().Returns(VersionLocator.DocumentSchemaVersionFieldName);
            // needed to make sure the version interceptor is added for TestEntity when initialising the bson pipeline
            this.versionLocator.IsVersioned(typeof(TestEntity)).Returns(true);
            this.versionLocator.GetTargetVersion(typeof(TestEntity)).Returns(0);

            // this is needed, as the version are added in the global bson serialisation pipeline
            SerialiserHelper.RegisterVersionInterceptor(this.versionLocator);

            this.dbCollection = db.GetCollection<TestEntity>(nameResolver.GetCollectionName<TestEntity>());
            this.bsonCollection = db.GetCollection<BsonDocument>(nameResolver.GetCollectionName<TestEntity>());
        }

        [Fact]
        public async Task ShouldMigrateDocument()
        {
            var e = new TestEntity { Id = Guid.Empty };
            await this.dbCollection.InsertOneAsync(e);

            var migration1 = new TestMigration(1, document => document["Value"] = "updated", _ => { });
            this.migrations.Add(migration1);
            this.versionLocator.GetTargetVersion(typeof(TestEntity)).Returns(1);
            var sut = new DocumentMigrationRunner(db, this.migrations, nameResolver, this.versionLocator, NullLogger<DocumentMigrationRunner>.Instance);

            await sut.MigrateType(typeof(TestEntity), MigrationTiming.AtAppStart);
            var actual = await this.dbCollection.Find(x => x.Id == e.Id).FirstOrDefaultAsync();
            var actualVersion = await this.GetVersionFromDb(e.Id);

            actualVersion.Should().Be(1);
            actual.Value.Should().Be("updated");
        }

        [Fact]
        public async Task ShouldRunMigrationsInOrder()
        {
            var e = new TestEntity { Id = Guid.Empty };
            await this.dbCollection.InsertOneAsync(e);

            var migration1 = this.AddFakeMigration(1);
            var migration2 = this.AddFakeMigration(2);
            this.versionLocator.GetTargetVersion(typeof(TestEntity)).Returns(2);
            var sut = new DocumentMigrationRunner(db, this.migrations, nameResolver, this.versionLocator, NullLogger<DocumentMigrationRunner>.Instance);

            await sut.MigrateType(typeof(TestEntity), MigrationTiming.AtAppStart);
            var actualVersion = await this.GetVersionFromDb(e.Id);

            actualVersion.Should().Be(2);
            migration1.Received().Up(Arg.Any<BsonDocument>());
            migration1.DidNotReceive().Down(Arg.Any<BsonDocument>());
            migration2.Received().Up(Arg.Any<BsonDocument>());
            migration2.DidNotReceive().Down(Arg.Any<BsonDocument>());
        }

        [Fact]
        public async Task ShouldDowngradeDocument()
        {
            this.versionLocator.GetTargetVersion(typeof(TestEntity)).Returns(3);
            var e = new TestEntity { Id = Guid.Empty, Value = "before" };
            await this.dbCollection.InsertOneAsync(e);

            var migration1 = this.AddFakeMigration(1);
            var migration2 = new TestMigration(2, _ => { }, doc => doc["Value"] = "after");
            this.migrations.Add(migration2);
            var migration3 = this.AddFakeMigration(3);
            this.versionLocator.GetTargetVersion(typeof(TestEntity)).Returns(1);
            var sut = new DocumentMigrationRunner(db, this.migrations, nameResolver, this.versionLocator, NullLogger<DocumentMigrationRunner>.Instance);

            await sut.MigrateType(typeof(TestEntity), MigrationTiming.AtAppStart);
            var actual = await this.dbCollection.Find(x => x.Id == e.Id).FirstOrDefaultAsync();
            var actualVersion = await this.GetVersionFromDb(e.Id);

            actualVersion.Should().Be(1);
            actual.Value.Should().Be("after");
            migration3.DidNotReceive().Up(Arg.Any<BsonDocument>());
            migration3.Received().Down(Arg.Any<BsonDocument>());
            migration1.DidNotReceive().Up(Arg.Any<BsonDocument>());
            migration1.DidNotReceive().Down(Arg.Any<BsonDocument>());
        }

        [Fact]
        public async Task ShouldOnlyRunRequiredMigrations()
        {
            this.versionLocator.GetTargetVersion(typeof(TestEntity)).Returns(1);
            var e = new TestEntity { Id = Guid.Empty, Value = "before" };
            await this.dbCollection.InsertOneAsync(e);

            var migration1 = this.AddFakeMigration(1);
            var migration2 = this.AddFakeMigration(2);
            var migration3 = this.AddFakeMigration(3);
            this.versionLocator.GetTargetVersion(typeof(TestEntity)).Returns(2);
            var sut = new DocumentMigrationRunner(db, this.migrations, nameResolver, this.versionLocator, NullLogger<DocumentMigrationRunner>.Instance);

            await sut.MigrateAllTypes(MigrationTiming.AtAppStart);
            var actualVersion = await this.GetVersionFromDb(e.Id);

            actualVersion.Should().Be(2);
            migration1.DidNotReceive().Up(Arg.Any<BsonDocument>());
            migration2.Received().Up(Arg.Any<BsonDocument>());
            migration3.DidNotReceive().Up(Arg.Any<BsonDocument>());
        }

        [Fact]
        public async Task ShouldOnlyRunMigrationsForCorrectType()
        {
            var e = new TestEntity { Id = Guid.Empty, Value = "before" };
            await this.dbCollection.InsertOneAsync(e);

            var migration1 = this.AddFakeMigration(1, typeof(TestEntity));
            var migration2 = this.AddFakeMigration(1, typeof(TestUser));
            this.versionLocator.GetTargetVersion(typeof(TestEntity)).Returns(1);
            var sut = new DocumentMigrationRunner(db, this.migrations, nameResolver, this.versionLocator, NullLogger<DocumentMigrationRunner>.Instance);

            await sut.MigrateAllTypes(MigrationTiming.AtAppStart);
            var actual = await this.dbCollection.Find(x => x.Id == e.Id).FirstOrDefaultAsync();
            var actualVersion = await this.GetVersionFromDb(e.Id);

            actualVersion.Should().Be(1);
            migration1.Received().Up(Arg.Any<BsonDocument>());
            migration2.DidNotReceive().Up(Arg.Any<BsonDocument>());
        }

        [Fact]
        public async Task ShouldOnlyRunMigrationsMarkedAtStartup()
        {
            var e = new TestEntity { Id = Guid.Empty };
            await this.dbCollection.InsertOneAsync(e);

            var migration1 = this.AddFakeMigration(1);
            var migration2 = this.AddFakeMigration(2);
            var migration3 = this.AddFakeMigration(3);
            migration3.WhenToMigrate.Returns(MigrationTiming.OnDocumentAccess);
            this.versionLocator.GetTargetVersion(typeof(TestEntity)).Returns(3);
            var sut = new DocumentMigrationRunner(db, this.migrations, nameResolver, this.versionLocator, NullLogger<DocumentMigrationRunner>.Instance);

            await sut.MigrateAllTypes(MigrationTiming.AtAppStart);
            var actual = await this.dbCollection.Find(x => x.Id == e.Id).FirstOrDefaultAsync();
            var actualVersion = await this.GetVersionFromDb(e.Id);

            actualVersion.Should().Be(2);
            migration1.Received().Up(Arg.Any<BsonDocument>());
            migration2.Received().Up(Arg.Any<BsonDocument>());
            migration3.DidNotReceive().Up(Arg.Any<BsonDocument>());
        }

        [Fact]
        public void ShouldRunAllMigrationsOnDocumentAccess()
        {
            this.versionLocator.GetTargetVersion(typeof(TestEntity)).Returns(1);
            var e = new TestEntity { Id = Guid.Empty };
            var doc = e.ToBsonDocument();
        
            var migration1 = this.AddFakeMigration(1);
            var migration2 = this.AddFakeMigration(2);
            migration2.WhenToMigrate.Returns(MigrationTiming.OnDocumentAccess);
            var migration3 = this.AddFakeMigration(3);
            this.versionLocator.GetTargetVersion(typeof(TestEntity)).Returns(3);
            var sut = new DocumentMigrationRunner(db, this.migrations, nameResolver, this.versionLocator, NullLogger<DocumentMigrationRunner>.Instance);
        
            sut.MigrateDocument(doc, typeof(TestEntity), MigrationTiming.OnDocumentAccess);

            var actualVersion = doc["_schemaVersion"].AsInt32;
            actualVersion.Should().Be(3);

            migration1.DidNotReceive().Up(Arg.Any<BsonDocument>());
            migration2.Received().Up(Arg.Any<BsonDocument>());
            migration3.Received().Up(Arg.Any<BsonDocument>());
        }

        [Fact]
        public void ShouldOnlyRunSomeMigrationsAtStartup()
        {
            this.versionLocator.GetTargetVersion(typeof(TestEntity)).Returns(1);
            var e = new TestEntity { Id = Guid.Empty };
            var doc = e.ToBsonDocument();
        
            var migration1 = this.AddFakeMigration(1);
            var migration2 = this.AddFakeMigration(2);
            migration2.WhenToMigrate.Returns(MigrationTiming.OnDocumentAccess);
            var migration3 = this.AddFakeMigration(3);
            var migration4 = this.AddFakeMigration(4);
            migration4.WhenToMigrate.Returns(MigrationTiming.OnDocumentAccess);
            this.versionLocator.GetTargetVersion(typeof(TestEntity)).Returns(4);
            var sut = new DocumentMigrationRunner(db, this.migrations, nameResolver, this.versionLocator, NullLogger<DocumentMigrationRunner>.Instance);
        
            sut.MigrateDocument(doc, typeof(TestEntity), MigrationTiming.AtAppStart);

            var actualVersion = doc["_schemaVersion"].AsInt32;
            actualVersion.Should().Be(3);

            migration1.DidNotReceive().Up(Arg.Any<BsonDocument>());
            migration2.Received().Up(Arg.Any<BsonDocument>());
            migration3.Received().Up(Arg.Any<BsonDocument>());
            migration4.DidNotReceive().Up(Arg.Any<BsonDocument>());
        }

        [Fact]
        public void ShouldSkipMigrationIfAlreadyAtTargetVersion()
        {
            this.versionLocator.GetTargetVersion(typeof(TestEntity)).Returns(2);
            var e = new TestEntity { Id = Guid.Empty };
            var doc = e.ToBsonDocument();
            
            var migration1 = this.AddFakeMigration(1);
            var migration2 = this.AddFakeMigration(2);
            var sut = new DocumentMigrationRunner(db, this.migrations, nameResolver, this.versionLocator, NullLogger<DocumentMigrationRunner>.Instance);
            
            sut.MigrateDocument(doc, typeof(TestEntity), MigrationTiming.OnDocumentAccess);
            
            migration1.DidNotReceive().Up(Arg.Any<BsonDocument>());
            migration2.DidNotReceive().Up(Arg.Any<BsonDocument>());
        }

        private async Task<int> GetVersionFromDb(Guid id)
        {
            var actualBson = await this.bsonCollection.Find(x => x["_id"] == id).FirstOrDefaultAsync();

            return actualBson[this.versionLocator.VersionFieldName()].AsInt32;
        }

        private IDocumentMigration AddFakeMigration(int version, Type? type = null)
        {
            type ??= typeof(TestEntity);

            var migration = Substitute.For<IDocumentMigration>();
            migration.Version.Returns(version);
            migration.WhenToMigrate.Returns(MigrationTiming.AtAppStart);
            migration.DocumentType.Returns(type);
            this.migrations.Add(migration);

            return migration;
        }

        internal class TestMigration : IDocumentMigration
        {
            public TestMigration(int version, Action<BsonDocument> up, Action<BsonDocument> down)
            {
                this.Version = version;
                this.up = up;
                this.down = down;
                this.WhenToMigrate = MigrationTiming.AtAppStart;
                this.DocumentType = typeof(TestEntity);
            }

            public int Version { get; init; }
            public MigrationTiming WhenToMigrate { get; }
            public Type DocumentType { get; init; }
            private readonly Action<BsonDocument> down;
            private readonly Action<BsonDocument> up;

            public void Up(BsonDocument document) => this.up(document);

            public void Down(BsonDocument document) => this.down(document);
        }
    }
}