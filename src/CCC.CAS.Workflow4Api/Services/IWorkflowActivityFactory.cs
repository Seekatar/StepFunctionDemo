using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CCC.CAS.Workflow2Service.Services
{
    interface IWorkflowActivityFactory
    {
        Task<IWorkflowActivity?> CreatePausedActivity(Type workflowActivityType, Guid correlationId);
        Task<IWorkflowActivity?> CreateActivity(string taskName, IWorkflow workflow, WorkflowActivityHandle handle);
        List<string> ActivityNames { get; }
    }
}

