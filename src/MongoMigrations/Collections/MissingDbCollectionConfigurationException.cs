using System;

namespace MongoMigrations.Collections
{
    public class MissingDbCollectionConfigurationException : Exception
    {
        public MissingDbCollectionConfigurationException(string type) :
            base($"{type} does not have a mapping to a collection name configured")
        {
        }
    }
}