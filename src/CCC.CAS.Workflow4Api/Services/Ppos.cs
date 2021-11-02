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

    class TestPpo : WorkflowActivity<ActivityInputBase, PpoOutput>
    {
        protected TestPpo(IWorkflow workflow, string taskToken, ILogger logger) : base(workflow, taskToken, logger)
        {
        }

        public override Task Start(ActivityInputBase? input)
        {
            // fake out long running task
            return Task.Run(async () =>
            {
                await Task.Delay(5000).ConfigureAwait(false);
                Logger.LogInformation("{activityName} completed!", this.GetType().Name);
                var _ = Complete(new PpoOutput());
            });
        }

    }

#pragma warning disable CA1812 // never instantiated
    [Workflow(Name = "Cas-Rbr-Ppo1")]
    class Ppo1 : TestPpo
    {
        public Ppo1(IWorkflow workflow, string taskToken, ILogger logger) : base(workflow, taskToken, logger)
        {
        }
    }

    [Workflow(Name = "Cas-Rbr-Ppo2")]
    class Ppo2 : TestPpo
    {
        public Ppo2(IWorkflow workflow, string taskToken, ILogger logger) : base(workflow, taskToken, logger)
        {
        }
    }

    [Workflow(Name = "Cas-Rbr-Ppon")]
    class Ppon : TestPpo
    {
        public Ppon(IWorkflow workflow, string taskToken, ILogger logger) : base(workflow, taskToken, logger)
        {
        }
    }

    class PpoExitInput : ActivityInputBase
    {
        public string TaskToken { get; set; } = "";
    }

    [Workflow(Name = "Cas-Rbr-Ppo-Exit")]
    class PpoExit : WorkflowActivity<PpoExitInput, PpoOutput>
    {
        public PpoExit(IWorkflow workflow, string taskToken, ILogger logger) : base(workflow, taskToken, logger)
        {
        }

        public async override Task Start(PpoExitInput? input)
        {
            if (!string.IsNullOrEmpty(input?.TaskToken))
            {
                Logger.LogInformation(">>> {task} Completing parent task from....", nameof(PpoExit));
                await Complete(input.TaskToken, new PpoOutput()).ConfigureAwait(false);
                await Complete(new PpoOutput()).ConfigureAwait(false);
            } else
            {
                await Fail(new WorkflowError { Reason = WorkflowError.ReasonCode.Error, Message = "No tasktoken in exit" } ).ConfigureAwait(false);
            }
        }

    }
#pragma warning restore CA1812

}
