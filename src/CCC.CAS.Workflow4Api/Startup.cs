using CCC.CAS.API.AspNetCommon;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CCC.CAS.Workflow4Api
{
    public class Startup : BaseStartup
    {
        public Startup(IWebHostEnvironment? env, IConfiguration configuration) : base(env, configuration, apiVersion: "1", title: "Workflow4Api")
        {
            JsonIgnoreNullValues = true;
        }

        protected override void AddMoreHealthChecks(IHealthChecksBuilder healthChecksBuilder)
        {
            // add other health checks here or delete this method
        }

    }
}
