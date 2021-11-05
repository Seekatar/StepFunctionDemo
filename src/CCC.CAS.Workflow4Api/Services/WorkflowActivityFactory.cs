using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Collections.Generic;
using CCC.CAS.Workflow4Api.Services;
using System.Reflection;
using System.Threading.Tasks;

namespace CCC.CAS.Workflow2Service.Services
{
#pragma warning disable CA1812
    class WorkflowActivityFactory : IWorkflowActivityFactory
    {
        private IWorkflowStateRepository _workflowStateRepository;
        private readonly ILogger<WorkflowActivityFactory> _logger;
        private Dictionary<string, (Type Type, ConstructorInfo Constructor)> _activityDict = new();
        static IWorkflowActivityFactory? _me;

        static public IWorkflowActivityFactory? Instance => _me;

        public WorkflowActivityFactory(IWorkflowStateRepository workflowStateRepository, ILogger<WorkflowActivityFactory> logger)
        {
            _workflowStateRepository = workflowStateRepository;
            _logger = logger;
            Load();
            _me = this;
        }

        public List<string> ActivityNames => _activityDict.Keys.ToList();

        private void Load()
        {
            Type workflowActivityType = typeof(WorkflowActivity<,>);

            var activityTypes = GetTypesInLoadedAssemblies(type => IsSubclassOfRawGeneric(workflowActivityType, type));
            foreach (var activityType in activityTypes)
            {
                var ok = false;
                if (activityType.GetCustomAttributes(typeof(WorkflowAttribute), false).FirstOrDefault() is WorkflowAttribute attr)
                {
                    var ctor = activityType.GetConstructors().FirstOrDefault();
                    var ctorParams = ctor?.GetParameters();
                    if (ctorParams != null &&
                        ctorParams.Length == 3 &&
                        ctorParams[0].ParameterType == typeof(IWorkflow) &&
                        ctorParams[1].ParameterType == typeof(string) &&
                        ctorParams[2].ParameterType == typeof(ILogger))
                    {
                        _activityDict[attr.Name] = (activityType, ctor!);
                        _logger.LogInformation("Loaded workflow activity {activityName}", attr.Name);
                        ok = true;
                    }

                    if (!ok)
                    {
                        _logger.LogInformation("Missing Constructor on class {activityClass}", activityType.Name);
                    }
                }
                else
                {
                    _logger.LogInformation("Missing WorkflowAttribute on class {activityClass}", activityType.Name);
                }
            }
        }

        public Task<IWorkflowActivity?> CreateActivity(string taskName, IWorkflow workflow, string taskToken)
        {
            IWorkflowActivity? ret = null;

            if (_activityDict.ContainsKey(taskName))
            {
                var ctor = _activityDict[taskName].Constructor;
                ret = ctor.Invoke(new object[] { workflow, taskToken, _logger }) as IWorkflowActivity;
            }
            return Task.FromResult(ret);
        }

        public async Task<IWorkflowActivity?> CreatePausedActivity(Type workflowActivityType, Guid correlationId, IWorkflow workflow)
        {
            var ctor = _activityDict.Values.Where(o => o.Type == workflowActivityType).SingleOrDefault().Constructor;

            if (ctor != null)
            {
                var token = await _workflowStateRepository!.RetrieveActivityState(workflowActivityType, correlationId).ConfigureAwait(false);
                if (token != null)
                {
                    return ctor.Invoke(new object[] { workflow, token, _logger }) as IWorkflowActivity;
                }
            }
            return null;
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

