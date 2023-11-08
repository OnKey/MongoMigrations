using System;
using System.Collections.Generic;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using MongoMigrations.MigrationRunner;
using MongoMigrations.Migrations;
using NSubstitute;
using Xunit;

namespace MongoMigrations.Test.MigrationRunner
{
    public class VersionLocatorTest
    {
        private VersionLocator sut;
        private List<IDocumentMigration> migrations = new();

        public VersionLocatorTest()
        {
            this.sut = new VersionLocator(this.migrations, NullLogger<VersionLocator>.Instance);
        }

        [Fact]
        public void ShouldGetVersion0ByDefault()
        {
            var actual = this.sut.GetTargetVersion(typeof(TestEntity2));

            actual.Should().Be(0);
        }

        [Fact]
        public void TargetVersionShouldBeMaxMigrationVersion()
        {
            this.AddFakeMigration(1, typeof(TestEntity));
            this.AddFakeMigration(2, typeof(TestEntity));
            this.AddFakeMigration(3, typeof(TestEntity));
            this.sut = new VersionLocator(this.migrations, NullLogger<VersionLocator>.Instance);

            var actual = this.sut.GetTargetVersion(typeof(TestEntity));

            actual.Should().Be(3);
        }

        [Fact]
        public void ShouldGetOverridenVersion()
        {
            this.AddFakeMigration(1, typeof(TestEntity));
            this.AddFakeMigration(2, typeof(TestEntity));
            this.AddFakeMigration(3, typeof(TestEntity));
            this.sut = new VersionLocator(this.migrations, NullLogger<VersionLocator>.Instance);
            this.sut.SetTargetVersion<TestEntity>(2);

            var actual = this.sut.GetTargetVersion(typeof(TestEntity));

            actual.Should().Be(2);
        }

        [Fact]
        public void ShouldGetVersionByType()
        {
            this.AddFakeMigration(1, typeof(TestEntity));
            this.AddFakeMigration(2, typeof(TestEntity2));
            this.sut = new VersionLocator(this.migrations, NullLogger<VersionLocator>.Instance);

            var actual1 = this.sut.GetTargetVersion(typeof(TestEntity));
            var actual2 = this.sut.GetTargetVersion(typeof(TestEntity2));

            actual1.Should().Be(1);
            actual2.Should().Be(2);
        }

        [Fact]
        public void ShouldThrowExceptionIfDuplicateVersions()
        {
            this.AddFakeMigration(1, typeof(TestEntity));
            this.AddFakeMigration(1, typeof(TestEntity));

            Action actual = () => this.sut.ReplaceTargetVersions(this.migrations);

            actual.Should().Throw<DuplicateMigrationVersion>();
        }

        [Fact]
        public void ShouldReturnFieldNameForVersion()
        {
            var actual = this.sut.VersionFieldName();

            actual.Should().Be("_schemaVersion");
        }

        [Fact]
        public void ShouldBeVersionedIfTargetVersionGreaterThanZero()
        {
            this.AddFakeMigration(1, typeof(TestEntity));
            this.sut = new VersionLocator(this.migrations, NullLogger<VersionLocator>.Instance);
            
            var actual = this.sut.IsVersioned(typeof(TestEntity));

            actual.Should().BeTrue();
        }

        [Fact]
        public void ShouldNotBeVersionedIfNoMigrations()
        {
            var actual = this.sut.IsVersioned(typeof(TestEntity));

            actual.Should().BeFalse();
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

        private class TestEntity2
        {
        }
    }
}