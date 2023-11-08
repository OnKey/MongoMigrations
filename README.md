# Mongo Migrations

Mongo Migrations is used with the dotnet Mongo DB driver to manage migrations as the database (i.e. adding indexes, renaming collections, etc.) or documents (i.e. field renaming) have their schema changes. 
It is similar to SRoddis/Mongo.Migration but does not require any additional attributes or version fields to be added to your entity classes. In fact, if you're using DDD and have a separate project for your
domain classes, that project does not need any dependency on Mongo Migrations at all, at the migrations are completely manages as part of serialisation/deserialisation in the Mongo driver.

## Installation

Install via nuget https://www.nuget.org/packages/Onkey.MongoMigration

PM> Install-Package Onkey.MongoMigration

## Quick Start

1. Write a migration for a class by implementing the IDatabaseMigration interface
```
public class User_UpdateFullName : IDocumentMigration
{
    public int Version { get; } = 1;
    public MigrationTiming WhenToMigrate { get; } = MigrationTiming.AtAppStart;
    public Type DocumentType { get; } = typeof(User);
    
    public void Up(BsonDocument document)
    {
        document["FullName"] = document["FirstName"] + " " + document["LastName"];
    }

    public void Down(BsonDocument document)
    {
        document.Remove("FullName");
    }
}
```

2. Register MongoMigration with your DI container and add the mapping of C# classes to Mongo collections

```
var builder = WebApplication.CreateBuilder(args);

// Register IMongoDatabase with DI in the normal way

builder.Services.AddMongoMigrations()
                .AddType(typeof(User), "Users")
                .AddType(typeof(TestDoc2), "TestDoc2Collection");
```

3. Register any document and database migrations with your DI container. Document migrations will migrate individual documents and are executed for 
each document in a collection; Database migrations just happen once for the database and are used for things like adding indexes.

```
builder.Services.AddTransient<IDocumentMigration, User_UpdateFullName>();
builder.Services.AddTransient<IDatabaseMigration, Db_AddIndex>();
```

These can also be registered by scanning an assembly if your don't want to add them individually (or use scanning in your DI container/Scrutor).

```
builder.Services.ScanForMongoMigrations(typeof(Program).Assembly);
```

4. Call RunDbMigrations() after the IHost is built. This will register the required serialisers and run any migrations
required on startup.

```
var host = builder.Build();

await host.RunDbMigrations();

await host.RunAsync();
```

5. Access Mongo as normal. Migrations will happen automatically as classes are serialised/deserialised.