using System;

namespace MongoMigrations.Test
{
    public class TestEntity
    {
        public Guid Id { get; set; }
        public string Value { get; set; } = string.Empty;
    }
    
    public class TestUser
    {
        public Guid Id { get; set; }
        public string Value { get; set; } = string.Empty;
    }
}