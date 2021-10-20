using Amazon;
using Amazon.StepFunctions;
using CCC.CAS.API.Common.Storage;
using CCC.CAS.Workflow4Api.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;
using CCC.CAS.Workflow2Service.Services;

namespace CCC.CAS.Workflow4Api.Services
{
    public class WorkflowService : IWorkflowService
    {
        private readonly ILogger<WorkflowService> _logger;
        private readonly AwsWorkflowConfiguration _config;
        string[] _arns = { "arn:aws:states:us-east-1:620135122039:stateMachine:test-jmw", "arn:aws:states:us-east-1:620135122039:stateMachine:MyStateMachine" };


        public WorkflowService(IOptions<AwsWorkflowConfiguration> config, ILogger<WorkflowService> logger)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            _logger = logger;
            _config = config.Value;
        }

        public async Task RestartWorkflow(string taskToken)
        {
            using var sfClient = new AmazonStepFunctionsClient(_config.AccessKey, _config.SecretKey, RegionEndpoint.GetBySystemName(_config.Region));
            var result = await sfClient.SendTaskSuccessAsync(new Amazon.StepFunctions.Model.SendTaskSuccessRequest
            {
                TaskToken = taskToken,
                Output = "{\"i\":1}"
            }).ConfigureAwait(false);
        }

        public async Task StartWorkflow(int scenario, string clientCode)
        {
            if (scenario >= 0 && scenario < _arns.Length)
            {
                using var sfClient = new AmazonStepFunctionsClient(_config.AccessKey, _config.SecretKey, RegionEndpoint.GetBySystemName(_config.Region));
                var result = await sfClient.StartExecutionAsync(new Amazon.StepFunctions.Model.StartExecutionRequest()
                {
                    StateMachineArn = _arns[scenario],
                    Input = JsonSerializer.Serialize(new WorkDemoActivityState() { ClientCode = clientCode, ScenarioNumber = scenario }),
                    Name = Guid.NewGuid().ToString()
                }).ConfigureAwait(false);
            }
            else
            {
                _logger.LogError("Bad scenario value of {scenario}", scenario);
            }

            return;

        }
    }
}
