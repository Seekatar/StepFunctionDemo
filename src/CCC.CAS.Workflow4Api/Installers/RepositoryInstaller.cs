using System;
using CCC.CAS.API.Common.Installers;
using CCC.CAS.API.Common.Logging;
using CCC.CAS.API.Common.Mongo;
using CCC.CAS.API.Common.Storage;
using CCC.CAS.Workflow2Service.Services;
using CCC.CAS.Workflow4Api.Interfaces;
using CCC.CAS.Workflow4Api.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CCC.CAS.Workflow4Api.Installers
{
    public class RepositoryInstaller : IInstaller
    {
        private readonly ILogger<RepositoryInstaller> _debugLogger;

        public RepositoryInstaller()
        {
            _debugLogger = DebuggingLoggerFactory.Create<RepositoryInstaller>();
        }

        public void InstallServices(IConfiguration configuration, IServiceCollection services)
        {
            if (configuration == null) { throw new ArgumentNullException(nameof(configuration)); }

            try
            {
                services.AddMongoClient(configuration);

                services.AddHostedService<AwsWorkflowActivityService>();

                services.AddTransient<IWorkflowStateRepository,WorkflowStateRepository>();

                var section = configuration.GetSection(AwsWorkflowConfiguration.DefaultConfigName);
                services.AddOptions<AwsWorkflowConfiguration>()
                         .Bind(section)
                         .ValidateDataAnnotations();

                services.AddSingleton<IWorkflowService, WorkflowService>();
                services.AddSingleton<IWorkflowActivityFactory, WorkflowActivityFactory>();

                _debugLogger.LogDebug("Services added.");
            }
            catch (Exception ex)
            {
                _debugLogger.LogError(ex, "Exception occurred while adding DB services.");
            }
        }
    }
}
