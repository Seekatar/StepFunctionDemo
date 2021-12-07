using MongoDB.Bson.Serialization.Attributes;
using System.Threading.Tasks;

namespace CCC.CAS.Workflow2Service.Services
{
    public record WorkflowActivityHandle {
        public WorkflowActivityHandle() {}
        public WorkflowActivityHandle(string handle) { Handle = handle; }
        [BsonElement("handle")]
        public string Handle { get; set; } = "";
        public bool IsValid => !string.IsNullOrWhiteSpace(Handle);
    }

    public interface IWorkflowActivity
    {
        Task Start(string input, WorkflowActivityHandle handle);

        Task Complete(object? output);

        Task Fail(WorkflowError workflowError);

        WorkflowActivityHandle Handle { get; set;  }
    }
}

