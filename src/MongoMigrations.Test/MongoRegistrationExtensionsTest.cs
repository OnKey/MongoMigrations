using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using MongoMigration.Sample.Migrations;
using MongoMigrations.Collections;
using MongoMigrations.Migrations;
using NSubstitute;
using Xunit;

namespace MongoMigrations.Test
{
    public class MongoRegistrationExtensionsTest
    {
        [Fact]
        public void ShouldRegisterIDocumentMigrationOnScan()
        {
            var services = new ServiceCollection();
            services.ScanForMongoMigrations(typeof(MongoMigration.Sample.Program).Assembly);
            var serviceProvider = services.BuildServiceProvider();

            var migrations = serviceProvider.GetService<IEnumerable<IDocumentMigration>>()?.ToList() ?? new List<IDocumentMigration>();

            migrations.Should().ContainSingle();
            var docMigration = migrations.First();
            docMigration.GetType().Should().Be(typeof(User_UpdateFullName));
        }
        
        [Fact]
        public void ShouldRegisterIDbMigrationOnScan()
        {
            var services = new ServiceCollection();
            services.ScanForMongoMigrations(typeof(MongoMigration.Sample.Program).Assembly);
            services.AddSingleton(Substitute.For<IMongoDatabase>());
            services.AddSingleton(Substitute.For<ICollectionNameResolver>());
            var serviceProvider = services.BuildServiceProvider();
            
            var migrations = serviceProvider.GetService<IEnumerable<IDatabaseMigration>>()?.ToList() ?? new List<IDatabaseMigration>();
            
            migrations.Should().ContainSingle();
            var dbMigration = migrations.First();
            dbMigration.GetType().Should().Be(typeof(Db_AddIndex));
        }

        [Fact]
        public void ShouldThrowIfNoAssembliesPassed()
        {
            var services = new ServiceCollection();
            Action scan = () => services.ScanForMongoMigrations();

            scan.Should().Throw<ArgumentNullException>();
        }
    }
}