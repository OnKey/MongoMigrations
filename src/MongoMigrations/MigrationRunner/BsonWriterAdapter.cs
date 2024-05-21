using System;
using MongoDB.Bson.IO;
using MongoDB.Bson.IO.Generator;

namespace MongoMigrations.MigrationRunner
{
    internal class BsonWriteIntercept : BsonWriterAdapter
    {
        private Action<IBsonWriter>? actionBeforeEnd;

        public BsonWriteIntercept(IBsonWriter inner) : base(inner)
        {
        }

        internal void BeforeEndDocument(Action<IBsonWriter> action) => this.actionBeforeEnd = action;

        public override void WriteEndDocument()
        {
            // only run if we're ending the top level doc, not nested documents
            if (this.SerializationDepth == 1)
            {
                this.actionBeforeEnd?.Invoke(this.inner);
            }
            
            base.WriteEndDocument();
        }
    }
}