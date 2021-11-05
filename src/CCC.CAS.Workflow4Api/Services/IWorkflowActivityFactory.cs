using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CCC.CAS.Workflow2Service.Services
{
    interface IWorkflowActivityFactory
    {
        Task<IWorkflowActivity?> CreatePausedActivity(Type workflowActivityType, Guid correlationId, IWorkflow workflow);
        Task<IWorkflowActivity?> CreateActivity(string taskName, IWorkflow workflow, string taskToken);
        List<string> ActivityNames { get; }
    }
}

