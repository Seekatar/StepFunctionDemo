using CCC.CAS.Workflow2Service.Services;
using CCC.CAS.Workflow4Api.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Swashbuckle.AspNetCore.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace CCC.CAS.Workflow4Api.Controllers
{
#pragma warning disable CA1812
    [ApiController]
    public class Workflow : Controller
    {
        IWorkflowService _workflowService;
        private readonly ILogger<Workflow> _logger;

        public Workflow(IWorkflowService workflowService, ILogger<Workflow> logger)
        {
            _workflowService = workflowService;
            _logger = logger;
        }

        // POST: Workflow/Create
        [HttpPost]
        [Route("/api/workflow")]
        //[ValidateAntiForgeryToken]
        [SwaggerOperation("StartWorkflow")]
        [SwaggerResponse(statusCode: 201, type: typeof(string), description: "Demo")]
        public async Task<ActionResult> Create(WorkDemoActivityState state)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            try
            {
                await _workflowService.StartWorkflow(state.ScenarioNumber, state.ClientCode).ConfigureAwait(false);
                return Ok();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Controller caught error");
                return StatusCode((int)HttpStatusCode.InternalServerError);
            }
        }
    }
#pragma warning restore CA1812
}
