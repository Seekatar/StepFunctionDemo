using Amazon;
using CCC.CAS.API.Common.Storage;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Amazon.StepFunctions;
using Amazon.StepFunctions.Model;
using System.Collections.Generic;

namespace CCC.CAS.Workflow2Service.Services
{
    public class AwsWorkflowActivityService : BackgroundService
    {
        private ILogger<AwsWorkflowActivityService> _logger;
        private readonly AwsWorkflowConfiguration _config;
        string[] ARNs = { "arn:aws:states:us-east-1:620135122039:activity:DemoActivity1", "arn:aws:states:us-east-1:620135122039:stateMachine:MyStateMachine" };

        public AwsWorkflowActivityService(IOptions<AwsWorkflowConfiguration> config, ILogger<AwsWorkflowActivityService> logger)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            _logger = logger;
            _config = config.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var sfClient = new AmazonStepFunctionsClient(_config.AccessKey, _config.SecretKey, RegionEndpoint.GetBySystemName(_config.Region));

            await RegisterAsNeeded(sfClient).ConfigureAwait(false);

            var tasks = new List<Task>();

            foreach (var a in ARNs)
            {
                tasks.Add(Task.Run(async () => { await doit(a, sfClient, stoppingToken).ConfigureAwait(false); }, stoppingToken));
            }

            Task.WaitAll(tasks.ToArray(), stoppingToken);
        }

        private async Task doit(string arn, AmazonStepFunctionsClient sfClient, CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {

                _logger.LogDebug($"{nameof(AwsWorkflowActivityService)} polling for {arn}");
                var activityTask = await Poll(sfClient, arn).ConfigureAwait(false);

                if (string.IsNullOrEmpty(activityTask?.TaskToken))
                    continue;

                var workDemoActivityState = JsonSerializer
                    .Deserialize<WorkDemoActivityState>(activityTask.Input);

                _logger.LogInformation("Task is {task}", activityTask.TaskToken);

                if (workDemoActivityState != null)
                {
                    workDemoActivityState = ProcessTask(arn, workDemoActivityState);
                    if (workDemoActivityState.ScenarioNumber == 1)
                    {
                        var _ = Task.Run(async () =>
                        {
                            _logger.LogInformation("Waiting....");
                            await Task.Delay(30000).ConfigureAwait(false);
                            await CompleteTask(sfClient, activityTask.TaskToken, workDemoActivityState).ConfigureAwait(false);
                        }, stoppingToken);
                    }
                    else
                    {
                        await CompleteTask (sfClient, activityTask.TaskToken, workDemoActivityState).ConfigureAwait(false); ;
                    }
                }
                Thread.Sleep(100);
            }
        }

        private async Task RegisterAsNeeded(AmazonStepFunctionsClient sfClient)
        {
            string[] myActivities = { "DemoActivity1", "DemoActivity2", "DemoActivity3", "DemoActivity4" };
            var activities = await sfClient.ListActivitiesAsync(new ListActivitiesRequest() { }).ConfigureAwait(false);
            foreach (var a in myActivities)
            {
                if (!activities.Activities.Any(o => o.Name == a))
                {
                    await sfClient.CreateActivityAsync(new CreateActivityRequest() { Name = a, Tags = new List<Tag> { new Tag { Key = "Casualty", Value = "test-jmw" } } }).ConfigureAwait(false);
                    _logger.LogInformation("Added activity {activity}", a);
                }
            }

        }

        private static WorkDemoActivityState ProcessTask(string activityTypeName, WorkDemoActivityState workDemoActivityState)
        {
            WorkDemo.WasteTime(
                int.Parse(activityTypeName.Last().ToString(), CultureInfo.InvariantCulture),
                workDemoActivityState);

            return workDemoActivityState;
        }

        private async Task CompleteTask(
            AmazonStepFunctionsClient amazonSimpleWorkflowClient,
            string taskToken, WorkDemoActivityState workDemoActivityState)
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
                _logger.LogError(ex, $"{workDemoActivityState} task complete failed: {ex}");
            }
        }

        private static async Task<GetActivityTaskResponse> Poll(AmazonStepFunctionsClient client, string arn)
        {
            var req = new GetActivityTaskRequest() { ActivityArn = arn };
            return await client.GetActivityTaskAsync(req).ConfigureAwait(false);
        }
    }
}

