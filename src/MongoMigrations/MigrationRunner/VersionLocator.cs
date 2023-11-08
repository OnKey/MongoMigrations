using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using MongoMigrations.Migrations;

namespace MongoMigrations.MigrationRunner
{
    internal class VersionLocator : IVersionLocator
    {
        public const string DocumentSchemaVersionFieldName = "_schemaVersion";
        private Dictionary<Type, int> targetVersions = new();
        private ILogger<VersionLocator> logger;

        public VersionLocator(IEnumerable<IDocumentMigration> migrations, ILogger<VersionLocator> logger)
        {
            this.logger = logger;
            this.ReplaceTargetVersions(migrations.ToList());
        }

        public int GetTargetVersion(Type targetType) =>
            this.targetVersions.TryGetValue(targetType, out var value) ? value : 0;

        /// <summary>
        /// Set the target schema version for a specific class. By default objects will be migrated to the maximum
        /// schema version present in the IDocumentMigrations.
        /// </summary>
        /// <param name="targetSchemaVersion">Target version to migrate to</param>
        /// <typeparam name="T">Object type to migrate</typeparam>
        public VersionLocator SetTargetVersion<T>(int targetSchemaVersion)
        {
            this.targetVersions[typeof(T)] = targetSchemaVersion;
            return this;
        }

        public string VersionFieldName() => DocumentSchemaVersionFieldName;

        public bool IsVersioned(Type t) => this.GetTargetVersion(t) > 0;

        internal void ReplaceTargetVersions(ICollection<IDocumentMigration> migrations)
        {
            var types = migrations.Select(x => x.DocumentType).Distinct();
            foreach (var t in types)
            {
                var typeMigrations = migrations.Where(x => x.DocumentType == t).ToList();
                var maxVersion = typeMigrations.Max(x => x.Version);
                this.CheckDuplicates(t, typeMigrations);
                this.targetVersions[t] = maxVersion;
            }
        }

        private void CheckDuplicates(Type t, ICollection<IDocumentMigration> migrations)
        {
            var duplicates = migrations.GroupBy(x => x.Version).Where(g => g.Count() > 1).ToList();
            if (duplicates.Any())
            {
                this.logger.LogWarning("Multiple document migrations defined for type {type} with version number {version}", t,
                    duplicates.First().Key);

                throw new DuplicateMigrationVersion(t, duplicates.First().Key);
            }
        }
    }
}