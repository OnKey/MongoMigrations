using System;
using System.Linq;
using MongoDB.Bson;
using MongoMigration.Sample.Entites;
using MongoMigrations.Migrations;

namespace MongoMigration.Sample.Migrations
{
    public class User_UpdateFullName : IDocumentMigration
    {
        public int Version { get; } = 1;
        public MigrationTiming WhenToMigrate { get; } = MigrationTiming.AtAppStart;
        public Type DocumentType { get; } = typeof(User);
        
        public void Up(BsonDocument document)
        {
            var fullname = document.Names.Contains("FullName") ? document["FullName"].AsString : "";
            if (string.IsNullOrEmpty(fullname))
            {
                document["FullName"] = document["FirstName"] + " " + document["LastName"];
            }
        }

        public void Down(BsonDocument document)
        {
            document.Remove("FullName");
        }
    }
}