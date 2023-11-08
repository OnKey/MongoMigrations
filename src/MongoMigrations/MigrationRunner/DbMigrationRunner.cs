using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using MongoMigrations.Migrations;

namespace MongoMigrations.MigrationRunner
{
    /// <summary>
    /// Executes DB level migrations on application startup. Individual document migrations can be run as documents are accessed.
    /// </summary>
    internal class DbMigrationRunner
    {
        internal const string SchemaVersionCollectionName = "SchemaVersion";
        private IMongoDatabase db;
        private IEnumerable<IDatabaseMigration> migrations;
        private ILogger<DbMigrationRunner> logger;

        public DbMigrationRunner(IEnumerable<IDatabaseMigration> migrations, IMongoDatabase db, ILogger<DbMigrationRunner> logger)
        {
            this.migrations = migrations;
            this.db = db;
            this.logger = logger;
        }

        /// <summary>
        /// Migration DB schema from current version to latest version
        /// </summary>
        /// <param name="targetSchemaVersion">Migrate to a specific version rather than latest</param>
        internal async Task RunDbMigrations(int targetSchemaVersion = int.MaxValue)
        {
            var currentSchemaVersion = this.GetSchemaVersion();
            var newMigrations = this.GetMigrations(currentSchemaVersion.Version, targetSchemaVersion);

            if (newMigrations.Count == 0)
            {
                this.logger.LogInformation("Database schema already up to date. No migrations.");
                return;
            }

            foreach (var migration in newMigrations)
            {
                if (targetSchemaVersion > currentSchemaVersion.Version)
                {
                    this.logger.LogInformation("Upgrading Mongo schema from {versionFrom} to {versionTo}",
                        currentSchemaVersion.Version, migration.Version);

                    await migration.Up();
                    this.SaveSchemaVersion(new SchemaVersion(migration.Version));
                }
                else
                {
                    this.logger.LogInformation("Downgrading Mongo schema from {versionFrom} to {versionTo}",
                        currentSchemaVersion.Version, migration.Version);

                    await migration.Down();
                    this.SaveSchemaVersion(new SchemaVersion(migration.Version - 1));
                }
            }

            this.logger.LogInformation("Completed Mongo DB migrations");
        }

        private List<IDatabaseMigration> GetMigrations(int currentSchemaVersion, int targetSchemaVersion)
        {
            if (targetSchemaVersion >= currentSchemaVersion)
            {
                return this.migrations
                    .Where(x => x.Version > currentSchemaVersion && x.Version <= targetSchemaVersion)
                    .OrderBy(x => x.Version)
                    .ToList();
            }

            return this.migrations
                .Where(x => x.Version > targetSchemaVersion && x.Version <= currentSchemaVersion)
                .OrderByDescending(x => x.Version)
                .ToList();
        }

        internal SchemaVersion GetSchemaVersion() =>
            this.db.GetCollection<SchemaVersion>(SchemaVersionCollectionName).AsQueryable()
                .FirstOrDefault() ?? new SchemaVersion(0);

        private void SaveSchemaVersion(SchemaVersion version) =>
            this.db.GetCollection<SchemaVersion>(SchemaVersionCollectionName)
                .ReplaceOne(x => x.Id == version.Id, version, new ReplaceOptions { IsUpsert = true });
    }
}