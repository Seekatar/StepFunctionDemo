using CCC.CAS.Workflow2Service.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CCC.CAS.Workflow4Api.Services
{
    public interface IWorkflowStateRepository
    {
        Task SaveActivityState(IWorkflowActivity activity, Guid correlationId);
        Task<WorkflowActivityHandle?> RetrieveActivityState(string activityTypeName, Guid correlationId);
    }
}
