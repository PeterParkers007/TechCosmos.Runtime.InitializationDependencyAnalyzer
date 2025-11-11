using System;

namespace TechCosmos.InitializeSortSystem.Runtime
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public class InitializeAttribute : Attribute
    {
        public string InitializationId { get; }

        public InitializeAttribute(string initializationId)
        {
            InitializationId = initializationId;
        }
    }
}
