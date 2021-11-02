using CCC.CAS.API.Common.Mongo;
using CCC.CAS.Workflow2Service.Services;
using CCC.CAS.Workflow4Api.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using System;
using System.Linq;
using System.Threading.Tasks;

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
        [BsonElement("taskToken ")]
        public string TaskToken { get; set; } = "";
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

    public async Task<string?> RetrieveActivityState(Type activityType, Guid correlationId)
    {
        var coll = _database.GetCollection<ActivityState>(_collectionName);

        var filter = Builders<ActivityState>.Filter
            .And(Builders<ActivityState>.Filter.Eq(ActivityState.activityFullName, activityType?.FullName ?? "<bug!>"),
                 Builders<ActivityState>.Filter.Eq(ActivityState.correlationId, correlationId));
        var query = coll.Find(filter);
        return (await query.ToListAsync().ConfigureAwait(false)).SingleOrDefault()?.TaskToken;
    }

    public async Task SaveActivityState(IWorkflowActivity activity, Guid correlationId)
    {
        if (activity?.GetType().FullName == null) throw new ArgumentNullException(nameof(activity));

        var coll = _database.GetCollection<ActivityState>(_collectionName);
        await coll.InsertOneAsync(new ActivityState { 
                    ActivityFullName = activity.GetType().FullName ?? "<bug!>", 
                    CorrelationId = correlationId, 
                    TaskToken = activity.TaskToken,
                    CreationTime = DateTime.UtcNow }).ConfigureAwait(false);
    }
}
