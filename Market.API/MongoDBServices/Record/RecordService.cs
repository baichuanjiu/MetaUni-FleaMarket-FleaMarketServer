using Market.API.DataCollection.Record;
using Market.API.ReusableClass;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Market.API.MongoDBServices.Record
{
    public class RecordService
    {
        private readonly IMongoCollection<Models.Record.Record> _recordCollection;
        private readonly IMongoCollection<BsonDocument> _bsonDocumentsCollection;

        public RecordService(IOptions<RecordCollectionSettings> recordCollectionSettings)
        {
            var mongoClient = new MongoClient(
                recordCollectionSettings.Value.ConnectionString);

            var mongoDatabase = mongoClient.GetDatabase(
                recordCollectionSettings.Value.DatabaseName);

            _recordCollection = mongoDatabase.GetCollection<Models.Record.Record>(
                recordCollectionSettings.Value.RecordCollectionName);

            _bsonDocumentsCollection = mongoDatabase.GetCollection<BsonDocument>(
                recordCollectionSettings.Value.RecordCollectionName);
        }

        public async Task CreateAsync(Models.Record.Record record)
        {
            await _recordCollection.InsertOneAsync(record);
        }

        public async Task<Models.Record.Record?> GetRecordByIdAsync(string id)
        {
            return await _recordCollection.Find(record => record.Id == id).FirstOrDefaultAsync();
        }

        public async Task DeleteAsync(string id)
        {
            var update = Builders<Models.Record.Record>.Update
                .Set(record => record.IsDeleted, true);

            await _recordCollection.UpdateOneAsync(record => record.Id == id, update);
        }

        public async Task<List<Models.Record.Record>> GetMyRecordsByLastResultAsync(string type, DateTime? lastDateTime, string? lastId, int UUID)
        {
            //MongoDB涉及到DateTime需要一律使用UTC进行操作（因为MongoDB默认采用UTC存储时间）
            if (lastDateTime == null)
            {
                lastDateTime = DateTime.UtcNow;
            }

            return await _recordCollection
                .Find(record => record.User == UUID && record.Type == type && record.CreatedTime.CompareTo(lastDateTime) <= 0 && !record.IsDeleted && record.Id != lastId)
                .SortByDescending(record => record.CreatedTime)
                .Limit(20)
                .ToListAsync();
        }

        public async Task<Models.Record.Record?> GetRecordNotCompletedAndNotDeletedByIdAsync(string id)
        {
            return await _recordCollection.Find(record => record.Id == id && !record.IsDeleted).FirstOrDefaultAsync();
        }
    }
}
