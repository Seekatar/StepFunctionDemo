using System;
using System.Threading.Tasks;

namespace CCC.CAS.Workflow2Service.Services
{
    interface IWorkflow
    {
        Task Complete(WorkflowActivityBase activity, object? output);
        Task Complete(string taskToken, string name, object? output);
        Task Fail(WorkflowActivityBase activity, WorkflowError error);
        Task SaveActivityState(IWorkflowActivity activity, Guid correlationId);
        Task<string?> RetrieveActivityState(Type activityType, Guid correlationId);
    }
}

