using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson.Serialization;
using MongoMigrations.MigrationRunner;
using NSubstitute;

namespace MongoMigrations.Test.MigrationRunner
{
    public class SerialiserHelper
    {
        public static void RegisterMigrationInterceptor<TDocument>(IMigrationInterceptor<TDocument> migrationInterceptor)
        {
            // Replace the version in the cache with the correct one for the test
            var cache = GetGlobalSerialiserCache();
            cache[typeof(TDocument)] = migrationInterceptor;
        }

        public static ServiceProvider RegisterVersionInterceptor(IVersionLocator versionLocator)
        {
            ResetSerialiserCache();

            var docMigrationRunner = Substitute.For<IDocumentMigrationRunner>();
            var services = new ServiceCollection();
            services.AddSingleton(versionLocator);
            services.AddSingleton(typeof(IMigrationInterceptor<>), typeof(MigrationInterceptor<>));
            services.AddSingleton<IDocumentMigrationRunner>(docMigrationRunner);
            
            var serviceProvider = services.BuildServiceProvider();

            BsonSerializer.RegisterSerializationProvider(
                new MigrationInterceptionProvider(versionLocator, serviceProvider));

            return serviceProvider;
        }

        /// <summary>
        /// Check that bson serialiser has been registered correctly for the test
        /// </summary>
        public static void AssertCorrectVersionLocatorInUse<TDocument>(IVersionLocator expectedVersionLocator)
        {
            var serialiser = BsonSerializer.LookupSerializer<TDocument>();
            IsInterceptor(serialiser).Should().BeTrue();

            var actualInterceptor =
                GetPrivateField<IVersionLocator, MigrationInterceptor<TDocument>>(
                    (MigrationInterceptor<TDocument>)serialiser, "versionLocator");

            actualInterceptor.Should().Be(expectedVersionLocator);

            // need to reset the cache again, as the lookup above will cause the serialiser to be cached
            // which can have side effects in the tests
            ResetSerialiserCache();
        }

        private static void ResetSerialiserCache()
        {
            // serialisers are cached in a global static field, so need to be reset between unit tests
            var cache = GetGlobalSerialiserCache();

            // Remove any interceptors already registered
            foreach (var cacheKey in cache.Keys)
            {
                var serialiser = cache[cacheKey];
                var isInterceptor = IsInterceptor(serialiser);

                if (isInterceptor)
                {
                    cache.Remove(cacheKey, out _);
                }
            }
        }

        private static bool IsInterceptor(IBsonSerializer serialiser)
        {
            return serialiser.GetType().GetInterfaces().Any(x =>
                x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IMigrationInterceptor<>));
        }

        private static ConcurrentDictionary<Type, IBsonSerializer> GetGlobalSerialiserCache() =>
            GetPrivateField<ConcurrentDictionary<Type, IBsonSerializer>, BsonSerializerRegistry>(
                (BsonSerializerRegistry)BsonSerializer.SerializerRegistry, "_cache");

        /*var cacheProperty =
                typeof(BsonSerializerRegistry).GetField("_cache", BindingFlags.NonPublic | BindingFlags.Instance);

            var cache =
                (ConcurrentDictionary<Type, IBsonSerializer>)cacheProperty!.GetValue(BsonSerializer.SerializerRegistry)!;
            return cache;*/
        private static TField GetPrivateField<TField, TObj>(TObj obj, string fieldname)
        {
            var field = typeof(TObj).GetField(fieldname, BindingFlags.NonPublic | BindingFlags.Instance);
            return (TField)field!.GetValue(obj)!;
        }
    }
}