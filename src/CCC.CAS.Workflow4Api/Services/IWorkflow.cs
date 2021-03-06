using System;
using System.Threading.Tasks;

namespace CCC.CAS.Workflow2Service.Services
{
    interface IWorkflow
    {
        Task Complete(WorkflowActivityBase activity, object? output);
        Task Complete(WorkflowActivityHandle handle, string name, object? output);
        Task Fail(WorkflowActivityBase activity, WorkflowError error);
        Task SaveActivityState(IWorkflowActivity activity, Guid correlationId);
        Task<WorkflowActivityHandle?> RetrieveActivityState(Type activityType, Guid correlationId);
        Task<IWorkflowActivity?> CreatePausedActivity(Type workflowActivityType, Guid correlationId);
    }
}

