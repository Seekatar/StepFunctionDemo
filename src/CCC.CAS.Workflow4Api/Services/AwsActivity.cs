using Microsoft.Extensions.Logging;
using System;
using System.Text.Json;
using System.Threading.Tasks;
using Amazon.StepFunctions;
using Amazon.StepFunctions.Model;
using Polly;

namespace CCC.CAS.Workflow2Service.Services
{
    abstract class AwsActivity<TInput, TOutput>
    {
        private readonly AmazonStepFunctionsClient _sfClient;
        private readonly string _taskToken;
        private readonly ILogger _logger;
        private readonly string _activityName;
        private readonly double _retryDelaySeconds = 3;
        private readonly int _retries = 3;

        protected ILogger Logger => _logger;


        protected AwsActivity(AmazonStepFunctionsClient sfClient, string taskToken, ILogger logger)
        {
            _sfClient = sfClient;
            _taskToken = taskToken;
            _logger = logger;
            _activityName = this.GetType().Name;
        }

        public string Name { get; set; } = "";
        public abstract Task Start(TInput input);
        public Task Complete(TOutput output)
        {
            // TODO any other exceptions?
            return Policy
                    .Handle<TaskTimedOutException>()
                    // .Or<ArgumentException>(ex => ex.ParamName == "example")
                    .WaitAndRetry(_retries, retryAttempt => TimeSpan.FromSeconds(_retryDelaySeconds))
                    .Execute(() => CompleteTask(_sfClient, _taskToken, output));
        }

        public async Task Fail(WorkflowError error)
        {
            // TODO any other exceptions?
            var task = Policy
                    .Handle<TaskTimedOutException>()
                    .WaitAndRetry(_retries, retryAttempt => TimeSpan.FromSeconds(_retryDelaySeconds))
                    .Execute(async () => await FailTask(_sfClient, _taskToken, error).ConfigureAwait(false));

            await task.ConfigureAwait(false);
        }

        private async Task CompleteTask(
            AmazonStepFunctionsClient amazonSimpleWorkflowClient,
            string taskToken, object? workDemoActivityState)
        {
            var respondActivityTaskCompletedRequest =
                new SendTaskSuccessRequest()
                {
                    Output = JsonSerializer.Serialize(workDemoActivityState),
                    TaskToken = taskToken
                };

            try
            {
                await amazonSimpleWorkflowClient.SendTaskSuccessAsync(respondActivityTaskCompletedRequest).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{activityName} task complete failed", _activityName);
                throw;
            }
        }

        private async Task FailTask(AmazonStepFunctionsClient sfClient, string taskToken, WorkflowError error)
        {
            var respondActivityTaskCompletedRequest =
                new SendTaskFailureRequest()
                {
                    Cause = error.Reason.ToString(),
                    Error = error.Message,
                    TaskToken = taskToken
                };

            try
            {
                await sfClient.SendTaskFailureAsync(respondActivityTaskCompletedRequest).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{activityName} task fail failed", _activityName);
                throw;
            }
        }

    }
}

