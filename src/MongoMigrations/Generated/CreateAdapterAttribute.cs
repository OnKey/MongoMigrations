using System;

namespace AutoAdapter
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
    public class CreateAdapterAttribute : Attribute
    {
        public CreateAdapterAttribute(Type type)
        {
        }
    }
}