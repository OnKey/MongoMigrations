using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using MongoMigrations.MigrationRunner;
using NSubstitute;
using Xunit;

namespace MongoMigrations.Test.MigrationRunner
{
    public class MigrationInterceptionProviderTest
    {
        private readonly MigrationInterceptionProvider sut;
        private readonly IVersionLocator versionLocator;
        private readonly ServiceProvider serviceProvider;
        private readonly IMigrationInterceptor<TestDoc> migrationInterceptor;

        public MigrationInterceptionProviderTest()
        {
            this.versionLocator = Substitute.For<IVersionLocator>();
            this.versionLocator.VersionFieldName().Returns("SchemaVersion");
            this.migrationInterceptor = Substitute.For<IMigrationInterceptor<TestDoc>>();

            var services = new ServiceCollection();
            services.AddSingleton(this.migrationInterceptor);
            this.serviceProvider = services.BuildServiceProvider();

            this.sut = new MigrationInterceptionProvider(this.versionLocator, this.serviceProvider);
        }

        [Fact]
        public void ShouldReturnInterceptorForClassesWithMigrations()
        {
            this.versionLocator.IsVersioned(typeof(TestDoc)).Returns(true);

            var actual = this.sut.GetSerializer(typeof(TestDoc));

            actual.Should()
                .NotBeNull()
                .And.Be(this.migrationInterceptor);
        }

        [Fact]
        public void ShouldReturnNullForOtherClasses()
        {
            this.versionLocator.IsVersioned(typeof(TestDoc)).Returns(false);

            var actual = this.sut.GetSerializer(typeof(TestDoc));

            actual.Should().BeNull();
        }

        public class TestDoc
        {
        }
    }
}