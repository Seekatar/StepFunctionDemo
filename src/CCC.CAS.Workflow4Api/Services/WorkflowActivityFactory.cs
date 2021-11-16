using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Collections.Generic;
using CCC.CAS.Workflow4Api.Services;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CCC.CAS.Workflow2Service.Services
{
#pragma warning disable CA1812
    class WorkflowActivityFactory : IWorkflowActivityFactory
    {
        private IWorkflowStateRepository _workflowStateRepository;
        private readonly ILogger<WorkflowActivityFactory> _logger;
        private static Dictionary<string, (Type Type, IWorkflowActivity? Instance)> _activityDict = new();
        static IWorkflowActivityFactory? _me;

        static public IWorkflowActivityFactory? Instance => _me;

        public WorkflowActivityFactory(IWorkflowStateRepository workflowStateRepository, ILogger<WorkflowActivityFactory> logger, IEnumerable<IWorkflowActivity> activities)
        {
            _workflowStateRepository = workflowStateRepository;
            _logger = logger;
            foreach (var activity in activities)
            {
                var pair = _activityDict.Where(o => o.Value.Type == activity.GetType()).SingleOrDefault();
                if (pair.Value.Type != null)
                {
                    _activityDict[pair.Key] = (pair.Value.Type, activity);
                }
            }
            _me = this;
        }

        public List<string> ActivityNames => _activityDict.Keys.ToList();

        public static void Register(IConfiguration _, IServiceCollection services, ILogger logger)
        {
            Load(logger);
            foreach ( var act in _activityDict)
            {
                services.AddSingleton(typeof(IWorkflowActivity), act.Value.Type);
            }
        }

        private static void Load(ILogger logger)
        {
            lock (_activityDict)
            {
                if (_activityDict.Any())
                {
                    return;
                }

                Type workflowActivityType = typeof(WorkflowActivity<,>);

                var activityTypes = GetTypesInLoadedAssemblies(type => IsSubclassOfRawGeneric(workflowActivityType, type));
                foreach (var activityType in activityTypes)
                {
                    if (activityType.GetCustomAttributes(typeof(WorkflowAttribute), false).FirstOrDefault() is WorkflowAttribute attr)
                    {
                        _activityDict[attr.Name] = new (activityType, null);
                    }
                    else
                    {
                        logger.LogInformation("Missing WorkflowAttribute on class {activityClass}", activityType.Name);
                    }
                }
            }
        }

        public Task<IWorkflowActivity?> CreateActivity(string taskName, IWorkflow workflow, string taskToken)
        {
            IWorkflowActivity? ret = null;

            if (_activityDict.ContainsKey(taskName))
            {
                ret = _activityDict[taskName].Instance;
            }
            return Task.FromResult(ret);
        }

        public async Task<(string? Token, IWorkflowActivity? Activity)> CreatePausedActivity(Type workflowActivityType, Guid correlationId)
        {
            var value = _activityDict.Values.Where(o => o.Type == workflowActivityType).SingleOrDefault();

            if (value.Type?.FullName != null)
            {
                var token = await _workflowStateRepository!.RetrieveActivityState(value.Type.FullName, correlationId).ConfigureAwait(false);
                if (token != null)
                {
                    return (token,value.Instance);
                }
            }
            return (null,null);
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
    }
#pragma warning restore CA1812
}

