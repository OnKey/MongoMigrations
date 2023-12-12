using System;
using FluentAssertions;
using MongoMigrations.Collections;
using Xunit;

namespace MongoMigrations.Test.Collections
{
    public class CollectionNameResolverTest
    {
        private ICollectionNameResolver sut = new CollectionNameResolver();
        
        [Fact]
        public void ShouldBeAbleToMapTypeToCollectionName()
        {
            sut.AddType(typeof(TestEntity), "TestEntityCollection");

            var actual = sut.GetCollectionName(typeof(TestEntity));

            actual.Should().Be("TestEntityCollection");
        }

        [Fact]
        public void ShouldBeAbleToGetTypeWithGenerics()
        {
            sut.AddType(typeof(TestEntity), "TestEntityCollection");

            var actual = sut.GetCollectionName<TestEntity>();

            actual.Should().Be("TestEntityCollection");
        }

        [Fact]
        public void ShouldThrowExceptionIfTypeNotMapped()
        {
            Func<string> actual = () => sut.GetCollectionName<TestEntity>();

            actual.Should().Throw<MissingDbCollectionConfigurationException>();
        }

        [Fact]
        public void ShouldNotAllowEmptyCollectionName()
        {
            Func<ICollectionNameResolver> actual = () => sut.AddType(typeof(TestEntity), "");

            actual.Should().Throw<ArgumentException>();
        }
    }
}