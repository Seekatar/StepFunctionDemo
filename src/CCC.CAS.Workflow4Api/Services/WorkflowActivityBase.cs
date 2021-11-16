using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace CCC.CAS.Workflow2Service.Services
{
    abstract class WorkflowActivityBase : IWorkflowActivity
    {
        private readonly IWorkflow _workflow;
        private readonly ILogger _logger;
        private bool _completed;

        protected ILogger Logger => _logger;
        protected IWorkflow Workflow => _workflow;

        public WorkflowActivityBase(IWorkflow workflow, ILogger logger)
        {
            _workflow = workflow;
            _logger = logger;
        }
        public WorkflowActivityHandle Handle { get; set; } = new WorkflowActivityHandle();

        public bool IsActivityComplete => _completed;

        public abstract Task Start(string input);

        public async Task Complete(object? output)
        {
            await _workflow.Complete(this, output).ConfigureAwait(false);
            _completed = true;
        }
        public async Task Complete(WorkflowActivityHandle handle, object? output)
        {
            await _workflow.Complete(handle, GetType().Name, output).ConfigureAwait(false);
            _completed = true;
        }


        public async Task Fail(WorkflowError error)
        {
            await _workflow.Fail(this, error).ConfigureAwait(false);
            _completed = true;
        }

    }
}

