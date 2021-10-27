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
        string[] _stateMachineArns = { "arn:aws:states:us-east-1:620135122039:stateMachine:Cas-Rbr-Test-ErrorHandling",
                                       "arn:aws:states:us-east-1:620135122039:stateMachine:Cas-Rbr-Recommendations" };


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

        public async Task StartDemoWorkflow(WorkDemoActivityState state)
        {
            await StartWorkflow(_stateMachineArns[0], state).ConfigureAwait(false);
        }

        public async Task StartDocWorkflow(WorkDemoActivityState state)
        {
            await StartWorkflow(_stateMachineArns[1], state).ConfigureAwait(false);
        }

        async Task StartWorkflow(string arn, WorkDemoActivityState state)
        { 
            using var sfClient = new AmazonStepFunctionsClient(_config.AccessKey, _config.SecretKey, RegionEndpoint.GetBySystemName(_config.Region));
            var result = await sfClient.StartExecutionAsync(new Amazon.StepFunctions.Model.StartExecutionRequest()
            {
                StateMachineArn = arn,
                Input = JsonSerializer.Serialize(state),
                Name = Guid.NewGuid().ToString()
            }).ConfigureAwait(false);

            return;

        }
    }
}
