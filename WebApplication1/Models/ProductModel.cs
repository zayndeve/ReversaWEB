using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using WebApplication1.Enums;

namespace WebApplication1.Models
{
    public class Product
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = null!;

        [BsonRepresentation(BsonType.String)]
        public ProductStatus ProductStatus { get; set; } = ProductStatus.PAUSE;

        [BsonRepresentation(BsonType.String)]
        public ProductCategory ProductCategory { get; set; }

        [BsonElement("productName")]
        public string ProductName { get; set; } = null!;

        [BsonRepresentation(BsonType.String)]
        public ProductGender ProductGender { get; set; }

        [BsonDefaultValue(0.0)]
        public double? ProductPrice { get; set; }

        public int ProductLeftCount { get; set; }

        [BsonRepresentation(BsonType.String)]
        public ProductSize ProductSize { get; set; } = ProductSize.M;

        public string? ProductDesc { get; set; }

        public List<string> ProductImages { get; set; } = new();

        public int ProductViews { get; set; } = 0;

        [BsonRepresentation(BsonType.String)]
        public List<ProductTag> ProductTags { get; set; } = new() { ProductTag.HOT };

        public double ProductRating { get; set; } = 4;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    // === Index configuration === //
    public static class ProductCollectionConfig
    {
        public static void ConfigureIndexes(IMongoCollection<Product> collection)
        {
            var indexKeys = Builders<Product>.IndexKeys
                .Ascending(p => p.ProductName)
                .Ascending(p => p.ProductSize)
                .Ascending(p => p.ProductGender);

            var indexOptions = new CreateIndexOptions { Unique = true };

            var indexModel = new CreateIndexModel<Product>(indexKeys, indexOptions);
            collection.Indexes.CreateOne(indexModel);
        }
    }
}
