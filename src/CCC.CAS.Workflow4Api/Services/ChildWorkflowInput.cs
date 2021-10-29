using System.Text.Json.Serialization;

namespace CCC.CAS.Workflow2Service.Services
{
    public class ChildWorkflowInput : ActivityInputBase
    {
        [JsonPropertyName("taskToken")]
        public string TaskToken { get; set; } = "";
    }
}

