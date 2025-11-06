using Microsoft.AspNetCore.Http;
using WebApplication1.Services;
using WebApplication1.Core.Utils;
using System.Text.Json.Serialization;
using System.Text.Json;

// =======================================
// 🔹 Fix for .NET 9 logger crash (must be BEFORE CreateBuilder)
// =======================================
// Prevents: InvalidCastException (StateMachineAttribute[])
AppContext.SetSwitch("System.Diagnostics.StackTrace.UseNativeStackTrace", false);

var builder = WebApplication.CreateBuilder(args);

// =======================================
// 🔹 Configure Services
// =======================================

// === Add MVC and JSON options === //
builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));

    });

// === Enable OpenAPI/Swagger (optional) === //
builder.Services.AddOpenApi();

// === Register MongoDB and custom services === //
builder.Services.AddSingleton<MongoDBService>();
builder.Services.AddSingleton<MemberService>();
builder.Services.AddSingleton<AuthService>();
builder.Services.AddSingleton<AnalyticsService>();
builder.Services.AddSingleton<ProductService>();
builder.Services.AddSingleton<OrderService>();
// Email helper (uses MongoDBService internally)
builder.Services.AddSingleton<EmailHelper>();

// === Enable session support === //
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2); // 2 hours session timeout
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    // Note: Cookie name is default .AspNetCore.Session, but sessions are isolated by different keys (Admin_* vs User_*)
});

// === Enable CORS for React frontend === //
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
        policy.WithOrigins("http://localhost:3000")
              .AllowCredentials()
              .AllowAnyHeader()
              .AllowAnyMethod());
});


// =======================================
// 🔹 Build App
// =======================================
var app = builder.Build();

// =======================================
// 🔹 Developer Mode Middleware
// =======================================
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.MapOpenApi();
}

// =======================================
// 🔹 Core Middleware Pipeline
// =======================================

// Serve static files from wwwroot
//  Enable CORS *before* routing and session for frontend requests
app.UseCors("AllowFrontend");

// Enable static files and routing
app.UseStaticFiles();
app.UseRouting();

// Enable authorization (optional)
app.UseAuthorization();

// Enable sessions (after routing)
app.UseSession();

// =======================================
// 🔹 Map Controllers and MVC Routes with Separate Sessions
// =======================================
// Admin routes (/admin/*) use Admin_* session keys for isolation
app.MapWhen(context => context.Request.Path.StartsWithSegments("/admin"), adminApp =>
{
    adminApp.UseEndpoints(endpoints => endpoints.MapDefaultControllerRoute()); // Enables default {controller}/{action}/{id?} pattern for admin
});

// Member API routes (/api/member/*) use User_* session keys for isolation
app.MapWhen(context => context.Request.Path.StartsWithSegments("/api/member"), memberApp =>
{
    memberApp.UseEndpoints(endpoints => endpoints.MapControllers()); // Maps API controllers for member
});

// =======================================
// 🔹 Connect MongoDB on startup
// =======================================
var mongo = app.Services.GetRequiredService<MongoDBService>();
Console.WriteLine(" MongoDB connected successfully!");
Console.WriteLine(" Server running with MongoDB connection.");

// =======================================
// 🔹 Run the Application
// =======================================
app.Run();

// Optional example record (safe to keep or remove)
record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
