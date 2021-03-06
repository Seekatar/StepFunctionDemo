using Amazon;
using CCC.CAS.API.Common.Storage;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Amazon.StepFunctions;
using Amazon.StepFunctions.Model;
using System.Collections.Generic;
using CCC.CAS.Workflow4Api.Services;
using Seekatar.Tools;
using Seekatar.Interfaces;

namespace CCC.CAS.Workflow2Service.Services
{
#pragma warning disable CA1812

    class AwsWorkflowActivityService : BackgroundService
    {
        private readonly ILogger<AwsWorkflowActivityService> _logger;
        private readonly IObjectFactory<IWorkflowActivity> _workflowActivityFactory;
        const string _arnBase = "arn:aws:states:us-east-1:620135122039:activity:";
        readonly string[] _myActivities = {  "Cas-Rbr-DemoActivity1",
                                    "Cas-Rbr-DemoActivity2",
                                    "Cas-Rbr-DemoActivity3",
                                    "Cas-Rbr-DemoActivity4",
                                    "Cas-Rbr-DocMain",
        };

        public AwsWorkflowActivityService(ILogger<AwsWorkflowActivityService> logger,
                                            IObjectFactory<IWorkflowActivity> workflowActivityFactory)
        {
            _logger = logger;
            _workflowActivityFactory = workflowActivityFactory;

            // TESTING ONLY
            TestPpoBase.WorkflowActivityFactory = _workflowActivityFactory;
        }


        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var sfClient = StepFunctionClientFactory.GetClient();

            await RegisterActivities(sfClient).ConfigureAwait(false);

            var tasks = new List<Task>();

            foreach (var arn in _myActivities)
            {
                tasks.Add(PollForActivity($"{_arnBase}{arn}", sfClient, stoppingToken));
            }
            foreach (var arn in _workflowActivityFactory.LoadedTypes.Keys)
            {
                tasks.Add(PollForActivity($"{_arnBase}{arn}", sfClient, stoppingToken));
            }

            Task.WaitAll(tasks.ToArray(), stoppingToken);
        }

        private async Task PollForActivity(string arn, AmazonStepFunctionsClient sfClient, CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                bool errorOut = true;
                var taskName = arn.Split(':').Last();
                _logger.LogDebug(">>> {task} polling", taskName);
                var activityTask = await Poll(sfClient, arn).ConfigureAwait(false);

                if (string.IsNullOrEmpty(activityTask?.TaskToken))
                    continue;

                WorkDemoActivityState? workDemoActivityState = null;
                var handle = new WorkflowActivityHandle(activityTask.TaskToken);
                try
                {
                    var activity = _workflowActivityFactory.GetInstance(taskName);
                    if (activity != null)
                    {    
                         await activity.Start(activityTask.Input, handle).ConfigureAwait(false);
                    }
                    else // Demo, old way
                    {
                        workDemoActivityState = JsonSerializer.Deserialize<WorkDemoActivityState>(activityTask.Input);

                        _logger.LogInformation(">>> {task} fired with state {scenario}", taskName, workDemoActivityState?.ScenarioNumber ?? -1);

                        if (workDemoActivityState != null)
                        {
                            workDemoActivityState = ProcessTask(arn, workDemoActivityState);
                            if (taskName == "Cas-Rbr-DemoActivity1")
                            {
                                if (workDemoActivityState.ClientCode.Equals("USAA", StringComparison.OrdinalIgnoreCase))
                                {
                                    _logger.LogInformation(">>> {task} Completing....", taskName);
                                    await CompleteTask(sfClient, new WorkflowActivityHandle(activityTask.TaskToken), workDemoActivityState).ConfigureAwait(false); ;
                                    errorOut = false;
                                }
                                else if (workDemoActivityState.ClientCode.Equals("GEICO", StringComparison.OrdinalIgnoreCase))
                                {
                                    _logger.LogInformation(">>> {task} Not completing....", taskName);
                                    _logger.LogInformation(">>> {taskToken}", activityTask.TaskToken);
                                    errorOut = false;
                                }
                                else
                                {
                                    _logger.LogInformation(">>> {task} Failing....", taskName);
                                    await FailTask(sfClient, new WorkflowActivityHandle(activityTask.TaskToken), workDemoActivityState).ConfigureAwait(false); ;
                                    errorOut = false;
                                }
                            }
                            else
                            {
                                _logger.LogInformation(">>> {task} Completing....", taskName);
                                await CompleteTask(sfClient, new WorkflowActivityHandle(activityTask.TaskToken), workDemoActivityState).ConfigureAwait(false); ;
                                errorOut = false;
                            }
                            if (!errorOut && taskName == "Cas-Rbr-Ppo-Exit")
                            {
                                if (!string.IsNullOrEmpty(workDemoActivityState.TaskToken))
                                {
                                    _logger.LogInformation(">>> {task} Completing parent task from....", taskName);
                                    await CompleteTask(sfClient, new WorkflowActivityHandle(activityTask.TaskToken), workDemoActivityState).ConfigureAwait(false);
                                }
                            }
                        }
                        else
                        {
                            _logger.LogError("Didn't get state for {task} with token {taskToken}", taskName, activityTask.TaskToken);
                        }
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Error processing task {task}", taskName);
                    if (errorOut)
                    {
                        await FailTask(sfClient, new WorkflowActivityHandle(activityTask.TaskToken), workDemoActivityState ?? new WorkDemoActivityState()).ConfigureAwait(false); ;
                    }
                }
                Thread.Sleep(100);
            }
        }

        private async Task FailTask(AmazonStepFunctionsClient sfClient, WorkflowActivityHandle handle, WorkDemoActivityState workDemoActivityState)
        {
            var respondActivityTaskCompletedRequest =
                new SendTaskFailureRequest()
                {
                    Cause = "Because",
                    Error = "Ouch!",
                    TaskToken = handle.Handle
                };

            try
            {
                await sfClient.SendTaskFailureAsync(respondActivityTaskCompletedRequest).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{workDemoActivityState} task complete failed: {ex}");
            }
        }

        private async Task RegisterActivities(AmazonStepFunctionsClient sfClient)
        {
            var activities = await sfClient.ListActivitiesAsync(new ListActivitiesRequest() { }).ConfigureAwait(false);

            foreach (var a in _myActivities)
            {
                if (!activities.Activities.Any(o => o.Name == a))
                {
                    await sfClient.CreateActivityAsync(new CreateActivityRequest() { Name = a, Tags = new List<Tag> { new Tag { Key = "Casualty", Value = "test-jmw" } } }).ConfigureAwait(false);
                    _logger.LogInformation("Added activity {activity}", a);
                }
            }
            foreach (var a in _workflowActivityFactory.LoadedTypes.Keys)
            {
                if (!activities.Activities.Any(o => o.Name == a))
                {
                    await sfClient.CreateActivityAsync(new CreateActivityRequest() { Name = a, Tags = new List<Tag> { new Tag { Key = "Casualty", Value = "test-jmw" } } }).ConfigureAwait(false);
                    _logger.LogInformation("Added activity {activity}", a);
                }
            }

            return;
        }

        private static WorkDemoActivityState ProcessTask(string activityTypeName, WorkDemoActivityState workDemoActivityState)
        {
            if (!int.TryParse(activityTypeName.Last().ToString(), out var id))
                id = -1;

            WorkDemo.WasteTime(
                id,
                workDemoActivityState);

            return workDemoActivityState;
        }

        private async Task CompleteTask(
            AmazonStepFunctionsClient amazonSimpleWorkflowClient,
            WorkflowActivityHandle handle, WorkDemoActivityState workDemoActivityState)
        {

            var respondActivityTaskCompletedRequest =
                new SendTaskSuccessRequest()
                {
                    Output = JsonSerializer.Serialize(workDemoActivityState),
                    TaskToken = handle.Handle
                };

            try
            {
                await amazonSimpleWorkflowClient.SendTaskSuccessAsync(respondActivityTaskCompletedRequest).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{workDemoActivityState} task complete failed: {ex}");
            }
        }

        private static async Task<GetActivityTaskResponse> Poll(AmazonStepFunctionsClient client, string arn)
        {
            var req = new GetActivityTaskRequest() { ActivityArn = arn };
            return await client.GetActivityTaskAsync(req).ConfigureAwait(false);
        }
    }
#pragma warning restore CA1812
}

