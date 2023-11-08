using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using MongoMigration.Sample.Entites;
using MongoMigration.Sample.Migrations;
using MongoMigrations;
using MongoMigrations.Migrations;

namespace MongoMigration.Sample
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = Host.CreateApplicationBuilder(args);
            builder.Services.AddHostedService<SampleService>();
            builder.Logging.ClearProviders();
            builder.Logging.AddConsole();
            
            // Register IMongoDatabase in the normal way
            var client = new MongoClient("mongodb://localhost:27017");
            var db = client.GetDatabase("testDb");
            builder.Services.AddSingleton(db);

            // Need to add mongo migrations to DI and tell it the names of the collections used to store our classes
            builder.Services.AddMongoMigrations()
                .AddType(typeof(User), "Users")
                .AddType(typeof(TestDoc2), "TestDoc2Collection");

            // Register any migrations with DI either manually or through scanning for the
            // IDocumentMigration/IDatabaseMigration interface
            builder.Services.AddTransient<IDocumentMigration, User_UpdateFullName>();
            builder.Services.AddTransient<IDatabaseMigration, Db_AddIndex>();
            
            var host = builder.Build();

            // Execute any migrations required on startup
            await host.RunDbMigrations();
            
            await host.RunAsync();
        }
    }
}