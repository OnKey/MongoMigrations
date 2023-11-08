using System;

namespace MongoMigrations.Collections
{
    public class MissingDbCollectionConfiguration : Exception
    {
        public MissingDbCollectionConfiguration(string type) :
            base($"{type} does not have a mapping to a collection name configured")
        {
        }
    }
}