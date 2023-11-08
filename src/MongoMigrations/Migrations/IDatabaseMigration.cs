using System.Threading.Tasks;

namespace MongoMigrations.Migrations
{
    /// <summary>
    /// Used to register any DB migrations which need to take place on start up
    /// </summary>
    public interface IDatabaseMigration
    {
        int Version { get; }

        /// <summary>
        /// Migrate database from previous version to this version
        /// </summary>
        Task Up();

        /// <summary>
        /// Migration database from this version to previous version
        /// </summary>
        Task Down();
    }
}