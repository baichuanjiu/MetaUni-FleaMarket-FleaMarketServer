using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using Market.API.ReusableClass;

namespace Market.API.Models.Record
{
    public class Record
    {
        public Record(string? id, string type, int user, string title, string description, List<MediaMetadata> medias, Dictionary<string, string> labels, string? campus, DateTime createdTime, double price, string remark, bool isDeleted)
        {
            Id = id;
            Type = type;
            User = user;
            Title = title;
            Description = description;
            Medias = medias;
            Labels = labels;
            Campus = campus;
            CreatedTime = createdTime;
            Price = price;
            Remark = remark;
            IsDeleted = isDeleted;
        }

        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; } //MongoDB中存储的_Id

        public string Type { get; set; } //sell("出售") purchase("购买")
        public int User { get; set; } //User's UUID
        public string Title { get; set; }
        public string Description { get; set; }
        public List<MediaMetadata> Medias { get; set; }
        public Dictionary<string, string> Labels { get; set; }
        public string? Campus { get; set; }
        public DateTime CreatedTime { get; set; }
        public double Price { get; set; }
        public string Remark { get; set; }
        public bool IsDeleted { get; set; }
    }
}
