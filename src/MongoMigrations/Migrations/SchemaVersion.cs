using System;

namespace MongoMigrations.Migrations
{
    /// <summary>
    /// Represents the current version of the DB schema
    /// </summary>
    internal record SchemaVersion(int Version)
    {
        public Guid Id = Guid.Parse("5D6D0977-EB1E-4D15-84F4-6535A411F306"); // required for mongo
    }
}