using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using WebApplication1.Enums;

namespace WebApplication1.Models
{
    public class View
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = null!;

        [BsonRepresentation(BsonType.String)]
        public ViewGroup ViewGroup { get; set; }

        [BsonRepresentation(BsonType.ObjectId)]
        [BsonElement("memberId")]
        public string MemberId { get; set; } = null!;

        [BsonRepresentation(BsonType.ObjectId)]
        [BsonElement("viewRefId")]
        public string ViewRefId { get; set; } = null!;

        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
