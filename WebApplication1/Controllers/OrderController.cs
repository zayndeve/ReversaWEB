using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Stripe;
using WebApplication1.Services;
using WebApplication1.Models;
using WebApplication1.Types;
using WebApplication1.Enums;

namespace WebApplication1.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrderController : ControllerBase
    {
        private readonly OrderService _orderService;
        private readonly IConfiguration _config;
        private readonly StripeClient _stripeClient;

        public OrderController(OrderService orderService, IConfiguration config)
        {
            _orderService = orderService;
            _config = config;
            var stripeSecret = _config["STRIPE_SECRET_KEY"];
            _stripeClient = new StripeClient(stripeSecret);
        }

        //  Create Stripe Payment Intent
        [HttpPost("create-payment-intent")]
        public async Task<IActionResult> CreatePaymentIntent([FromBody] PaymentRequest input)
        {
            try
            {
                var memberId = HttpContext.Session.GetString("User_MemberId");
                if (string.IsNullOrEmpty(memberId))
                {
                    return Unauthorized(new
                    {
                        code = 401,
                        message = "User not authenticated."
                    });
                }

                if (input.TotalAmount <= 0)
                {
                    return BadRequest(new
                    {
                        code = 400,
                        message = "Total amount is required."
                    });
                }

                var service = new PaymentIntentService(_stripeClient);
                var paymentIntent = await service.CreateAsync(new PaymentIntentCreateOptions
                {
                    Amount = (long)Math.Round(input.TotalAmount * 100),
                    Currency = "usd",
                    PaymentMethodTypes = new List<string> { "card" }
                });

                return Ok(new
                {
                    clientSecret = paymentIntent.ClientSecret
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error creating payment intent: {ex.Message}");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        //  Save order after successful payment
        [HttpPost("save-paid-order")]
        public async Task<IActionResult> SaveOrderAfterPayment([FromBody] OrderInput input)
        {
            try
            {
                var memberId = HttpContext.Session.GetString("User_MemberId");
                if (string.IsNullOrEmpty(memberId))
                {
                    return Unauthorized(new
                    {
                        code = 401,
                        message = "User not authenticated."
                    });
                }

                var savedOrder = await _orderService.SavePaidOrderAsync(memberId, input);
                Console.WriteLine($"✅ Order saved for member: {memberId}, Order ID: {savedOrder.Id}");
                return Ok(savedOrder);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error saving order: {ex.Message}");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        //  Get orders by member ID
        [HttpGet("member/{memberId}")]
        public async Task<IActionResult> GetOrdersByMember(string memberId)
        {
            try
            {
                var sessionMemberId = HttpContext.Session.GetString("User_MemberId");

                if (string.IsNullOrEmpty(sessionMemberId) || sessionMemberId != memberId)
                {
                    return Unauthorized(new
                    {
                        code = 401,
                        message = "User not authenticated."
                    });
                }

                var orders = await _orderService.GetOrdersByMemberAsync(memberId);
                Console.WriteLine($"✅ Retrieved {orders.Count} orders for member: {memberId}");
                return Ok(orders);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error fetching member orders: {ex.Message}");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }
    }
}
