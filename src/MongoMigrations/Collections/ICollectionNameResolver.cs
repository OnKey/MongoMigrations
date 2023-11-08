using System;

namespace MongoMigrations.Collections
{
    /// <summary>
    /// Lookup a class and get the correct DB collection name for it
    /// </summary>
    public interface ICollectionNameResolver
    {
        string? GetCollectionName<T>();

        string? GetCollectionName(Type entityType);

        /// <summary>
        /// Add a mapping between a Type and a DB collection
        /// </summary>
        ICollectionNameResolver AddType(Type type, string collectionName);
    }
}