using System;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoMigrations.Migrations;

namespace MongoMigrations.MigrationRunner
{
    /// <summary>
    /// Intercepts requests to deserialise a document and performs any IDocumentMigrations required. Also sets
    /// the version field to the correct value when serialising.
    /// </summary>
    internal class MigrationInterceptor<TDocument> : BsonClassMapSerializer<TDocument>, IMigrationInterceptor<TDocument>
    {
        private readonly IVersionLocator versionLocator;
        private readonly IDocumentMigrationRunner documentMigrationRunner;

        public MigrationInterceptor(IVersionLocator versionLocator, IDocumentMigrationRunner documentMigrationRunner) : base(BsonClassMap.LookupClassMap(typeof(TDocument)))
        {
            this.versionLocator = versionLocator;
            this.documentMigrationRunner = documentMigrationRunner;
        }

        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, TDocument doc)
        {
            var version = this.versionLocator.GetTargetVersion(typeof(TDocument));
            var newContext = this.AddVersionField(context, version);
            base.Serialize(newContext, args, doc);
        }

        public override TDocument Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            var document = BsonDocumentSerializer.Instance.Deserialize(context);
        
            this.documentMigrationRunner.MigrateDocument(document, typeof(TDocument), MigrationTiming.OnDocumentAccess);
            document.Remove(this.versionLocator.VersionFieldName());

            var migratedContext =
                BsonDeserializationContext.CreateRoot(new BsonDocumentReader(document));

            return base.Deserialize(migratedContext, args);
        }

        private BsonSerializationContext AddVersionField(BsonSerializationContext context, int version)
        {
            var versionFieldName = this.versionLocator.VersionFieldName();
            if (string.IsNullOrEmpty(versionFieldName))
            {
                throw new ArgumentOutOfRangeException(nameof(this.versionLocator.VersionFieldName),
                    "Version field name cannot be set to null or empty");
            }

            // there doesn't seem be any good hook to add additional fields which aren't in the object
            // so we intercept the call to WriteEndDocument and add the extra field then
            var writeIntercept = new BsonWriteIntercept(context.Writer);
            writeIntercept.BeforeEndDocument(x =>
            {
                x.WriteName(versionFieldName);
                x.WriteInt32(version);
            });

            var newContext = BsonSerializationContext.CreateRoot(writeIntercept);
            return newContext;
        }
    }
}