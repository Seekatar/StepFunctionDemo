using System;
using System.Text.Json.Serialization;

namespace CCC.CAS.Workflow2Service.Services
{
    public class ActivityInputBase
    {
        [JsonPropertyName("correlationId")]
        public Guid CorrelationId { get; set; }

        [JsonPropertyName("requestId")]
        public string RequestId { get; set; } = "";
        
        [JsonPropertyName("clientCode")]
        public string ClientCode { get; set; } = "";
        
        [JsonPropertyName("profileId")]
        public int ProfileId { get; set; }
        
        [JsonPropertyName("workflowStart")]
        public DateTimeOffset WorkflowStart { get; set; }
        
        [JsonPropertyName("stateStart")]
        public DateTimeOffset StateStart { get; set; }
    }
}

