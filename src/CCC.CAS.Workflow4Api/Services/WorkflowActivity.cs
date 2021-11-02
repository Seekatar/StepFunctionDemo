using Microsoft.Extensions.Logging;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace CCC.CAS.Workflow2Service.Services
{
    abstract class WorkflowActivity<TInput, TOutput> : WorkflowActivityBase where TInput : ActivityInputBase
    {
        protected WorkflowActivity(IWorkflow workflow, string taskToken, ILogger logger) : base(workflow, taskToken, logger)
        {
        }

        public override Task Start(string input)
        {
            try
            {
                var inputObj = JsonSerializer.Deserialize<TInput>(input, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                var ret = Start(inputObj);
                if (!IsActivityComplete)
                {
                    if (inputObj != null)
                    {
                        Workflow.SaveActivityState(this, inputObj.CorrelationId);
                    }
                    // TODO what if no input?
                }
                return ret;
            }
            catch (Exception e) when (e is JsonException || e is NotSupportedException)
            {
                return Fail(new WorkflowError { Reason = WorkflowError.ReasonCode.Error, Message = "Json deserialation error. " + e });
            }
        }

        public abstract Task Start(TInput? input);

        public Task Complete(TOutput? output)
        {
            return base.Complete(output);
        }

        public Task Complete(string taskToken, TOutput? output)
        {
            return base.Complete(taskToken, output);
        }

    }
}

