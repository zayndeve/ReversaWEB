using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace WebApplication1.Models
{
    public class OrderItem
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        [BsonElement("_id")]
        public string Id { get; set; } = null!;

        [BsonRepresentation(BsonType.ObjectId)]
        [BsonElement("orderId")]
        public string OrderId { get; set; } = null!;

        [BsonRepresentation(BsonType.ObjectId)]
        [BsonElement("productId")]
        public string ProductId { get; set; } = null!;

        [BsonElement("itemPrice")]
        public double ItemPrice { get; set; }

        [BsonElement("itemQuantity")]
        public int ItemQuantity { get; set; }

        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [BsonElement("updatedAt")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
