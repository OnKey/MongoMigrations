using System;
using System.Collections.Generic;

namespace MongoMigrations.Collections
{
    internal class CollectionNameResolver : ICollectionNameResolver
    {
        private Dictionary<Type, string> collectionNames = new();

        public string GetCollectionName<T>() => this.GetCollectionName(typeof(T));

        public string GetCollectionName(Type entityType)
        {
            if (this.collectionNames.TryGetValue(entityType, out var name))
            {
                return name;
            }

            throw new MissingDbCollectionConfiguration(entityType.ToString());
        }

        public ICollectionNameResolver AddType(Type type, string collectionName)
        {
            this.collectionNames.Add(type, collectionName);
            return this;
        }
    }
}