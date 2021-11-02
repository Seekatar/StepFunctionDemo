using System;
using System.Collections.Generic;

namespace CCC.CAS.Workflow2Service.Services
{
    interface IWorkflowActivityFactory
    {
        IWorkflowActivity? CreatePausedActivity(Type workflowActivityType, Guid correlationId, IWorkflow workflow);
        IWorkflowActivity? CreateActivity(string taskName, IWorkflow workflow, string taskToken);
        List<string> ActivityNames { get; }
    }
#pragma warning restore CA1812
}

