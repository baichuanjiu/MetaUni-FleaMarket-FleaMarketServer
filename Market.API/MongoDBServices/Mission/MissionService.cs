using Market.API.DataCollection.Mission;
using Market.API.ReusableClass;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Market.API.MongoDBServices.Mission
{
    public class MissionService
    {
        private readonly IMongoCollection<Models.Mission.Mission> _missionCollection;
        private readonly IMongoCollection<BsonDocument> _bsonDocumentsCollection;

        public MissionService(IOptions<MissionCollectionSettings> missionCollectionSettings)
        {
            var mongoClient = new MongoClient(
                missionCollectionSettings.Value.ConnectionString);

            var mongoDatabase = mongoClient.GetDatabase(
                missionCollectionSettings.Value.DatabaseName);

            _missionCollection = mongoDatabase.GetCollection<Models.Mission.Mission>(
                missionCollectionSettings.Value.MissionCollectionName);

            _bsonDocumentsCollection = mongoDatabase.GetCollection<BsonDocument>(
                missionCollectionSettings.Value.MissionCollectionName);
        }

        public async Task CreateAsync(Models.Mission.Mission mission)
        {
            await _missionCollection.InsertOneAsync(mission);
        }

        public async Task<Models.Mission.Mission?> GetMissionByIdAsync(string id)
        {
            return await _missionCollection.Find(mission => mission.Id == id).FirstOrDefaultAsync();
        }

        public async Task DeleteAsync(string id)
        {
            var update = Builders<Models.Mission.Mission>.Update
                .Set(mission => mission.IsDeleted, true);

            await _missionCollection.UpdateOneAsync(mission => mission.Id == id, update);
        }

        public async Task CompleteAsync(string id)
        {
            var update = Builders<Models.Mission.Mission>.Update
                .Set(mission => mission.IsCompleted, true);

            await _missionCollection.UpdateOneAsync(mission => mission.Id == id, update);
        }

        public async Task<List<Models.Mission.Mission>> GetMissionsByLastResultAsync(string type,DateTime? lastDateTime, string? lastId)
        {
            //MongoDB涉及到DateTime需要一律使用UTC进行操作（因为MongoDB默认采用UTC存储时间）
            if (lastDateTime == null)
            {
                lastDateTime = DateTime.UtcNow;
            }

            return await _missionCollection
                .Find(mission => mission.Type == type && mission.CreatedTime.CompareTo(lastDateTime) <= 0 && !mission.IsCompleted && !mission.IsDeleted && mission.Id != lastId)
                .SortByDescending(mission => mission.CreatedTime)
                .Limit(20)
                .ToListAsync();
        }

        public async Task<List<Models.Mission.Mission>> GetMyMissionsByLastResultAsync(string type, DateTime? lastDateTime, string? lastId,int UUID)
        {
            //MongoDB涉及到DateTime需要一律使用UTC进行操作（因为MongoDB默认采用UTC存储时间）
            if (lastDateTime == null)
            {
                lastDateTime = DateTime.UtcNow;
            }

            return await _missionCollection
                .Find(mission => mission.User == UUID && mission.Type == type && mission.CreatedTime.CompareTo(lastDateTime) <= 0 && !mission.IsCompleted && !mission.IsDeleted && mission.Id != lastId)
                .SortByDescending(mission => mission.CreatedTime)
                .Limit(20)
                .ToListAsync();
        }

        public async Task<Models.Mission.Mission?> GetMissionNotCompletedAndNotDeletedByIdAsync(string id) 
        {
            return await _missionCollection.Find(mission => mission.Id == id && !mission.IsCompleted && !mission.IsDeleted).FirstOrDefaultAsync();
        }

        public async Task<List<Models.Mission.Mission>> Search(List<string> searchKeys, string type, DateTime? lastDateTime, string? lastId)
        {
            //MongoDB涉及到DateTime需要一律使用UTC进行操作（因为MongoDB默认采用UTC存储时间）
            if (lastDateTime == null)
            {
                lastDateTime = DateTime.UtcNow;
            }

            var builder = Builders<Models.Mission.Mission>.Filter;

            FilterDefinition<Models.Mission.Mission> typeFilter = builder.Where(mission => mission.Type == type);

            FilterDefinition<Models.Mission.Mission> notCompletedAndNotDeletedFilter = builder.Where(mission => !mission.IsCompleted && !mission.IsDeleted);

            FilterDefinition<Models.Mission.Mission> offsetFilter = builder.Where(mission => mission.CreatedTime.CompareTo(lastDateTime) <= 0 && mission.Id != lastId);

            FilterDefinition<Models.Mission.Mission> keyFilter;
            string firstKey = searchKeys[0];
            keyFilter = builder.Regex(mission => mission.Title, firstKey);
            keyFilter |= builder.Regex(mission => mission.Description, firstKey);
            searchKeys.RemoveAt(0);
            foreach (var key in searchKeys)
            {
                keyFilter = builder.Regex(mission => mission.Title, key);
                keyFilter |= builder.Regex(mission => mission.Description, key);
            }


            return await _missionCollection
                .Find(typeFilter & notCompletedAndNotDeletedFilter & offsetFilter & keyFilter)
                .SortByDescending(mission => mission.CreatedTime)
                .Limit(20)
                .ToListAsync();
        }

        public async Task<List<Models.Mission.Mission>> Search(ChannelData channel, string type, DateTime? lastDateTime, string? lastId)
        {
            //MongoDB涉及到DateTime需要一律使用UTC进行操作（因为MongoDB默认采用UTC存储时间）
            if (lastDateTime == null)
            {
                lastDateTime = DateTime.UtcNow;
            }

            var builder = Builders<Models.Mission.Mission>.Filter;

            FilterDefinition<Models.Mission.Mission> typeFilter = builder.Where(mission => mission.Type == type);

            FilterDefinition<Models.Mission.Mission> notCompletedAndNotDeletedFilter = builder.Where(mission => !mission.IsCompleted && !mission.IsDeleted);

            FilterDefinition<Models.Mission.Mission> offsetFilter = builder.Where(mission => mission.CreatedTime.CompareTo(lastDateTime) <= 0 && mission.Id != lastId);

            FilterDefinition<Models.Mission.Mission> channelFilter;
            if (channel.SubChannel == null) 
            {
                channelFilter = builder.Where(mission => mission.ChannelData.MainChannel == channel.MainChannel);
            }
            else
            {
                channelFilter = builder.Where(mission => mission.ChannelData.MainChannel == channel.MainChannel && mission.ChannelData.SubChannel == channel.SubChannel);
            }

            return await _missionCollection
                .Find(typeFilter & notCompletedAndNotDeletedFilter & offsetFilter & channelFilter)
                .SortByDescending(mission => mission.CreatedTime)
                .Limit(20)
                .ToListAsync();
        }


    }
}
