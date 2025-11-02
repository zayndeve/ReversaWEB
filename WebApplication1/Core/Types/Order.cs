using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using WebApplication1.Enums;

namespace WebApplication1.Types
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

    public class OrderItem
    {
        [BsonRepresentation(BsonType.ObjectId)]
        [BsonElement("productId")]
        public string ProductId { get; set; } = null!;

        [BsonElement("itemPrice")]
        public double ItemPrice { get; set; }

        [BsonElement("itemQuantity")]
        public int ItemQuantity { get; set; }

        [BsonElement("productName")]
        public string ProductName { get; set; } = null!;

        [BsonElement("productImage")]
        public string ProductImage { get; set; } = null!;
    }

    public class Order
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = null!;

        [BsonRepresentation(BsonType.ObjectId)]
        [BsonElement("memberId")]
        public string MemberId { get; set; } = null!;

        [BsonElement("orderItems")]
        public List<OrderItem> OrderItems { get; set; } = new();

        [BsonElement("shippingAddress")]
        public ShippingAddress ShippingAddress { get; set; } = new();

        [BsonRepresentation(BsonType.String)]
        [BsonElement("paymentMethod")]
        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.CARD;

        [BsonRepresentation(BsonType.String)]
        [BsonElement("status")]
        public OrderStatus Status { get; set; } = OrderStatus.PENDING;

        [BsonElement("orderTotal")]
        public double OrderTotal { get; set; }

        [BsonElement("orderDelivery")]
        public double OrderDelivery { get; set; }

        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [BsonElement("updatedAt")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    public class PaymentRequest
    {
        public double TotalAmount { get; set; }
    }

    public class OrderInput
    {
        public List<OrderItemInput> OrderItems { get; set; } = new();
        public ShippingAddress ShippingAddress { get; set; } = new();
        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.CARD;
    }

    public class OrderItemInput
    {
        public string ProductId { get; set; } = null!;
        public double ItemPrice { get; set; }
        public int ItemQuantity { get; set; }
        public string ProductName { get; set; } = null!;
        public string ProductImage { get; set; } = null!;
    }
}
