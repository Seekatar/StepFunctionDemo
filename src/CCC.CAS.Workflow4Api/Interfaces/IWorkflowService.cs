using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CCC.CAS.Workflow4Api.Interfaces
{
    public interface IWorkflowService
    {
        Task StartWorkflow(int scenario, string s);
        Task RestartWorkflow(string taskToken);
    }
}
