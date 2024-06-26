using System;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoMigrations.MigrationRunner;
using MongoMigrations.Migrations;
using NSubstitute;
using Xunit;

namespace MongoMigrations.Test.MigrationRunner
{
    /// <summary>
    /// These tests are very fragile due to the BSON serialiser cache being global and so shared between tests. We have
    /// to try and reset this for each unit test.
    /// </summary>
    public class MigrationInterceptorTest
    {
        private readonly IVersionLocator versionLocator;
        private MigrationInterceptor<TestDoc> sut;
        private readonly IDocumentMigrationRunner migrationRunner;

        public MigrationInterceptorTest()
        {
            this.versionLocator = Substitute.For<IVersionLocator>();
            this.versionLocator.VersionFieldName().Returns("SchemaVersion");
            this.versionLocator.IsVersioned(typeof(TestDoc)).Returns(true);
            this.versionLocator.IsVersioned(typeof(TestParentDoc)).Returns(true);

            this.migrationRunner = Substitute.For<IDocumentMigrationRunner>();
        
            this.sut = new MigrationInterceptor<TestDoc>(this.versionLocator, this.migrationRunner);
            var interceptor2 = new MigrationInterceptor<TestParentDoc>(this.versionLocator, this.migrationRunner);
            
            SerialiserHelper.RegisterMigrationInterceptor(this.sut);
            SerialiserHelper.RegisterMigrationInterceptor(interceptor2);
        }

        [Fact]
        public void ShouldAddCurrentVersionWhenSerialising()
        {
            this.versionLocator.GetTargetVersion(typeof(TestDoc)).Returns(5);
            var before = new TestDoc { Id = Guid.NewGuid() };

            var doc = before.ToBsonDocument();
            doc["SchemaVersion"].AsInt32.Should().Be(5);
        }

        [Fact]
        public void ShouldRemoveVersionFieldBeforeDeserialise()
        {
            this.versionLocator.GetTargetVersion(typeof(TestDoc)).Returns(1);
            var doc = new BsonDocument
            {
                [this.versionLocator.VersionFieldName()] = 123
            };

            Action actual = () => BsonSerializer.Deserialize<TestDoc>(doc);

            actual.Should().NotThrow();
        }

        [Fact]
        public void ShouldMigrateDocOnAccess()
        {
            this.versionLocator.GetTargetVersion(typeof(TestDoc)).Returns(2);
            var doc = new BsonDocument
            {
                [this.versionLocator.VersionFieldName()] = 1
            };
        
            BsonSerializer.Deserialize<TestDoc>(doc);
        
            this.migrationRunner.Received().MigrateDocument(Arg.Any<BsonDocument>(), typeof(TestDoc), MigrationTiming.OnDocumentAccess);
        }

        [Fact]
        public void ShouldNotAllowEmptyVersionFieldName()
        {
            this.versionLocator.VersionFieldName().Returns(string.Empty);

            var before = new TestDoc { Id = Guid.NewGuid() };
            
            Action actual = () => before.ToBsonDocument();

            actual.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void ShouldMigrateNestedDocuments()
        {
            this.versionLocator.GetTargetVersion(typeof(TestParentDoc)).Returns(1);
            var before = new TestParentDoc { Id = Guid.NewGuid(), Child = new TestNestedDoc { Name = "bob" }};

            var doc = before.ToBsonDocument();
            
            doc["SchemaVersion"].AsInt32.Should().Be(1);
        }

        [Fact]
        public void ShouldNotAddSchemaVersionToNestedDocuments()
        {
            this.versionLocator.GetTargetVersion(typeof(TestParentDoc)).Returns(1);
            var before = new TestParentDoc { Id = Guid.NewGuid(), Child = new TestNestedDoc { Name = "bob" }};

            var doc = before.ToBsonDocument();

            doc["Child"].AsBsonDocument.Contains("SchemaVersion").Should().BeFalse();
        }

        public class TestDoc 
        {
            public Guid Id { get; set; }
        }
        
        public class TestParentDoc 
        {
            public Guid Id { get; set; }
            public required TestNestedDoc Child { get; set; }
        }

        public class TestNestedDoc
        {
            public required string Name { get; set; }
        }
    }
}