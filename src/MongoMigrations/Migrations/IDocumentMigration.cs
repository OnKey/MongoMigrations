using System;
using MongoDB.Bson;

namespace MongoMigrations.Migrations
{
    /// <summary>
    /// Used to register any migrations for individual documents which are needed as a result of schema change.
    /// Can be applied at startup with an IDocumentMigration or when document is accessed.
    /// </summary>
    public interface IDocumentMigration
    {
        /// <summary>
        /// Schema version
        /// </summary>
        int Version { get; }

        /// <summary>
        /// Run migration when application starts
        /// </summary>
        MigrationTiming WhenToMigrate { get; }

        /// <summary>
        /// Type of document to migrate
        /// </summary>
        Type DocumentType { get; }

        /// <summary>
        /// Migrate document from previous version to this version
        /// </summary>
        void Up(BsonDocument document);

        /// <summary>
        /// Migration from this version to previous version
        /// </summary>
        void Down(BsonDocument document);
    }
}