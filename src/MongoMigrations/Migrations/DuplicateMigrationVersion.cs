using System;

namespace MongoMigrations.Migrations
{
    public class DuplicateMigrationVersion : Exception
    {
        public DuplicateMigrationVersion(Type t, int version) : base($"Multiple document migrations defined for type {t} with version number {version}")
        {
        }
    }
}