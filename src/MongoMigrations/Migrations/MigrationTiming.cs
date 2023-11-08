namespace MongoMigrations.Migrations
{
    /// <summary>
    /// When a document should have a migration applied
    /// </summary>
    public enum MigrationTiming
    {
        AtAppStart, OnDocumentAccess
    }
}