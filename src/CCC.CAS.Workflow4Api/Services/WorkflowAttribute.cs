using System;

namespace CCC.CAS.Workflow2Service.Services
{
    [AttributeUsage(AttributeTargets.Class)]
    class WorkflowAttribute : Attribute
    {
        public string Name { get; set; } = "";
    }
}

