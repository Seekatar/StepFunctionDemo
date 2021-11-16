using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Collections.Generic;
using CCC.CAS.Workflow4Api.Services;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using CCC.CAS.API.Common.Logging;

namespace CCC.CAS.Workflow2Service.Services
{
#pragma warning disable CA1812
    class WorkflowActivityFactory : IWorkflowActivityFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private IWorkflowStateRepository _workflowStateRepository;
        private readonly ILogger<WorkflowActivityFactory> _logger;
        private static Dictionary<string, Type> _activityNameToType = new();
        static IWorkflowActivityFactory? _me;

        static public IWorkflowActivityFactory? Instance => _me;

        public WorkflowActivityFactory(IWorkflowStateRepository workflowStateRepository, ILogger<WorkflowActivityFactory> logger, IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _workflowStateRepository = workflowStateRepository;
            _logger = logger;
            _me = this;
        }

        public List<string> ActivityNames => _activityNameToType.Keys.ToList();

        public static void Register(IConfiguration _, IServiceCollection services, ILogger logger)
        {
            LoadActivityTypes(logger);
            foreach (var act in _activityNameToType)
            {
                services.AddTransient(act.Value);
            }
        }

        private static void LoadActivityTypes(ILogger logger)
        {
            lock (_activityNameToType)
            {
                if (_activityNameToType.Any())
                {
                    return;
                }

                Type workflowActivityType = typeof(WorkflowActivity<,>);

                var activityTypes = GetTypesInLoadedAssemblies(type => IsSubclassOfRawGeneric(workflowActivityType, type));
                foreach (var activityType in activityTypes)
                {
                    if (activityType.GetCustomAttributes(typeof(WorkflowAttribute), false).FirstOrDefault() is WorkflowAttribute attr)
                    {
                        _activityNameToType[attr.Name] = activityType;
                    }
                    else
                    {
                        logger.LogInformation("Missing WorkflowAttribute on class {activityClass}", activityType.Name);
                    }
                }
            }
        }

        public Task<IWorkflowActivity?> CreateActivity(string taskName, IWorkflow workflow, WorkflowActivityHandle handle)
        {
            IWorkflowActivity? ret = null;

            if (_activityNameToType.ContainsKey(taskName))
            {
                ret = _serviceProvider.GetRequiredService(_activityNameToType[taskName]) as IWorkflowActivity;
            }

            if (ret != null)
            {
                ret.Handle = handle;
            }
            else
            {
                _logger.LogError("{taskName} has no activity type", taskName);
            }
            return Task.FromResult(ret);
        }

        public async Task<IWorkflowActivity?> CreatePausedActivity(Type workflowActivityType, Guid correlationId)
        {
            if (workflowActivityType?.FullName == null) throw new ArgumentNullException(nameof(workflowActivityType));

            IWorkflowActivity? ret;

            ret = _serviceProvider.GetRequiredService(workflowActivityType) as IWorkflowActivity;
            var taskName = workflowActivityType.Name;

            if (ret != null)
            {
                var handle = await _workflowStateRepository!.RetrieveActivityState(workflowActivityType.FullName, correlationId).ConfigureAwait(false);
                if (handle != null)
                {
                    ret.Handle = handle;
                }
                else
                {
                    _logger.LogError(correlationId, "{taskName} has no activity type", taskName);
                }
            }
            else
            {
                _logger.LogError(correlationId, "{taskName} has no activity type", taskName);
            }
            return ret;
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

