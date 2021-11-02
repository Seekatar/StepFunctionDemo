﻿using Microsoft.Extensions.Logging;
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
    class Workflow : IWorkflow
    {
        private readonly double _retryDelaySeconds = 3;
        private readonly int _retries = 3;
        private readonly ILogger _logger;
        private readonly IWorkflowStateRepository _workflowStateRepository;
        private readonly AmazonStepFunctionsClient _sfClient;

        public Workflow(AmazonStepFunctionsClient sfClient, ILogger logger, IWorkflowStateRepository workflowStateRepository)
        {
            _logger = logger;
            _workflowStateRepository = workflowStateRepository;
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

        public Task Complete(WorkflowActivityBase activity, object? output)
        {
            return Complete(activity.TaskToken, activity.GetType().Name, output);
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

        public Task SaveActivityState(IWorkflowActivity activity, Guid correlationId)
        {
            return _workflowStateRepository.SaveActivityState(activity, correlationId);
        }

        public Task<string?> RetrieveActivityState(Type activityType, Guid correlationId)
        {
            return _workflowStateRepository.RetrieveActivityState(activityType, correlationId);
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

        private async Task FailTask(WorkflowActivityBase activity, WorkflowError error)
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
}
