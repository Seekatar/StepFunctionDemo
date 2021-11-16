using Microsoft.Extensions.Logging;
using System;
using System.Text.Json;
using System.Threading.Tasks;
using Amazon.StepFunctions;
using Amazon.StepFunctions.Model;
using Polly;
using System.Collections.Generic;
using CCC.CAS.Workflow4Api.Services;

namespace CCC.CAS.Workflow2Service.Services
{
#pragma warning disable CA1812 // never instantiated

    class Workflow : IWorkflow
    {
        private readonly double _retryDelaySeconds = 3;
        private readonly int _retries = 3;
        private readonly ILogger<Workflow> _logger;
        private readonly IWorkflowStateRepository _workflowStateRepository;
        private readonly AmazonStepFunctionsClient _sfClient;

        public Workflow(ILogger<Workflow> logger, IWorkflowStateRepository workflowStateRepository)
        {
            _logger = logger;
            _workflowStateRepository = workflowStateRepository;
            _sfClient = StepFunctionClientFactory.GetClient();
        }

        public Task Complete(WorkflowActivityHandle handle, string name, object? output)
        {
            // TODO any other exceptions?
            return Policy
                    .Handle<TaskTimedOutException>()
                    // .Or<ArgumentException>(ex => ex.ParamName == "example")
                    .WaitAndRetry(_retries, retryAttempt => TimeSpan.FromSeconds(_retryDelaySeconds))
                    .Execute(() => CompleteTask(handle, name, output));
        }

        public Task Complete(WorkflowActivityBase activity, object? output)
        {
            return Complete(activity.Handle, activity.GetType().Name, output);
        }

        public async Task Fail(WorkflowActivityBase activity, WorkflowError error)
        {
            // TODO any other exceptions?
            var task = Policy
                    .Handle<TaskTimedOutException>()
                    .WaitAndRetry(_retries, retryAttempt => TimeSpan.FromSeconds(_retryDelaySeconds))
                    .Execute(async () => await FailTask(activity, error).ConfigureAwait(false));

            await task.ConfigureAwait(false);
        }

        public async Task SaveActivityState(IWorkflowActivity activity, Guid correlationId)
        {
            await _workflowStateRepository.SaveActivityState(activity, correlationId).ConfigureAwait(false);
        }

        public async Task<WorkflowActivityHandle?> RetrieveActivityState(Type activityType, Guid correlationId)
        {
            return await _workflowStateRepository.RetrieveActivityState(activityType?.FullName ?? "", correlationId).ConfigureAwait(false);
        }

        private async Task CompleteTask(WorkflowActivityHandle handle, string name, object? workDemoActivityState)
        {
            var respondActivityTaskCompletedRequest =
                new SendTaskSuccessRequest()
                {
                    Output = JsonSerializer.Serialize(workDemoActivityState),
                    TaskToken = handle.Handle
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

        private async Task FailTask(WorkflowActivityBase activity, WorkflowError error)
        {
            var respondActivityTaskCompletedRequest =
                new SendTaskFailureRequest()
                {
                    Cause = error.Reason.ToString(),
                    Error = error.Message,
                    TaskToken = activity.Handle.Handle
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
#pragma warning restore CA1812 // never instantiated
}

