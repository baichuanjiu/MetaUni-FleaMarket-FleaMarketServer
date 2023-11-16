using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace Market.API.Models.Profit
{
    public class Profit
    {
        public Profit(string? id, int user, double income, double expenditure)
        {
            Id = id;
            User = user;
            Income = income;
            Expenditure = expenditure;
        }

        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; } //MongoDB中存储的_Id
        public int User { get; set; } //User's UUID
        public double Income { get; set; }
        public double Expenditure { get; set; }
    }
}
