using Microsoft.Extensions.Logging;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace CCC.CAS.Workflow2Service.Services
{
    abstract class WorkflowActivity<TInput, TOutput> : WorkflowActivityBase where TInput : CorrelatedActivityInput
    {
        protected WorkflowActivity(IWorkflow workflow, ILogger logger) : base(workflow, logger)
        {
        }

        public async override Task Start(string input, WorkflowActivityHandle handle)
        {
            Handle = handle;
            try
            {
                var inputObj = JsonSerializer.Deserialize<TInput>(input, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                await Start(inputObj).ConfigureAwait(false);
                if (!IsActivityComplete)
                {
                    if (inputObj != null)
                    {
                        await Workflow.SaveActivityState(this, inputObj.CorrelationId).ConfigureAwait(false);
                    }
                    else
                    {
                        await Fail(new WorkflowError { ActivityName = GetType().Name, Reason = WorkflowError.ReasonCode.Error, Message = "For long running activities, must have input with correlationId" }).ConfigureAwait(false);
                    }
                }
            }
            catch (Exception e) when (e is JsonException || e is NotSupportedException)
            {
                await Fail(new WorkflowError { ActivityName = GetType().Name, Reason = WorkflowError.ReasonCode.Error, Message = "Json deserialation error. " + e }).ConfigureAwait(false);
            }
        }

        public abstract Task Start(TInput? input);

        public Task Complete(TOutput? output)
        {
            return base.Complete(output);
        }

        public Task Complete(WorkflowActivityHandle handle, TOutput? output)
        {
            return base.Complete(handle, output);
        }

    }
}

