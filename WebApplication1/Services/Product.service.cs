using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Driver;
using WebApplication1.Models;
using WebApplication1.Enums;
using WebApplication1.Types;

namespace WebApplication1.Services
{
    public class ProductService
    {
        private readonly IMongoCollection<Product> _productCollection;
        private readonly ViewService _viewService;

        public ProductService(MongoDBService mongoService)
        {
            _productCollection = mongoService.Database.GetCollection<Product>("Products");
            _viewService = new ViewService(mongoService);
        }

        // === Get New Arrivals === //
        public async Task<List<Product>> GetNewArrivals(ProductInquiry inquiry)
        {
            var filterBuilder = Builders<Product>.Filter;
            var filters = new List<FilterDefinition<Product>>
            {
                filterBuilder.Eq(p => p.ProductStatus, ProductStatus.PROCESS)
            };

            if (inquiry.ProductCategory.HasValue)
                filters.Add(filterBuilder.Eq(p => p.ProductCategory, inquiry.ProductCategory.Value));


            if (!string.IsNullOrEmpty(inquiry.Search))
                filters.Add(filterBuilder.Regex(p => p.ProductName, new MongoDB.Bson.BsonRegularExpression(inquiry.Search, "i")));

            var filter = filterBuilder.And(filters);

            var sortBuilder = Builders<Product>.Sort;
            var sort = inquiry.Order == "productPrice"
                ? sortBuilder.Ascending(inquiry.Order)
                : sortBuilder.Descending(inquiry.Order ?? "createdAt");

            var result = await _productCollection
                .Find(filter)
                .Sort(sort)
                .Skip((inquiry.Page - 1) * inquiry.Limit)
                .Limit(inquiry.Limit)
                .ToListAsync();

            if (result == null || result.Count == 0)
                throw new Exception("No data found");

            return result;
        }

        // === Get Popular Products === //
        public async Task<List<Product>> GetPopularProducts(ProductInquiry inquiry)
        {
            try
            {
                var filter = Builders<Product>.Filter.Eq(p => p.ProductStatus, ProductStatus.PROCESS);

                var result = await _productCollection
                    .Find(filter)
                    .Sort(Builders<Product>.Sort.Descending("productViews"))
                    .Skip((inquiry.Page - 1) * inquiry.Limit)
                    .Limit(inquiry.Limit)
                    .ToListAsync();

                if (result == null || result.Count == 0)
                    throw new Exception("No data found");

                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error, GetPopularProducts: {ex.Message}");
                throw new Exception("Something went wrong while fetching popular products");
            }
        }

        // === Get Product By Id === //
        public async Task<Product> GetProductById(string id)
        {
            var filter = Builders<Product>.Filter.Eq(p => p.Id, id);
            var product = await _productCollection.Find(filter).FirstOrDefaultAsync();

            if (product == null)
                throw new Exception("No data found");

            return product;
        }

        // === Track Product View (optional) === //
        public async Task TrackProductView(string productId, string? memberId)
        {
            try
            {
                var productFilter = Builders<Product>.Filter.Eq(p => p.Id, productId);

                if (!string.IsNullOrEmpty(memberId))
                {
                    var exists = await _viewService.CheckViewExistenceAsync(new ViewInput
                    {
                        MemberId = memberId,
                        ViewRefId = productId,
                        ViewGroup = ViewGroup.PRODUCT
                    });

                    if (exists == null)

                    {
                        await _viewService.InsertMemberViewAsync(new ViewInput
                        {
                            MemberId = memberId,
                            ViewRefId = productId,
                            ViewGroup = ViewGroup.PRODUCT
                        });

                        var update = Builders<Product>.Update.Inc(p => p.ProductViews, 1);
                        await _productCollection.UpdateOneAsync(productFilter, update);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error, TrackProductView: {ex.Message}");
            }
        }

        // === Get Product List === //
        public async Task<(List<Product> Products, long Total)> GetProductList(ProductInquiry inquiry)
        {
            var filterBuilder = Builders<Product>.Filter;
            var filters = new List<FilterDefinition<Product>>
    {
        filterBuilder.Eq(p => p.ProductStatus, ProductStatus.PROCESS)
    };

            // === Category (string[] → enum) ===
            if (inquiry.Category?.Count > 0)
            {
                var enumCategories = inquiry.Category
                    .Select(c => Enum.TryParse<ProductCategory>(c, true, out var cat) ? cat : (ProductCategory?)null)
                    .Where(c => c.HasValue)
                    .Select(c => c!.Value)
                    .ToList();

                filters.Add(filterBuilder.In(p => p.ProductCategory, enumCategories));
            }

            // === Filtering ===
            if (inquiry.ProductCategory.HasValue)
                filters.Add(filterBuilder.Eq(p => p.ProductCategory, inquiry.ProductCategory.Value));

            if (!string.IsNullOrEmpty(inquiry.Search))
                filters.Add(filterBuilder.Regex(p => p.ProductName, new MongoDB.Bson.BsonRegularExpression(inquiry.Search, "i")));

            if (inquiry.Size?.Count > 0)
            {
                // Convert input strings to enum ProductSize
                var enumSizes = inquiry.Size
                    .Select(s => Enum.TryParse<ProductSize>(s, true, out var size) ? size : (ProductSize?)null)
                    .Where(s => s.HasValue)
                    .Select(s => s!.Value)
                    .ToList();

                filters.Add(filterBuilder.In(p => p.ProductSize, enumSizes));
            }


            if (inquiry.Tag?.Count > 0)
                filters.Add(filterBuilder.AnyIn("ProductTags", inquiry.Tag));

            var filter = filterBuilder.And(filters);

            // === Sorting ===
            SortDefinition<Product> sort;
            if (inquiry.Order == "productPrice")
                sort = Builders<Product>.Sort.Ascending("ProductPrice");
            else if (inquiry.Order == "productPriceDesc")
                sort = Builders<Product>.Sort.Descending("ProductPrice");
            else if (inquiry.Order == "productViews")
                sort = Builders<Product>.Sort.Descending("ProductViews");
            else
                sort = Builders<Product>.Sort.Descending("CreatedAt");

            // === Query Execution ===
            var productsTask = _productCollection
                .Find(filter)
                .Sort(sort)
                .Skip((inquiry.Page - 1) * inquiry.Limit)
                .Limit(inquiry.Limit)
                .ToListAsync();

            var totalTask = _productCollection.CountDocumentsAsync(filter);

            await Task.WhenAll(productsTask, totalTask);

            return (productsTask.Result, totalTask.Result);
        }


        // === Get All Products (SSR) === //
        public async Task<List<Product>> GetAllProducts()
        {
            var result = await _productCollection.Find(_ => true).ToListAsync();
            return result ?? new List<Product>();
        }

        // === Create New Product (SSR) === //
        public async Task<Product> CreateNewProduct(ProductInput input)
        {
            try
            {
                await _productCollection.InsertOneAsync(new Product
                {
                    ProductName = input.ProductName,
                    ProductPrice = input.ProductPrice,
                    ProductDesc = input.ProductDesc,
                    ProductCategory = input.ProductCategory,
                    ProductImages = input.ProductImages ?? new List<string>(),
                    ProductTags = input.ProductTags ?? new List<ProductTag>(),
                    ProductStatus = ProductStatus.PROCESS,
                    ProductGender = input.ProductGender,
                    ProductLeftCount = input.ProductLeftCount,
                    ProductSize = input.ProductSize.GetValueOrDefault(),
                    ProductRating = input.ProductRating.GetValueOrDefault(),
                    CreatedAt = DateTime.UtcNow
                });

                return input.ToProduct(); // optional helper mapper
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error, CreateNewProduct: {ex.Message}");
                throw new Exception("Product creation failed.");
            }
        }
        public async Task<Product?> UpdateChosenProduct(string id, ProductUpdateInput input)
        {
            try
            {
                // === If DELETE is selected, remove the product permanently === //
                if (string.Equals(input.ProductStatus, "DELETE", StringComparison.OrdinalIgnoreCase))
                {
                    var deleteResult = await _productCollection.DeleteOneAsync(p => p.Id == id);

                    if (deleteResult.DeletedCount == 0)
                        throw new Exception("Product not found or already deleted");

                    // For compatibility with the original TS version:
                    Console.WriteLine("Product deleted successfully.");
                    return new Product
                    {
                        Id = id,
                        ProductName = "Product deleted"
                    };
                }

                // === Otherwise, update the existing product === //
                var updateDef = Builders<Product>.Update.Set(p => p.UpdatedAt, DateTime.UtcNow);

                if (!string.IsNullOrEmpty(input.ProductName))
                    updateDef = updateDef.Set(p => p.ProductName, input.ProductName);

                if (input.ProductPrice.HasValue)
                    updateDef = updateDef.Set(p => p.ProductPrice, input.ProductPrice.Value);

                if (!string.IsNullOrEmpty(input.ProductDesc))
                    updateDef = updateDef.Set(p => p.ProductDesc, input.ProductDesc);

                if (input.ProductCategory != default(ProductCategory))
                    updateDef = updateDef.Set(p => p.ProductCategory, input.ProductCategory);

                if (!string.IsNullOrEmpty(input.ProductStatus))
                {
                    var status = Enum.TryParse<ProductStatus>(input.ProductStatus, true, out var s) ? s : ProductStatus.PROCESS;
                    updateDef = updateDef.Set(p => p.ProductStatus, status);
                }

                if (input.ProductImages != null && input.ProductImages.Any())
                    updateDef = updateDef.Set(p => p.ProductImages, input.ProductImages);

                if (input.ProductTags != null && input.ProductTags.Any())
                    updateDef = updateDef.Set(p => p.ProductTags, input.ProductTags);

                var options = new FindOneAndUpdateOptions<Product>
                {
                    ReturnDocument = ReturnDocument.After
                };

                var result = await _productCollection.FindOneAndUpdateAsync(
                    Builders<Product>.Filter.Eq(p => p.Id, id),
                    updateDef,
                    options
                );

                if (result == null)
                    throw new Exception("Product update failed or not found");

                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error, UpdateChosenProduct: {ex.Message}");
                throw new Exception("Something went wrong while updating the product.");
            }
        }
    }
}
