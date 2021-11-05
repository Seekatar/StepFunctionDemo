using System.Threading.Tasks;

namespace CCC.CAS.Workflow2Service.Services
{
    public interface IWorkflowActivity
    {
        Task Start(string input);

        Task Complete(object? output);

        Task Fail(WorkflowError workflowError);

        string TaskToken { get; set; }
    }
}

