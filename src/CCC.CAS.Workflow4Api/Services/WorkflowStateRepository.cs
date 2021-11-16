using CCC.CAS.API.Common.Mongo;
using CCC.CAS.Workflow2Service.Services;
using CCC.CAS.Workflow4Api.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using Polly;
using System;
using System.Linq;
using System.Threading.Tasks;

public class ActivityStateNotFoundException : Exception
{
    public ActivityStateNotFoundException() { }
    public ActivityStateNotFoundException(string msg) : base(msg) { }
    public ActivityStateNotFoundException(string msg, Exception innerException) : base(msg, innerException) { }
}

public class WorkflowStateRepository : IWorkflowStateRepository
{
    private readonly IMongoDatabase _database;
    private readonly IMongoClient _client;
    private readonly ILogger<WorkflowStateRepository> _logger;
    private const string _collectionName = "workflowState";

    [BsonIgnoreExtraElements]
    class ActivityState
    {
        public const string activityFullName = "activityFullName";
        public const string correlationId = "correlationId";

        [BsonElement(activityFullName)]
        public string ActivityFullName { get; set; } = "";
        [BsonElement(correlationId)]
        public Guid CorrelationId { get; set; }
        [BsonElement("handle")]
        public WorkflowActivityHandle Handle { get; set; } = new WorkflowActivityHandle();
        [BsonElement("creationTime")]
        public DateTime CreationTime { get; set; }
    }

    public WorkflowStateRepository(IMongoClient client, ILogger<WorkflowStateRepository> logger, IOptions<MongoConfig> mongoConfig)
    {
        _client = client;
        _logger = logger;

        var dbName = mongoConfig?.Value.Database ?? throw new ArgumentNullException(nameof(mongoConfig));
        _database = _client.GetDatabase(dbName);
    }

    public Task<WorkflowActivityHandle?> RetrieveActivityState(string activityTypeName, Guid correlationId)
    {
        // race condition if the activity Start returns as Incomplete, but immediately then calls Complete
        // but the Save hasn't completed yet.
        var _retries = 3;
        var _retryDelayMs = 200;
        return Policy
                .Handle<ActivityStateNotFoundException>()
                // .Or<ArgumentException>(ex => ex.ParamName == "example")
                .WaitAndRetry(_retries, retryAttempt => TimeSpan.FromMilliseconds(_retryDelayMs))
                .Execute(async () =>
                {
                    if (activityTypeName == null) throw new ArgumentNullException(nameof(activityTypeName));

                    var coll = _database.GetCollection<ActivityState>(_collectionName);

                    var filter = Builders<ActivityState>.Filter
                        .And(Builders<ActivityState>.Filter.Eq(ActivityState.activityFullName, activityTypeName),
                             Builders<ActivityState>.Filter.Eq(ActivityState.correlationId, correlationId));
                    var query = coll.Find(filter);
                    WorkflowActivityHandle? ret = (await query.ToListAsync().ConfigureAwait(false)).SingleOrDefault()?.Handle;

                    if (ret == null) throw new ActivityStateNotFoundException();
                    
                    if (ret != null)
                    {
                        await coll.DeleteOneAsync(filter).ConfigureAwait(false);
                        return ret;
                    }
                    return null;
                });
    }

    public async Task SaveActivityState(IWorkflowActivity activity, Guid correlationId)
    {
        if (activity == null) throw new ArgumentNullException(nameof(activity));
        if (!activity.Handle.IsValid) throw new ArgumentException($"Invalid handle for activity with correlationId {correlationId}");

        Type t = activity!.GetType();
        if (t.FullName == null) throw new ArgumentNullException(nameof(activity));

        var coll = _database.GetCollection<ActivityState>(_collectionName);
        await coll.InsertOneAsync(new ActivityState
        {
            ActivityFullName = t!.FullName,
            CorrelationId = correlationId,
            Handle = activity.Handle,
            CreationTime = DateTime.UtcNow
        }).ConfigureAwait(false);
    }
}
