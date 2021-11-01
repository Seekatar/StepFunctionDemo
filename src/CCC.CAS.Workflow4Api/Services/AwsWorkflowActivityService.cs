﻿using Amazon;
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
using CCC.CAS.API.Common.Installers;
using CCC.CAS.Workflow4Api.Services;

namespace CCC.CAS.Workflow2Service.Services
{
    public class AwsWorkflowActivityService : BackgroundService
    {
        private ILogger<AwsWorkflowActivityService> _logger;
        private readonly AwsWorkflowConfiguration _config;
        const string _arnBase = "arn:aws:states:us-east-1:620135122039:activity:";
        readonly string[] _myActivities = {  "Cas-Rbr-DemoActivity1",
                                    "Cas-Rbr-DemoActivity2",
                                    "Cas-Rbr-DemoActivity3",
                                    "Cas-Rbr-DemoActivity4",
                                    //"Cas-Rbr-Ppo1",
                                    "Cas-Rbr-Ppo2",
                                    "Cas-Rbr-Ppon",
                                    "Cas-Rbr-Ppo-Exit",
                                    "Cas-Rbr-DocMain",
        };

        public AwsWorkflowActivityService(IOptions<AwsWorkflowConfiguration> config, ILogger<AwsWorkflowActivityService> logger)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            _logger = logger;
            _config = config.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // This reads from C:\Users\<me>\.aws\credentials for the secret and 
            // the region is from environment (LaunchSettings.json)
            // using var sfClient = new AmazonStepFunctionsClient(); // RegionEndpoint.GetBySystemName(_config.Region)); //  _config.AccessKey, _config.SecretKey, RegionEndpoint.GetBySystemName(_config.Region));
            using var sfClient = new AmazonStepFunctionsClient(_config.AccessKey, _config.SecretKey, RegionEndpoint.GetBySystemName(_config.Region));

            await RegisterAsNeeded(sfClient).ConfigureAwait(false);

            var tasks = new List<Task>();

            foreach (var arn in _myActivities)
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
                try
                {
                    workDemoActivityState = JsonSerializer
                        .Deserialize<WorkDemoActivityState>(activityTask.Input);

                    _logger.LogInformation(">>> {task} fired with state {scenario}", taskName, workDemoActivityState?.ScenarioNumber ?? -1);

                    if (workDemoActivityState != null)
                    {
                        if (taskName == "Cas-Rbr-Ppo1")
                        {
                            var input = JsonSerializer.Deserialize<ActivityInputBase>(activityTask.Input);
                            if (input == null)
                            {
                                await CompleteTask(sfClient, activityTask.TaskToken, workDemoActivityState).ConfigureAwait(false);
                            }
                            else
                            {
                                await new Ppo1(sfClient, activityTask.TaskToken, _logger).Start(input).ConfigureAwait(false);
                            }
                        }
                        else
                        {
                            workDemoActivityState = ProcessTask(arn, workDemoActivityState);
                            if (taskName == "Cas-Rbr-DemoActivity1")
                            {
                                if (workDemoActivityState.ClientCode.Equals("USAA", StringComparison.OrdinalIgnoreCase))
                                {
                                    _logger.LogInformation(">>> {task} Completing....", taskName);
                                    await CompleteTask(sfClient, activityTask.TaskToken, workDemoActivityState).ConfigureAwait(false); ;
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
                                    await FailTask(sfClient, activityTask.TaskToken, workDemoActivityState).ConfigureAwait(false); ;
                                    errorOut = false;
                                }
                            }
                            else
                            {
                                _logger.LogInformation(">>> {task} Completing....", taskName);
                                await CompleteTask(sfClient, activityTask.TaskToken, workDemoActivityState).ConfigureAwait(false); ;
                                errorOut = false;
                            }
                            if (!errorOut && taskName == "Cas-Rbr-Ppo-Exit")
                            {
                                if (!string.IsNullOrEmpty(workDemoActivityState.TaskToken))
                                {
                                    _logger.LogInformation(">>> {task} Completing parent task from....", taskName);
                                    await CompleteTask(sfClient, workDemoActivityState.TaskToken, workDemoActivityState).ConfigureAwait(false);
                                }
                            }
                        }
                    }
                    else
                    {
                        _logger.LogError("Didn't get state for {task} with token {taskToken}", taskName, activityTask.TaskToken);
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Error processing task {task}", taskName);
                    if (errorOut)
                    {
                        await FailTask(sfClient, activityTask.TaskToken, workDemoActivityState ?? new WorkDemoActivityState()).ConfigureAwait(false); ;
                    }
                }
                Thread.Sleep(100);
            }
        }

        private async Task FailTask(AmazonStepFunctionsClient sfClient, string taskToken, WorkDemoActivityState workDemoActivityState)
        {
            var respondActivityTaskCompletedRequest =
                new SendTaskFailureRequest()
                {
                    Cause = "Because",
                    Error = "Ouch!",
                    TaskToken = taskToken
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

        private async Task RegisterAsNeeded(AmazonStepFunctionsClient sfClient)
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

            var activityType = typeof(AwsActivity<,>);

            var activityTypes = GetTypesInLoadedAssemblies(type => IsSubclassOfRawGeneric(activityType, type));
            var ctors = activityType.GetConstructors();
            var ctor = ctors.FirstOrDefault();
            ctor.Invoke();

            return;

        }

        public static List<Type> GetTypesInLoadedAssemblies(Predicate<Type> predicate, string assemblyPrefix = "CCC.")
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                                .Where(o => o.GetName().Name?.StartsWith(assemblyPrefix, StringComparison.OrdinalIgnoreCase) ?? false)
                                .SelectMany(s => s.GetTypes())
                                .Where(x => predicate(x))
                                .ToList();
        }


        // modified from this link to add if check to only do concrete classes and not itself
        // https://stackoverflow.com/questions/457676/check-if-a-class-is-derived-from-a-generic-class
        static bool IsSubclassOfRawGeneric(Type generic, Type? toCheck)
        {
            if (toCheck != null && generic != toCheck && !toCheck.IsInterface && !toCheck.IsAbstract)
            {
                while (toCheck != null && toCheck != typeof(object))
                {
                    var cur = toCheck.IsGenericType ? toCheck.GetGenericTypeDefinition() : toCheck;
                    if (generic == cur)
                    {
                        return true;
                    }
                    toCheck = toCheck.BaseType;
                }
            }
            return false;
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

