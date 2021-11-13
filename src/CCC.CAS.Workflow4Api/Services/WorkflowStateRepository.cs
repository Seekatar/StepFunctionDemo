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

    public async Task<string?> RetrieveActivityState(string activityTypeName, Guid correlationId)
    {
        if (activityTypeName == null) throw new ArgumentNullException(nameof(activityTypeName));

        var coll = _database.GetCollection<ActivityState>(_collectionName);

        var filter = Builders<ActivityState>.Filter
            .And(Builders<ActivityState>.Filter.Eq(ActivityState.activityFullName, activityTypeName),
                 Builders<ActivityState>.Filter.Eq(ActivityState.correlationId, correlationId));
        var query = coll.Find(filter);
        var ret = (await query.ToListAsync().ConfigureAwait(false)).SingleOrDefault()?.TaskToken;
        if (ret != null)
        {
            await coll.DeleteOneAsync(filter).ConfigureAwait(false);
        }
        return ret;
    }

    public async Task SaveActivityState(IWorkflowActivity activity, Guid correlationId)
    {
        if (activity == null) throw new ArgumentNullException(nameof(activity));
        Type t = activity!.GetType();
        if (t.FullName == null) throw new ArgumentNullException(nameof(activity));

        var coll = _database.GetCollection<ActivityState>(_collectionName);
        await coll.InsertOneAsync(new ActivityState { 
                    ActivityFullName = t!.FullName, 
                    CorrelationId = correlationId, 
                    TaskToken = activity.TaskToken,
                    CreationTime = DateTime.UtcNow }).ConfigureAwait(false);
    }
}
