using Market.API.DataCollection.Profit;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Market.API.MongoDBServices.Profit
{
    public class ProfitService
    {
        private readonly IMongoCollection<Models.Profit.Profit> _profitCollection;
        private readonly IMongoCollection<BsonDocument> _bsonDocumentsCollection;

        public ProfitService(IOptions<ProfitCollectionSettings> profitCollectionSettings)
        {
            var mongoClient = new MongoClient(
                profitCollectionSettings.Value.ConnectionString);

            var mongoDatabase = mongoClient.GetDatabase(
                profitCollectionSettings.Value.DatabaseName);

            _profitCollection = mongoDatabase.GetCollection<Models.Profit.Profit>(
                profitCollectionSettings.Value.ProfitCollectionName);

            _bsonDocumentsCollection = mongoDatabase.GetCollection<BsonDocument>(
                profitCollectionSettings.Value.ProfitCollectionName);
        }

        public async Task CreateAsync(Models.Profit.Profit profit)
        {
            await _profitCollection.InsertOneAsync(profit);
        }

        public async Task<Models.Profit.Profit?> GetMyProfitAsync(int UUID)
        {
            return await _profitCollection.Find(profit => profit.User == UUID).FirstOrDefaultAsync();
        }

        public async Task<double> ComputeMyIncomeAsync(double income,int UUID) 
        {
            var profit = await _profitCollection.Find(profit => profit.User == UUID).FirstOrDefaultAsync();

            double result;
            if (profit == null)
            {
                await CreateAsync(new(null, UUID, 0, 0));
                result = income;
            }
            else 
            {
                result = profit.Income + income;
            }

            var update = Builders<Models.Profit.Profit>.Update
                .Set(profit => profit.Income,result);

            _ = _profitCollection.UpdateOneAsync(profit => profit.User == UUID, update);
            return result;
        }

        public async Task<double> ComputeMyExpenditureAsync(double expenditure, int UUID)
        {
            var profit = await _profitCollection.Find(profit => profit.User == UUID).FirstOrDefaultAsync();

            double result;
            if (profit == null)
            {
                await CreateAsync(new(null, UUID, 0, 0));
                result = expenditure;
            }
            else
            {
                result = profit.Expenditure + expenditure;
            }

            var update = Builders<Models.Profit.Profit>.Update
                .Set(profit => profit.Expenditure, result);

            _ = _profitCollection.UpdateOneAsync(profit => profit.User == UUID, update);
            return result;
        }
    }
}
