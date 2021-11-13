using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CCC.CAS.Workflow2Service.Services
{
    interface IWorkflowActivityFactory
    {
        Task<(string? Token, IWorkflowActivity? Activity)> CreatePausedActivity(Type workflowActivityType, Guid correlationId);
        Task<IWorkflowActivity?> CreateActivity(string taskName, IWorkflow workflow, string taskToken);
        List<string> ActivityNames { get; }
    }
}

