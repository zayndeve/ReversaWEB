using MongoDB.Driver;
using MongoDB.Bson;
using WebApplication1.Models;

namespace WebApplication1.Services
{
    public class AnalyticsService
    {
        // private readonly IMongoCollection<Order> _orders;
        // private readonly IMongoCollection<OrderItem> _orderItems;
        private readonly IMongoCollection<Member> _members;
        // private readonly IMongoCollection<Product> _products;

        public AnalyticsService(MongoDBService mongo)
        {
            // _orders = mongo.Database.GetCollection<Order>("orders");
            // _orderItems = mongo.Database.GetCollection<OrderItem>("orderitems");
            _members = mongo.Database.GetCollection<Member>("members");
            // _products = mongo.Database.GetCollection<Product>("products");
        }

        // ===== KPI ===== //
        // public async Task<object> GetKPIAsync()
        // {
        //     var totalOrders = await _orders.CountDocumentsAsync(o => o.OrderStatus == "PAID");

        //     var totalRevenueAgg = await _orders.Aggregate()
        //         .Match(o => o.OrderStatus == "PAID")
        //         .Group(new BsonDocument
        //         {
        //             { "_id", BsonNull.Value },
        //             { "total", new BsonDocument("$sum", "$TotalAmount") }
        //         })
        //         .FirstOrDefaultAsync();

        //     var totalRevenue = totalRevenueAgg?["total"].ToDecimal() ?? 0;
        //     var customers = await _orders.DistinctAsync<string>("MemberId", Builders<Order>.Filter.Empty);
        //     var totalCustomers = customers.ToList().Count;

        //     var averageOrder = totalOrders > 0 ? totalRevenue / totalOrders : 0;

        //     return new
        //     {
        //         TotalOrders = totalOrders,
        //         TotalRevenue = totalRevenue,
        //         TotalCustomers = totalCustomers,
        //         AverageOrder = averageOrder
        //     };
        // }

        // ===== Monthly Sales ===== //
        // public async Task<List<object>> GetMonthlySalesAsync()
        // {
        //     var pipeline = new[]
        //     {
        //         new BsonDocument("$match", new BsonDocument("OrderStatus", "PAID")),
        //         new BsonDocument("$group", new BsonDocument
        //         {
        //             { "_id", new BsonDocument
        //                 {
        //                     { "year", new BsonDocument("$year", "$CreatedAt") },
        //                     { "month", new BsonDocument("$month", "$CreatedAt") }
        //                 }
        //             },
        //             { "total", new BsonDocument("$sum", "$TotalAmount") },
        //             { "orders", new BsonDocument("$sum", 1) }
        //         }),
        //         new BsonDocument("$sort", new BsonDocument
        //         {
        //             { "_id.year", 1 },
        //             { "_id.month", 1 }
        //         })
        //     };

        //     var results = await _orders.Aggregate<BsonDocument>(pipeline).ToListAsync();

        //     return results.Select(r => new
        //     {
        //         Month = $"{r["_id"]["month"]}/{r["_id"]["year"]}",
        //         Total = r["total"].ToDecimal(),
        //         Orders = r["orders"].ToInt32()
        //     } as object).ToList();
        // }

        // ===== Top Categories ===== //
        // public async Task<List<object>> GetTopCategoriesAsync()
        // {
        //     var pipeline = new[]
        //     {
        //         new BsonDocument("$lookup", new BsonDocument
        //         {
        //             { "from", "products" },
        //             { "localField", "ProductId" },
        //             { "foreignField", "_id" },
        //             { "as", "product" }
        //         }),
        //         new BsonDocument("$unwind", "$product"),
        //         new BsonDocument("$group", new BsonDocument
        //         {
        //             { "_id", "$product.ProductCategory" },
        //             { "value", new BsonDocument("$sum",
        //                 new BsonDocument("$multiply", new BsonArray { "$ItemPrice", "$ItemQuantity" })) }
        //         }),
        //         new BsonDocument("$sort", new BsonDocument("value", -1)),
        //         new BsonDocument("$limit", 5)
        //     };

        //     var results = await _orderItems.Aggregate<BsonDocument>(pipeline).ToListAsync();

        //     return results.Select(r => new
        //     {
        //         Name = r["_id"].IsBsonNull ? "Uncategorized" : r["_id"].AsString,
        //         Value = r["value"].ToDecimal()
        //     } as object).ToList();
        // }

        // ===== Top Buyers ===== //
        public async Task<List<object>> GetTopBuyersAsync()
        {
            var pipeline = new[]
            {
                new BsonDocument("$lookup", new BsonDocument
                {
                    { "from", "orders" },
                    { "localField", "OrderId" },
                    { "foreignField", "_id" },
                    { "as", "order" }
                }),
                new BsonDocument("$unwind", "$order"),
                new BsonDocument("$group", new BsonDocument
                {
                    { "_id", "$order.MemberId" },
                    { "totalSpent", new BsonDocument("$sum",
                        new BsonDocument("$multiply", new BsonArray { "$ItemPrice", "$ItemQuantity" })) },
                    { "lastPurchase", new BsonDocument("$max", "$order.CreatedAt") }
                }),
                new BsonDocument("$sort", new BsonDocument("totalSpent", -1)),
                new BsonDocument("$limit", 5)
            };

            // var result = await _orderItems.Aggregate<BsonDocument>(pipeline).ToListAsync();
            var buyers = new List<object>();

            // foreach (var buyer in result)
            {
                // var memberId = buyer["_id"].AsString;
                // var member = await _members.Find(m => m.Id == memberId).FirstOrDefaultAsync();

                buyers.Add(new
                {
                    // Nickname = member?.MemberNick ?? "Unknown",
                    // TotalSpent = buyer["totalSpent"].ToDecimal(),
                    // TotalSpentFormatted = string.Format("{0:N0}", buyer["totalSpent"].ToDecimal()),
                    // LastPurchaseFormatted = buyer["lastPurchase"].ToUniversalTime().ToShortDateString()
                });
            }

            return buyers;
        }
    }
}
