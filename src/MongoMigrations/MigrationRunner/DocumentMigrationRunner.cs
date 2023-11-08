using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoMigrations.Collections;
using MongoMigrations.Migrations;

namespace MongoMigrations.MigrationRunner
{
    /// <summary>
    /// Migrates individual documents to the required schema version.
    /// Currently just runs at startup but should be able to run as objects are accessed as well.
    /// </summary>
    internal class DocumentMigrationRunner : IDocumentMigrationRunner
    {
        private IMongoDatabase db;
        private ICollection<IDocumentMigration> migrations;
        private ICollectionNameResolver nameResolver;
        private IVersionLocator versionLocator;
        private ILogger<DocumentMigrationRunner> logger;

        public DocumentMigrationRunner(IMongoDatabase db, IEnumerable<IDocumentMigration> migrations,
            ICollectionNameResolver nameResolver, IVersionLocator versionLocator, ILogger<DocumentMigrationRunner> logger)
        {
            this.db = db;
            this.nameResolver = nameResolver;
            this.versionLocator = versionLocator;
            this.logger = logger;
            this.migrations = migrations.ToList();
        }

        public async Task MigrateAllTypes(MigrationTiming migrationTiming)
        {
            foreach (var targetType in this.GetTypesToBeMigrated(migrationTiming))
            {
                try
                {
                    await this.MigrateType(targetType, migrationTiming);
                }
                catch (Exception ex)
                {
                    this.logger.LogWarning(ex, "Failed to run DB Document migration for type {targetType}.", targetType);
                }
            }
        }

        private IEnumerable<Type> GetTypesToBeMigrated(MigrationTiming migrationTiming)
        {
            if (migrationTiming == MigrationTiming.AtAppStart)
            {
                return this.migrations
                    .Where(x => x.WhenToMigrate == MigrationTiming.AtAppStart)
                    .Select(x => x.DocumentType)
                    .Distinct();
            }
        
            return this.migrations
                .Select(x => x.DocumentType)
                .Distinct();
        }
    
        public async Task MigrateType(Type targetType, MigrationTiming migrationTiming)
        {
            var targetSchemaVersion = this.versionLocator.GetTargetVersion(targetType);
            var targetTypeMigrations = this.GetMigrationsForType(targetType, migrationTiming);
            var collection = this.db.GetCollection<BsonDocument>(this.nameResolver.GetCollectionName(targetType));
            var filter = Builders<BsonDocument>.Filter.Ne(this.versionLocator.VersionFieldName(),
                targetSchemaVersion);

            var migratedDocs = 0;
            using var cursor = await collection.FindAsync(filter);
            while (await cursor.MoveNextAsync())
            {
                var batch = (IReadOnlyList<BsonDocument>)cursor.Current;
                foreach (var document in batch)
                {
                    if (migratedDocs == 0)
                    {
                        this.logger.LogInformation(
                            "Starting Mongo document migration for type {DocType} from version {versionFrom} to {versionTo}",
                            targetType.Name, this.GetDocVersion(document), targetSchemaVersion);
                    }

                    this.MigrateDocument(targetTypeMigrations, targetSchemaVersion, document);
                    await collection.ReplaceOneAsync(new BsonDocument("_id", document["_id"]), document);
                    migratedDocs++;
                }
            }

            if (migratedDocs > 0)
            {
                this.logger.LogInformation("Migrated {count} documents", migratedDocs);
            }
        }

        private List<IDocumentMigration> GetMigrationsForType(Type targetType, MigrationTiming migrationTiming)
        {
            if (migrationTiming == MigrationTiming.OnDocumentAccess)
            {
                return this.migrations
                    .Where(x => x.DocumentType == targetType)
                    .ToList();
            }

            var maxStartUpMigration = this.migrations
                .Where(x => x.DocumentType == targetType && x.WhenToMigrate == MigrationTiming.AtAppStart)
                .Max(x => x.Version);
        
            return this.migrations
                .Where(x => x.DocumentType == targetType && x.Version <= maxStartUpMigration)
                .ToList();
        }

        public void MigrateDocument(BsonDocument document, Type targetType, MigrationTiming migrationTiming)
        {
            var targetSchemaVersion = this.versionLocator.GetTargetVersion(targetType);
            if (this.GetDocVersion(document) == targetSchemaVersion)
            {
                return;
            }
        
            var targetTypeMigrations = this.GetMigrationsForType(targetType, migrationTiming);
            this.MigrateDocument(targetTypeMigrations, targetSchemaVersion, document);
        }

        public void MigrateDocument(ICollection<IDocumentMigration> typeMigrations, int targetSchemaVersion,
            BsonDocument document)
        {
            var startingVersion = this.GetDocVersion(document);
            var requiredMigrations = this.GetMigrations(startingVersion, targetSchemaVersion, typeMigrations);
            foreach (var migration in requiredMigrations)
            {
                if (targetSchemaVersion > startingVersion)
                {
                    migration.Up(document);
                    document[this.versionLocator.VersionFieldName()] = migration.Version;
                }
                else
                {
                    migration.Down(document);
                    document[this.versionLocator.VersionFieldName()] = migration.Version - 1;
                }
            }
        
            this.logger.LogDebug("Migrated document from {previousVersion} to {currentVersion}", startingVersion, document[this.versionLocator.VersionFieldName()].AsInt32);
        }

        private int GetDocVersion(BsonDocument document) =>
            document.GetValue(this.versionLocator.VersionFieldName(), 0).AsInt32;

        private IEnumerable<IDocumentMigration> GetMigrations(int currentVersion, int targetSchemaVersion,
            ICollection<IDocumentMigration> typeMigrations)
        {
            if (currentVersion == targetSchemaVersion)
            {
                return new List<IDocumentMigration>();
            }

            // Migrate Up
            if (currentVersion < targetSchemaVersion)
            {
                return typeMigrations
                    .Where(x => x.Version > currentVersion && x.Version <= targetSchemaVersion)
                    .OrderBy(x => x.Version);
            }

            // Migrate Down
            return typeMigrations
                .Where(x => x.Version > targetSchemaVersion && x.Version <= currentVersion)
                .OrderByDescending(x => x.Version);
        }
    }
}