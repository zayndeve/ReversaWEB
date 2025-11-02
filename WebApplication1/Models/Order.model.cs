using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using WebApplication1.Enums;

namespace WebApplication1.Models
{
    public class ShippingAddress
    {
        [BsonElement("fullName")]
        public string FullName { get; set; } = null!;

        [BsonElement("phone")]
        public string Phone { get; set; } = null!;

        [BsonElement("address")]
        public string Address { get; set; } = null!;

        [BsonElement("city")]
        public string City { get; set; } = null!;

        [BsonElement("postalCode")]
        public string PostalCode { get; set; } = null!;

        [BsonElement("country")]
        public string Country { get; set; } = null!;
    }

    public class PreviewItem
    {
        [BsonElement("name")]
        public string? Name { get; set; }

        [BsonElement("image")]
        public string? Image { get; set; }
    }

    public class Order
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        [BsonElement("_id")]
        public string Id { get; set; } = null!;

        [BsonRepresentation(BsonType.ObjectId)]
        [BsonElement("memberId")]
        public string MemberId { get; set; } = null!;

        [BsonElement("totalAmount")]
        public double TotalAmount { get; set; }

        [BsonRepresentation(BsonType.String)]
        [BsonElement("paymentMethod")]
        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.CARD;

        [BsonRepresentation(BsonType.String)]
        [BsonElement("orderStatus")]
        public OrderStatus OrderStatus { get; set; } = OrderStatus.PENDING;

        [BsonElement("shippingAddress")]
        public ShippingAddress ShippingAddress { get; set; } = new();

        [BsonElement("previewItem")]
        public PreviewItem PreviewItem { get; set; } = new();

        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [BsonElement("updatedAt")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
