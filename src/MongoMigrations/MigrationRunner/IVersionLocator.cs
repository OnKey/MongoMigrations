using System;

namespace MongoMigrations.MigrationRunner
{
    /// <summary>
    /// Get the target schema version for a Type
    /// </summary>
    public interface IVersionLocator
    {
        int GetTargetVersion(Type t);
        string VersionFieldName();
        bool IsVersioned(Type t);
    }
}