using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using WebApplication1.Enums;
using WebApplication1.Models; // Required for the ToProduct() mapper

namespace WebApplication1.Types
{
    // === Product Inquiry (used for filtering & pagination) === //
    public class ProductInquiry
    {
        public string Order { get; set; } = "createdAt";
        public int Page { get; set; } = 1;
        public int Limit { get; set; } = 10;

        [BsonRepresentation(BsonType.String)]
        public ProductCategory? ProductCategory { get; set; }

        public string? Search { get; set; }
        public List<string>? Category { get; set; }
        public List<string>? Size { get; set; }
        public List<string>? Tag { get; set; }
    }

    // === Product Input (used for creating new products) === //
    public class ProductInput
    {
        [BsonRepresentation(BsonType.String)]
        public ProductStatus? ProductStatus { get; set; }

        [BsonRepresentation(BsonType.String)]
        public ProductCategory ProductCategory { get; set; }

        public string ProductName { get; set; } = null!;

        [BsonRepresentation(BsonType.String)]
        public ProductGender ProductGender { get; set; }

        public double ProductPrice { get; set; }

        public int ProductLeftCount { get; set; }

        [BsonRepresentation(BsonType.String)]
        public ProductSize? ProductSize { get; set; }

        public string? ProductDesc { get; set; }

        public List<string>? ProductImages { get; set; }

        public int? ProductViews { get; set; }

        [BsonRepresentation(BsonType.String)]
        public List<ProductTag>? ProductTags { get; set; }

        public double? ProductRating { get; set; }

        // === Helper: Convert DTO to Product model === //
        public Product ToProduct()
        {
            return new Product
            {
                ProductName = this.ProductName,
                ProductPrice = this.ProductPrice,
                ProductDesc = this.ProductDesc,
                ProductCategory = this.ProductCategory,
                ProductGender = this.ProductGender,
                ProductSize = this.ProductSize ?? WebApplication1.Enums.ProductSize.M,
                ProductImages = this.ProductImages ?? new List<string>(),
                ProductTags = this.ProductTags ?? new List<ProductTag>(),
                ProductLeftCount = this.ProductLeftCount,
                ProductStatus = this.ProductStatus ?? WebApplication1.Enums.ProductStatus.PROCESS,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }
    }

    // === Product Update Input (used for updating or deleting products) === //
    public class ProductUpdateInput
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = null!;

        public string? ProductStatus { get; set; }

        [BsonRepresentation(BsonType.String)]
        public ProductCategory ProductCategory { get; set; }

        public string? ProductName { get; set; }

        [BsonRepresentation(BsonType.String)]
        public ProductGender ProductGender { get; set; }

        public double? ProductPrice { get; set; }

        public int? ProductLeftCount { get; set; }

        [BsonRepresentation(BsonType.String)]
        public ProductSize? ProductSize { get; set; }

        public double? ProductVolume { get; set; }

        public string? ProductDesc { get; set; }

        public List<string>? ProductImages { get; set; }

        public int? ProductViews { get; set; }

        [BsonRepresentation(BsonType.String)]
        public List<ProductTag>? ProductTags { get; set; }

        public double? ProductRating { get; set; }
    }
}
