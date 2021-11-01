using Microsoft.Extensions.Logging;
using System;
using System.Text.Json;
using System.Threading.Tasks;
using Amazon.StepFunctions;
using Amazon.StepFunctions.Model;
using Polly;

namespace CCC.CAS.Workflow2Service.Services
{
    interface IAwsActivity
    {
        string Name { get; }
        Task Start(object? input);

        Task Complete(object? output);

        Task Fail(WorkflowError error);
    }

    abstract class AwsActivity : IAwsActivity
    {
        private readonly AmazonStepFunctionsClient _sfClient;
        private readonly string _taskToken;
        private readonly ILogger _logger;
        private readonly string _activityName;
        private readonly double _retryDelaySeconds = 3;
        private readonly int _retries = 3;

        protected ILogger Logger => _logger;

        public AwsActivity(AmazonStepFunctionsClient sfClient, string taskToken, ILogger logger)
        {
            _sfClient = sfClient;
            _taskToken = taskToken;
            _logger = logger;
            _activityName = this.GetType().Name;
        }
        public string Name => _activityName;

        public abstract Task Start(object? input);

        public Task Complete(object? output)
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

        public Task SaveTask(AwsActivity activity, Guid correlationId )
        {
            throw new NotImplementedException();
        }

        public Task<string> RetrieveTaskId(AwsActivity activity, Guid correlationId)
        {
            throw new NotImplementedException();
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


    abstract class AwsActivity<TInput, TOutput> : AwsActivity
    {
        protected AwsActivity(AmazonStepFunctionsClient sfClient, string taskToken, ILogger logger) : base(sfClient, taskToken, logger)
        {
        }

        public Task Start(string input)
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

    }
}

