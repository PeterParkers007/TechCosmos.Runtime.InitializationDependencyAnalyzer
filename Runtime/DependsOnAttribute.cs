using System;

namespace TechCosmos.InitializeSortSystem.Runtime
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public class DependsOnAttribute : Attribute
    {
        public string[] SystemIds { get; }

        public DependsOnAttribute(params string[] systemIds)
        {
            SystemIds = systemIds;
        }
    }
}