using Amazon.StepFunctions;
using CCC.CAS.Workflow2Service.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CCC.CAS.Workflow4Api.Services
{
    class PpoOutput
    {

    }

    class Ppo1 : AwsActivity<ActivityInputBase, PpoOutput>
    {
        public Ppo1(AmazonStepFunctionsClient sfClient, string taskToken, ILogger logger) : base(sfClient, taskToken, logger)
        {

        }

        public override Task Start(ActivityInputBase input)
        {
            // save off task
            // fake out long running task
            return Task.Run(async () =>
            {
                await Task.Delay(10000).ConfigureAwait(false);
                Logger.LogInformation("Ppo1 completed!");
                var _ = Complete(new PpoOutput());
            });
        }
    }
}
