using MongoDB.Bson.Serialization.Attributes;

namespace WebApplication1.Enums
{
    public enum OrderStatus
    {
        [BsonRepresentation(MongoDB.Bson.BsonType.String)]
        PENDING,     // Order created, not paid
        [BsonRepresentation(MongoDB.Bson.BsonType.String)]
        PAID,        // Payment completed
        [BsonRepresentation(MongoDB.Bson.BsonType.String)]
        SHIPPED,     // Order shipped
        [BsonRepresentation(MongoDB.Bson.BsonType.String)]
        DELIVERED,   // Customer received
        [BsonRepresentation(MongoDB.Bson.BsonType.String)]
        CANCELED     // Manually canceled
    }

    public enum PaymentMethod
    {
        [BsonRepresentation(MongoDB.Bson.BsonType.String)]
        CARD,
        [BsonRepresentation(MongoDB.Bson.BsonType.String)]
        PAYPAL,
        [BsonRepresentation(MongoDB.Bson.BsonType.String)]
        BANK_TRANSFER,
        [BsonRepresentation(MongoDB.Bson.BsonType.String)]
        CASH_ON_DELIVERY
    }
}
