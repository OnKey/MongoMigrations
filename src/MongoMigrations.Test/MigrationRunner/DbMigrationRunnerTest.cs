using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Driver;
using MongoMigrations.MigrationRunner;
using MongoMigrations.Migrations;
using NSubstitute;
using Xunit;

namespace MongoMigrations.Test.MigrationRunner
{
    [Collection("Mongo collection")]
    public class DbMigrationRunnerTest
    {
        private readonly IMongoDatabase db;
        private readonly DbMigrationRunner sut;
        private List<IDatabaseMigration> migrations = new();

        public DbMigrationRunnerTest(MongoTestContainer mongo)
        {
            this.db = mongo.Db;
            this.db.DropCollection(DbMigrationRunner.SchemaVersionCollectionName, CancellationToken.None);
            this.sut = new DbMigrationRunner(this.migrations, this.db, NullLogger<DbMigrationRunner>.Instance);
        }

        [Fact]
        public void ShouldHaveVersion0ByDefault()
        {
            var actual = this.sut.GetSchemaVersion();

            actual.Version.Should().Be(0);
        }

        [Fact]
        public void ShouldGetCurrentSchemaVersion()
        {
            this.db.GetCollection<SchemaVersion>(DbMigrationRunner.SchemaVersionCollectionName)
                .InsertOne(new SchemaVersion(10));

            var actual = this.sut.GetSchemaVersion();

            actual.Version.Should().Be(10);
        }

        [Fact]
        public async Task ShouldRunMigrationsAndUpdateVersion()
        {
            var migration = Substitute.For<IDatabaseMigration>();
            migration.Version.Returns(1);
            this.migrations.Add(migration);

            await this.sut.RunDbMigrations();

            await migration.Received().Up();

            var actual = this.sut.GetSchemaVersion();

            actual.Version.Should().Be(1);
        }

        [Fact]
        public async Task ShouldRunMigrationsInOrder()
        {
            var migration1 = Substitute.For<IDatabaseMigration>();
            migration1.Version.Returns(1);
            this.migrations.Add(migration1);
            var migration2 = Substitute.For<IDatabaseMigration>();
            migration2.Version.Returns(2);
            this.migrations.Add(migration2);

            await this.sut.RunDbMigrations();

            await migration1.Received().Up();
            await migration2.Received().Up();

            var actual = this.sut.GetSchemaVersion();

            actual.Version.Should().Be(2);
        }

        [Fact]
        public async Task ShouldNotRunMigrationsForOlderSchemaVersions()
        {
            this.db.GetCollection<SchemaVersion>(DbMigrationRunner.SchemaVersionCollectionName)
                .InsertOne(new SchemaVersion(1));

            var migration1 = Substitute.For<IDatabaseMigration>();
            migration1.Version.Returns(1);
            this.migrations.Add(migration1);
            var migration2 = Substitute.For<IDatabaseMigration>();
            migration2.Version.Returns(2);
            this.migrations.Add(migration2);

            await this.sut.RunDbMigrations();

            await migration1.DidNotReceive().Up();
            await migration2.Received().Up();
        }

        [Fact]
        public async Task ShouldRunIfNoMigrationsNeeded()
        {
            await this.sut.RunDbMigrations();
        }

        [Fact]
        public async Task ShouldMigrateUpToTargetVersion()
        {
            var migration1 = Substitute.For<IDatabaseMigration>();
            migration1.Version.Returns(1);
            this.migrations.Add(migration1);
            var migration2 = Substitute.For<IDatabaseMigration>();
            migration2.Version.Returns(2);
            this.migrations.Add(migration2);
            var migration3 = Substitute.For<IDatabaseMigration>();
            migration3.Version.Returns(3);
            this.migrations.Add(migration3);

            await this.sut.RunDbMigrations(2);

            await migration1.Received().Up();
            await migration2.Received().Up();
            await migration3.DidNotReceive().Up();

            var actual = this.sut.GetSchemaVersion();

            actual.Version.Should().Be(2);
        }

        [Fact]
        public async Task ShouldMigrateDownToTargetVersion()
        {
            this.db.GetCollection<SchemaVersion>(DbMigrationRunner.SchemaVersionCollectionName)
                .InsertOne(new SchemaVersion(3));

            var migration1 = Substitute.For<IDatabaseMigration>();
            migration1.Version.Returns(1);
            this.migrations.Add(migration1);
            var migration2 = Substitute.For<IDatabaseMigration>();
            migration2.Version.Returns(2);
            this.migrations.Add(migration2);
            var migration3 = Substitute.For<IDatabaseMigration>();
            migration3.Version.Returns(3);
            this.migrations.Add(migration3);

            await this.sut.RunDbMigrations(1);

            await migration1.DidNotReceive().Down();
            await migration2.Received().Down();
            await migration3.Received().Down();

            await migration1.DidNotReceive().Up();
            await migration2.DidNotReceive().Up();
            await migration3.DidNotReceive().Up();

            var actual = this.sut.GetSchemaVersion();

            actual.Version.Should().Be(1);
        }
    }
}