using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MongoDB.Bson.Serialization;
using MongoMigrations.Collections;
using MongoMigrations.MigrationRunner;
using MongoMigrations.Migrations;

[assembly: InternalsVisibleTo("MongoMigrations.Test")]
namespace MongoMigrations
{
    public static class MongoRegistrationExtensions
    {
        public static ICollectionNameResolver AddMongoMigrations(this IServiceCollection services)
        {
            services.AddSingleton<DbMigrationRunner>();
            services.AddSingleton<IDocumentMigrationRunner, DocumentMigrationRunner>();
            services.AddSingleton<IVersionLocator, VersionLocator>();
            services.AddSingleton<MigrationInterceptionProvider>();
            services.AddSingleton(typeof(IMigrationInterceptor<>), typeof(MigrationInterceptor<>));

            var cnr = new CollectionNameResolver();
            services.AddSingleton<ICollectionNameResolver>(cnr);
            return cnr;
        }
        
        /// <summary>
        /// Run any DB and document level migrations at startup
        /// </summary>
        public static async Task RunDbMigrations(this IHost host)
        {
            RegisterMongoSerializationInterceptor(host);

            var migrationRunner = host.Services.GetRequiredService<DbMigrationRunner>();
            var docRunner = host.Services.GetRequiredService<IDocumentMigrationRunner>();

            await migrationRunner.RunDbMigrations();
            await docRunner.MigrateAllTypes(MigrationTiming.AtAppStart);
        }

        public static void RegisterMongoSerializationInterceptor(IHost host)
        {
            var migrationInterceptionProvider = host.Services.GetRequiredService<MigrationInterceptionProvider>();
            BsonSerializer.RegisterSerializationProvider(migrationInterceptionProvider);
        }

        /// <summary>
        /// Scan one or more assemblies and add all instances of IDatabaseMigration and IDocumentMigration to DI
        /// </summary>
        public static void ScanForMongoMigrations(this IServiceCollection services, params Assembly[] assembliesToScan)
        {
            if (assembliesToScan.Length == 0)
            {
                throw new ArgumentNullException(nameof(assembliesToScan), "Must pass at least 1 assembly to scanner");
            }

            foreach (var assembly in assembliesToScan)
            {
                AddDocumentMigrations(services, assembly);
                AddDbMigrations(services, assembly);
            }
        }

        private static void AddDocumentMigrations(IServiceCollection services, Assembly assembly)
        {
            var docMigrations = assembly.GetTypes().Where(x =>
                x is { IsClass: true, IsAbstract: false } && x.IsAssignableTo(typeof(IDocumentMigration)));
            foreach (var docMigration in docMigrations)
            {
                services.AddTransient(typeof(IDocumentMigration), docMigration);
            }
        }
        
        private static void AddDbMigrations(IServiceCollection services, Assembly assembly)
        {
            var dbMigrations = assembly.GetTypes().Where(x =>
                x is { IsClass: true, IsAbstract: false } && x.IsAssignableTo(typeof(IDatabaseMigration)));
            foreach (var dbMigration in dbMigrations)
            {
                services.AddTransient(typeof(IDatabaseMigration), dbMigration);
            }
        }
    }
}