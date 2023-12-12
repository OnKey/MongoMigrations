using System;

namespace MongoMigrations.Collections
{
    /// <summary>
    /// Lookup a class and get the correct DB collection name for it
    /// </summary>
    public interface ICollectionNameResolver
    {
        /// <summary>
        /// Get the collection name for a type. Throws MissingDbCollectionConfigurationException if type is not mapped
        /// to a collection.
        /// </summary>
        /// <typeparam name="T">Type to look up</typeparam>
        /// <returns>name of Mongo colleciton</returns>
        string GetCollectionName<T>();

        /// <summary>
        /// Get the collection name for a type. Throws MissingDbCollectionConfigurationException if type is not mapped
        /// to a collection.
        /// </summary>
        /// <param name="entityType">Type to look up</param>
        /// <returns>name of Mongo colleciton</returns>
        string GetCollectionName(Type entityType);

        /// <summary>
        /// Add a mapping between a Type and a DB collection
        /// </summary>
        ICollectionNameResolver AddType(Type type, string collectionName);
    }
}