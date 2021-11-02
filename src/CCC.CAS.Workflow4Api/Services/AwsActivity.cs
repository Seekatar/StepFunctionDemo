using Microsoft.Extensions.Logging;
using System;
using System.Text.Json;
using System.Threading.Tasks;
using Amazon.StepFunctions;
using Amazon.StepFunctions.Model;
using Polly;

namespace CCC.CAS.Workflow2Service.Services
{
    interface IWorkflowActivity
    {
        Task Start(string input);

        Task Complete(object? output);

        Task Fail(WorkflowError error);
    }

    interface IWorkflow
    {
        Task Complete(WorkflowActivity activity, object? output);
        Task Complete(string taskToken, string name, object? output);
        Task Fail(WorkflowActivity activity, WorkflowError error);
    }

    class Workflow : IWorkflow
    {
        private readonly double _retryDelaySeconds = 3;
        private readonly int _retries = 3;
        private readonly ILogger _logger;
        private readonly AmazonStepFunctionsClient _sfClient;

        public Workflow(AmazonStepFunctionsClient sfClient, ILogger logger)
        {
            _logger = logger;
            _sfClient = sfClient;
        }

        public Task Complete(string taskToken, string name, object? output)
        {
            // TODO any other exceptions?
            return Policy
                    .Handle<TaskTimedOutException>()
                    // .Or<ArgumentException>(ex => ex.ParamName == "example")
                    .WaitAndRetry(_retries, retryAttempt => TimeSpan.FromSeconds(_retryDelaySeconds))
                    .Execute(() => CompleteTask(taskToken, name, output));
        }

        public Task Complete(WorkflowActivity activity, object? output)
        {
            return Complete(activity.TaskToken, activity.GetType().Name, output);
        }

        public async Task Fail(WorkflowActivity activity, WorkflowError error)
        {
            // TODO any other exceptions?
            var task = Policy
                    .Handle<TaskTimedOutException>()
                    .WaitAndRetry(_retries, retryAttempt => TimeSpan.FromSeconds(_retryDelaySeconds))
                    .Execute(async () => await FailTask(activity, error).ConfigureAwait(false));

            await task.ConfigureAwait(false);
        }

        public Task SaveTask(WorkflowActivity activity, Guid correlationId)
        {
            throw new NotImplementedException();
        }

        public Task<string> RetrieveTaskId(WorkflowActivity activity, Guid correlationId)
        {
            throw new NotImplementedException();
        }

        private async Task CompleteTask(string taskToken, string name, object? workDemoActivityState)
        {
            var respondActivityTaskCompletedRequest =
                new SendTaskSuccessRequest()
                {
                    Output = JsonSerializer.Serialize(workDemoActivityState),
                    TaskToken = taskToken
                };

            try
            {
                await _sfClient.SendTaskSuccessAsync(respondActivityTaskCompletedRequest).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{activityName} task complete failed", name);
                throw;
            }
        }

        private async Task FailTask(WorkflowActivity activity, WorkflowError error)
        {
            var respondActivityTaskCompletedRequest =
                new SendTaskFailureRequest()
                {
                    Cause = error.Reason.ToString(),
                    Error = error.Message,
                    TaskToken = activity.TaskToken
                };

            try
            {
                await _sfClient.SendTaskFailureAsync(respondActivityTaskCompletedRequest).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{activityName} task fail failed", activity.GetType().Name);
                throw;
            }
        }
    }

    abstract class WorkflowActivity : IWorkflowActivity
    {
        private readonly IWorkflow _workflow;
        private readonly string _taskToken;
        private readonly ILogger _logger;
        private bool _completed;

        protected ILogger Logger => _logger;

        public WorkflowActivity(IWorkflow workflow, string taskToken, ILogger logger)
        {
            _workflow = workflow;
            _taskToken = taskToken;
            _logger = logger;
        }
        public string TaskToken => _taskToken;
        public bool IsCompleted => _completed;

        public abstract Task Start(string input);

        public async Task Complete(object? output)
        {
            await _workflow.Complete(this, output).ConfigureAwait(false);
            _completed = true;
        }
        public async Task Complete(string taskToken, object? output)
        {
            await _workflow.Complete(taskToken, GetType().Name, output).ConfigureAwait(false);
            _completed = true;
        }


        public async Task Fail(WorkflowError error)
        {
            await _workflow.Fail(this, error).ConfigureAwait(false);
            _completed = true;
        }

    }

    [AttributeUsage(AttributeTargets.Class)]
    class WorkflowAttribute : Attribute
    {
        public string Name { get; set; } = "";
    }

    abstract class WorkflowActivity<TInput, TOutput> : WorkflowActivity
    {
        protected WorkflowActivity(IWorkflow workflow, string taskToken, ILogger logger) : base(workflow, taskToken, logger)
        {
        }

        public override Task Start(string input)
        {
            try
            {
                var inputObj = JsonSerializer.Deserialize<TInput>(input, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                return Start(inputObj);
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

