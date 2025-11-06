using MongoDB.Driver;
using MongoDB.Bson;
using WebApplication1.Models;
using WebApplication1.Enums;

namespace WebApplication1.Services
{
    public class AnalyticsService
    {
        private readonly IMongoCollection<Order> _orders;
        private readonly IMongoCollection<OrderItem> _orderItems;
        private readonly IMongoCollection<Member> _members;
        private readonly IMongoCollection<Product> _products;

        public AnalyticsService(MongoDBService mongo)
        {
            _orders = mongo.Database.GetCollection<Order>("orders");
            _orderItems = mongo.Database.GetCollection<OrderItem>("orderItems");
            _members = mongo.Database.GetCollection<Member>("members");
            _products = mongo.Database.GetCollection<Product>("Products"); // ✅ matches DB
        }

        // ===== KPI ===== //
        public async Task<object> GetKPIAsync()
        {
            var totalOrders = await _orders.CountDocumentsAsync(o => o.OrderStatus == OrderStatus.PAID);

            var totalRevenueAgg = await _orders.Aggregate()
                .Match(o => o.OrderStatus == OrderStatus.PAID)
                .Group(new BsonDocument
                {
                    { "_id", BsonNull.Value },
                    { "total", new BsonDocument("$sum", "$totalAmount") }
                })
                .FirstOrDefaultAsync();

            var totalRevenue = totalRevenueAgg?["total"].ToDecimal() ?? 0;
            // get unique member IDs as ObjectId
            var customers = await _orders.DistinctAsync<ObjectId>("memberId", Builders<Order>.Filter.Empty);
            var totalCustomers = customers.ToList().Count;


            var averageOrder = totalOrders > 0 ? totalRevenue / totalOrders : 0;

            return new
            {
                TotalOrders = totalOrders,
                TotalRevenue = totalRevenue,
                TotalCustomers = totalCustomers,
                AverageOrder = averageOrder
            };
        }

        // ===== Monthly Sales ===== //
        public async Task<List<object>> GetMonthlySalesAsync()
        {
            var pipeline = new[]
            {
                new BsonDocument("$match", new BsonDocument("orderStatus", OrderStatus.PAID.ToString())),
                new BsonDocument("$group", new BsonDocument
                {
                    { "_id", new BsonDocument
                        {
                            // ✅ use lowercase 'createdAt'
                            { "year", new BsonDocument("$year", "$createdAt") },
                            { "month", new BsonDocument("$month", "$createdAt") }
                        }
                    },
                    { "total", new BsonDocument("$sum", "$totalAmount") },
                    { "orders", new BsonDocument("$sum", 1) }
                }),
                new BsonDocument("$sort", new BsonDocument
                {
                    { "_id.year", 1 },
                    { "_id.month", 1 }
                })
            };

            var results = await _orders.Aggregate<BsonDocument>(pipeline).ToListAsync();

            return results.Select(r => new
            {
                Month = $"{r["_id"]["month"]}/{r["_id"]["year"]}",
                Total = r["total"].ToDecimal(),
                Orders = r["orders"].ToInt32()
            } as object).ToList();
        }

        // ===== Top Categories ===== //
        public async Task<List<object>> GetTopCategoriesAsync()
        {
            var pipeline = new[]
            {
                // ✅ convert productId only if it’s a string
                new BsonDocument("$addFields", new BsonDocument("productId",
                    new BsonDocument("$cond", new BsonDocument
                    {
                        { "if", new BsonDocument("$eq", new BsonArray { new BsonDocument("$type", "$productId"), "string" }) },
                        { "then", new BsonDocument("$toObjectId", "$productId") },
                        { "else", "$productId" }
                    }))
                ),
                new BsonDocument("$lookup", new BsonDocument
                {
                    { "from", "Products" },
                    { "localField", "productId" },
                    { "foreignField", "_id" },
                    { "as", "product" }
                }),
                new BsonDocument("$unwind", "$product"),
                new BsonDocument("$group", new BsonDocument
                {
                    // ✅ correct key (capital P)
                    { "_id", "$product.ProductCategory" },
                    { "value", new BsonDocument("$sum",
                        new BsonDocument("$multiply", new BsonArray { "$itemPrice", "$itemQuantity" })) }
                }),
                new BsonDocument("$sort", new BsonDocument("value", -1)),
                new BsonDocument("$limit", 5)
            };

            var results = await _orderItems.Aggregate<BsonDocument>(pipeline).ToListAsync();

            return results.Select(r => new
            {
                Name = r["_id"].IsBsonNull ? "Uncategorized" : r["_id"].AsString,
                Value = r["value"].ToDecimal()
            } as object).ToList();
        }

        // ===== Top Buyers ===== //
        public async Task<List<object>> GetTopBuyersAsync()
        {
            var pipeline = new[]
            {
                new BsonDocument("$addFields", new BsonDocument("orderId",
                    new BsonDocument("$toObjectId", "$orderId"))),
                new BsonDocument("$lookup", new BsonDocument
                {
                    { "from", "orders" },
                    { "localField", "orderId" },
                    { "foreignField", "_id" },
                    { "as", "order" }
                }),
                new BsonDocument("$unwind", "$order"),
                new BsonDocument("$match", new BsonDocument("order.orderStatus", OrderStatus.PAID.ToString())),
                new BsonDocument("$group", new BsonDocument
                {
                    { "_id", "$order.memberId" },
                    { "totalSpent", new BsonDocument("$sum",
                        new BsonDocument("$multiply", new BsonArray { "$itemPrice", "$itemQuantity" })) },
                    { "lastPurchase", new BsonDocument("$max", "$order.createdAt") }
                }),
                new BsonDocument("$sort", new BsonDocument("totalSpent", -1)),
                new BsonDocument("$limit", 5)
            };

            var result = await _orderItems.Aggregate<BsonDocument>(pipeline).ToListAsync();
            var buyers = new List<object>();

            foreach (var buyer in result)
            {
                // handle both string and ObjectId _id from aggregation
                ObjectId memberId;
                if (buyer["_id"].BsonType == BsonType.ObjectId)
                    memberId = buyer["_id"].AsObjectId;
                else
                    memberId = ObjectId.Parse(buyer["_id"].ToString());

                var member = await _members.Find(Builders<Member>.Filter.Eq("_id", memberId)).FirstOrDefaultAsync();

                buyers.Add(new
                {
                    Nickname = member?.MemberNick ?? "Unknown",
                    TotalSpent = buyer["totalSpent"].ToDecimal(),
                    TotalSpentFormatted = string.Format("{0:N0}", buyer["totalSpent"].ToDecimal()),
                    LastPurchaseFormatted = ((DateTime)buyer["lastPurchase"]).ToShortDateString()
                });
            }


            return buyers;
        }
    }
}
