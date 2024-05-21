using System;
using MongoDB.Bson.Serialization;

namespace MongoMigrations.MigrationRunner
{
    /// <summary>
    /// Checks whether types being serialised/deserialised to Mongo have migrations defined; if so, then it adds an
    /// interceptor to perform any document migration tasks required.
    /// </summary>
    internal class MigrationInterceptionProvider : IBsonSerializationProvider
    {
        private readonly IVersionLocator versionLocator;
        private readonly IServiceProvider serviceProvider;

        public MigrationInterceptionProvider(IVersionLocator versionLocator, IServiceProvider serviceProvider)
        {
            this.versionLocator = versionLocator;
            this.serviceProvider = serviceProvider;
        }

        public IBsonSerializer? GetSerializer(Type type)
        {
            if (this.versionLocator.IsVersioned(type))
            {
                var genericType = typeof(IMigrationInterceptor<>).MakeGenericType(type);
                var interceptor = this.serviceProvider.GetService(genericType);
                if (interceptor is null)
                {
                    throw new Exception("IMigrationInterceptor<> has not been registered with dependancy injection");
                }

                return (IBsonSerializer)interceptor;
            }

            return null;
        }
    }
}