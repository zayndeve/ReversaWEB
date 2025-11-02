using Microsoft.AspNetCore.Mvc;
using WebApplication1.Services;
using WebApplication1.Models;
using WebApplication1.Enums;
using WebApplication1.Types;
using System;
using System.Threading.Tasks;
using WebApplication1.Core.Utils;
using ViewModel = WebApplication1.Models.View;


namespace WebApplication1.Controllers
{
    [Route("api/[controller]")]
    public class ProductController : Controller

    {
        private readonly ProductService _productService;

        public ProductController(ProductService productService)
        {
            _productService = productService;
        }

        // === Get New Arrivals === //
        [HttpGet("new-arrivals")]
        public async Task<IActionResult> GetNewArrivals(
            [FromQuery] string order = "createdAt",
            [FromQuery] int page = 1,
            [FromQuery] int limit = 8,
            [FromQuery] string? search = null,
            [FromQuery] ProductCategory? productCategory = null)
        {
            try
            {
                var inquiry = new ProductInquiry
                {
                    Order = order,
                    Page = page,
                    Limit = limit,
                    Search = search,
                    ProductCategory = productCategory
                };

                var result = await _productService.GetNewArrivals(inquiry);
                return Ok(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error, GetNewArrivals: " + ex.Message);
                return StatusCode(500, new { message = "Something went wrong" });
            }
        }

        // === Get Popular Products === //
        [HttpGet("popular")]
        public async Task<IActionResult> GetPopularProducts(
            [FromQuery] string order = "productViews",
            [FromQuery] int page = 1,
            [FromQuery] int limit = 8)
        {
            try
            {
                var inquiry = new ProductInquiry
                {
                    Order = order,
                    Page = page,
                    Limit = limit
                };

                var result = await _productService.GetPopularProducts(inquiry);
                return Ok(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error, GetPopularProducts: " + ex.Message);
                return StatusCode(500, new { message = "Something went wrong" });
            }
        }

        // === Get Product By Id === //
        [HttpGet("{id}")]
        public async Task<IActionResult> GetProductById(string id)
        {
            try
            {
                Console.WriteLine("GetProductById");

                var result = await _productService.GetProductById(id);

                // Auto-increase views
                var memberId = HttpContext.Items["memberId"]?.ToString();
                await _productService.TrackProductView(id, memberId);

                return Ok(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error, GetProductById: " + ex.Message);
                return StatusCode(500, new { message = "Something went wrong" });
            }
        }

        // === Get Product List === //
        [HttpGet("list")]
        public async Task<IActionResult> GetProductList(
            [FromQuery] string order = "createdAt",
            [FromQuery] int page = 1,
            [FromQuery] int limit = 12,
            [FromQuery] string? search = null,
            [FromQuery] ProductCategory? productCategory = null,
            [FromQuery] string? category = null,
            [FromQuery] string? size = null,
            [FromQuery] string? tag = null)
        {
            try
            {
                var inquiry = new ProductInquiry
                {
                    Order = order,
                    Page = page,
                    Limit = limit,
                    Search = search,
                    ProductCategory = productCategory
                };

                if (!string.IsNullOrEmpty(category))
                    inquiry.Category = new List<string>(category.Split(','));

                if (!string.IsNullOrEmpty(size))
                    inquiry.Size = new List<string>(size.Split(','));

                if (!string.IsNullOrEmpty(tag))
                    inquiry.Tag = new List<string>(tag.Split(','));

                var (products, total) = await _productService.GetProductList(inquiry);
                return Ok(new { products, total });
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error, GetProductList: " + ex.Message);
                return StatusCode(500, new { message = "Something went wrong" });
            }
        }

        // === Get All Products (SSR) === //
        [HttpGet("all")]
        [HttpGet("/admin/product/all")]
        public async Task<IActionResult> GetAllProducts()
        {
            try
            {
                Console.WriteLine("GetAllProducts");
                var products = await _productService.GetAllProducts();
                return View("~/Views/Admin/Product.cshtml", products);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error, GetAllProducts: " + ex.Message);
                var message = "Something went wrong while loading products.";
                return Content(
                    $"<script>alert('{message}'); window.location.replace('/admin/product/all');</script>",
                    "text/html"
                );
            }
        }

        // === Create New Product (SSR) === //
        [HttpPost("create")]
        [HttpPost("/admin/product/create")]
        public async Task<IActionResult> CreateNewProduct(List<IFormFile>? productImages, [FromForm] ProductInput model)
        {
            try
            {
                Console.WriteLine("CreateNewProduct");
                Console.WriteLine("Files count: " + (productImages?.Count ?? 0));

                if (productImages == null || productImages.Count == 0)
                {
                    throw new Exception("File upload failed or missing files.");
                }

                model.ProductImages = new List<string>();
                foreach (var file in productImages)
                {
                    var path = await FileUploader.SaveFileAsync(file, "products");
                    model.ProductImages.Add(path);
                }

                await _productService.CreateNewProduct(model);

                return Content(
                    "<script>alert('✅ Successful creation!'); window.location.replace('/admin/product/all');</script>",
                    "text/html"
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error, CreateNewProduct: " + ex.Message);
                var message = "Product creation failed.";
                return Content(
                    $"<script>alert('{message}'); window.location.replace('/admin/product/all');</script>",
                    "text/html"
                );
            }
        }

        // === Update Chosen Product (SSR) === //
        [HttpPost("update/{id}")]
        [HttpPost("/admin/product/{id}")]
        public async Task<IActionResult> UpdateChosenProduct(string id, [FromBody] ProductUpdateInput model)

        {
            try
            {
                Console.WriteLine("UpdateChosenProduct");
                var result = await _productService.UpdateChosenProduct(id, model);
                return Ok(new { data = result });
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error, UpdateChosenProduct: " + ex.Message);
                var message = "Product update failed.";
                return Content(
                    $"<script>alert('{message}'); window.location.replace('/admin/product/all');</script>",
                    "text/html"
                );
            }
        }
    }
}
