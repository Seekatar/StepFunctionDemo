using CCC.CAS.Workflow2Service.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CCC.CAS.Workflow4Api.Interfaces
{
    public interface IWorkflowService
    {
        Task StartDemoWorkflow(WorkDemoActivityState state);
        Task RestartWorkflow(string taskToken);
        Task StartDocWorkflow(WorkDemoActivityState state);
    }
}
