using System;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoMigrations.Migrations;

namespace MongoMigrations.MigrationRunner
{
    /// <summary>
    /// Migrates individual documents based on the migrations defined in IDocumentMigration and the versions in IVersionLocator
    /// </summary>
    public interface IDocumentMigrationRunner
    {
        Task MigrateAllTypes(MigrationTiming migrationTiming);
        Task MigrateType(Type targetType, MigrationTiming migrationTiming);
    
        /// <summary>
        /// Applies migrations to document to bring is the current target version for the targetType. Does not save to the
        /// DB, it will just update the field in the document passed in.
        /// </summary>
        /// <param name="document">Document to migrate</param>
        /// <param name="targetType">The class Type which should be used when looking for migrations</param>
        /// <param name="migrationTiming">If this is AtAppStart will only run migrations with this value in the migration type;
        /// otherwise it will run all migrations i.e. OnDocumentAccess also runs AtAppStart migrations.</param>
        void MigrateDocument(BsonDocument document, Type targetType, MigrationTiming migrationTiming);
    }
}