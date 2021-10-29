namespace CCC.CAS.Workflow2Service.Services
{
    public class WorkflowError
    {
        public enum ReasonCode
        {
            Error,
            Timeout
        }

        public ReasonCode Reason { get; set; }
        public string Message { get; set; } = "";
    }
}

