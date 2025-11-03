using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Driver;
using WebApplication1.Enums;
using WebApplication1.Models;
using WebApplication1.Types;

namespace WebApplication1.Services
{
    public class OrderService
    {
        private readonly IMongoCollection<WebApplication1.Models.Order> _orderCollection;
        private readonly IMongoCollection<WebApplication1.Models.OrderItem> _orderItemCollection;
        private readonly MemberService _memberService;

        public OrderService(MongoDBService mongoService, IConfiguration config)
        {
            _orderCollection = mongoService.Database.GetCollection<WebApplication1.Models.Order>("orders");
            _orderItemCollection = mongoService.Database.GetCollection<WebApplication1.Models.OrderItem>("orderItems");
            _memberService = new MemberService(mongoService, new WebApplication1.Core.Utils.EmailHelper(mongoService, config));
        }

        //  Save order after successful payment
        public async Task<WebApplication1.Models.Order> SavePaidOrderAsync(string memberId, WebApplication1.Types.OrderInput input)
        {
            try
            {
                if (string.IsNullOrEmpty(memberId))
                    throw new Exception("Invalid member ID");

                var objectId = new ObjectId(memberId);

                // === Compute totals ===
                var totalAmount = input.OrderItems.Sum(i => i.ItemPrice * i.ItemQuantity);
                var totalQuantity = input.OrderItems.Sum(i => i.ItemQuantity);
                var delivery = totalAmount < 100 ? 5.0 : 0.0;

                // === Prepare preview item ===
                var firstItem = input.OrderItems.FirstOrDefault();
                var previewItem = new PreviewItem
                {
                    Name = firstItem?.ProductName ?? "Item",
                    Image = firstItem?.ProductImage ?? string.Empty
                };

                // === Create new order ===
                var newOrder = new WebApplication1.Models.Order
                {
                    MemberId = memberId,
                    TotalAmount = totalAmount + delivery,
                    PaymentMethod = input.PaymentMethod,
                    OrderStatus = OrderStatus.PAID,
                    ShippingAddress = new WebApplication1.Models.ShippingAddress
                    {
                        FullName = input.ShippingAddress.FullName,
                        Phone = input.ShippingAddress.Phone,
                        Address = input.ShippingAddress.Address,
                        City = input.ShippingAddress.City,
                        PostalCode = input.ShippingAddress.PostalCode,
                        Country = input.ShippingAddress.Country
                    },
                    PreviewItem = previewItem,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _orderCollection.InsertOneAsync(newOrder);

                // === Record order items ===
                await RecordOrderItemsAsync(newOrder.Id, input.OrderItems);

                // === Add member points ===
                var earnedPoints = totalQuantity * 2;
                await _memberService.AddUserPointAsync(memberId, earnedPoints);

                return newOrder;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error, SavePaidOrderAsync: {ex.Message}");
                throw new Exception("Something went wrong while saving the order.");
            }
        }

        //  Save order items
        private async Task RecordOrderItemsAsync(string orderId, List<OrderItemInput> items)
        {
            if (items == null || items.Count == 0)
                return;

            var itemData = items.Select(item => new WebApplication1.Models.OrderItem
            {
                OrderId = orderId,
                ProductId = item.ProductId,
                ItemPrice = item.ItemPrice,
                ItemQuantity = item.ItemQuantity,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }).ToList();

            await _orderItemCollection.InsertManyAsync(itemData);
        }

        //  Get all orders by member
        public async Task<List<WebApplication1.Models.Order>> GetOrdersByMemberAsync(string memberId)
        {
            try
            {
                var filter = Builders<WebApplication1.Models.Order>.Filter.Eq(o => o.MemberId, memberId);
                var orders = await _orderCollection
                    .Find(filter)
                    .SortByDescending(o => o.CreatedAt)
                    .ToListAsync();

                return orders;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error, GetOrdersByMemberAsync: {ex.Message}");
                throw;
            }
        }

        //  Update order status
        public async Task<WebApplication1.Models.Order?> UpdateOrderStatusAsync(string orderId, OrderStatus newStatus)
        {
            try
            {
                var filter = Builders<WebApplication1.Models.Order>.Filter.Eq(o => o.Id, orderId);
                var update = Builders<WebApplication1.Models.Order>.Update
                    .Set(o => o.OrderStatus, newStatus)
                    .Set(o => o.UpdatedAt, DateTime.UtcNow);

                var options = new FindOneAndUpdateOptions<WebApplication1.Models.Order>
                {
                    ReturnDocument = ReturnDocument.After
                };

                return await _orderCollection.FindOneAndUpdateAsync(filter, update, options);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error, UpdateOrderStatusAsync: {ex.Message}");
                throw new Exception("Failed to update order status.");
            }
        }
    }
}
