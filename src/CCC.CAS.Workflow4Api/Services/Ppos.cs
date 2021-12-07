using Amazon.StepFunctions;
using CCC.CAS.Workflow2Service.Services;
using Microsoft.Extensions.Logging;
using Seekatar.Interfaces;
using Seekatar.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CCC.CAS.Workflow4Api.Services
{
    class PpoOutput
    {
        public bool Applied { get; set; }
    }

    class TestPpoBase : WorkflowActivity<ActivityInputBase, PpoOutput>
    {
        protected TestPpoBase(IWorkflow workflow, ILogger logger) : base(workflow, logger)
        {
        }

        public bool Applied { get; set; }

        // ugly static just for testing async workflow activity
        public static IObjectFactory<IWorkflowActivity>? WorkflowActivityFactory { get; set; }

        public override Task Start(ActivityInputBase? input)
        {
            // fake out long running task
            Task.Run(async () =>
            {
                await Task.Delay(5000).ConfigureAwait(false);
                Logger.LogInformation("{activityName} completed!", this.GetType().Name);
                if (WorkflowActivityFactory != null && input != null)
                {
                    var activity = await Workflow.CreatePausedActivity(this.GetType(), input.CorrelationId).ConfigureAwait(false);
                    if (activity != null)
                    {
                        await activity.Complete(new PpoOutput() { Applied = Applied }).ConfigureAwait(false);
                    }
                }
            });

            return Task.CompletedTask;
        }

    }

#pragma warning disable CA1812 // never instantiated
    [ObjectName(Name = "Cas-Rbr-Ppo1")]
    class Ppo1 : TestPpoBase
    {
        public Ppo1(IWorkflow workflow, ILogger<Ppo1> logger) : base(workflow, logger)
        {
        }
    }

    [ObjectName(Name = "Cas-Rbr-Ppo2")]
    class Ppo2 : TestPpoBase
    {
        public Ppo2(IWorkflow workflow, ILogger<Ppo2> logger) : base(workflow, logger)
        {
            Applied = true;
        }
    }

    [ObjectName(Name = "Cas-Rbr-Ppon")]
    class Ppon : TestPpoBase
    {
        public Ppon(IWorkflow workflow, ILogger<Ppon> logger) : base(workflow, logger)
        {
        }
    }

    class PpoExitInput : ActivityInputBase
    {
        public string TaskToken { get; set; } = "";
    }

    [ObjectName(Name = "Cas-Rbr-Ppo-Exit")]
    class PpoExit : WorkflowActivity<PpoExitInput, PpoOutput>
    {
        public PpoExit(IWorkflow workflow, ILogger<PpoExit> logger) : base(workflow, logger)
        {
        }

        public async override Task Start(PpoExitInput? input)
        {
            if (!string.IsNullOrEmpty(input?.TaskToken))
            {
                Logger.LogInformation(">>> {task} Completing parent task from....", nameof(PpoExit));
                await Complete(new WorkflowActivityHandle(input.TaskToken), new PpoOutput()).ConfigureAwait(false);
                await Complete(new PpoOutput()).ConfigureAwait(false);
            }
            else
            {
                await Fail(new WorkflowError { Reason = WorkflowError.ReasonCode.Error, Message = "No tasktoken in exit" }).ConfigureAwait(false);
            }
        }

    }
#pragma warning restore CA1812

}
