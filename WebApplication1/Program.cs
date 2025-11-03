using Microsoft.AspNetCore.Http;
using WebApplication1.Services;
using WebApplication1.Core.Utils;
using System.Text.Json.Serialization;
using System.Text.Json;



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
// Email helper (uses MongoDBService internally)
builder.Services.AddSingleton<EmailHelper>();

// === Enable session support === //
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2); // 2 hours session timeout
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// === Enable CORS for all origins === //
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy
            .WithOrigins("http://localhost:3000") // your React dev server
            .SetIsOriginAllowedToAllowWildcardSubdomains()
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials());
});


// =======================================
// 🔹 Build App
// =======================================
var app = builder.Build();

// =======================================
// 🔹 Fix for .NET 9 logger crash
// =======================================
// Prevents: InvalidCastException (StateMachineAttribute[])
AppContext.SetSwitch("System.Diagnostics.StackTrace.UseNativeStackTrace", false);

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
app.UseCors("AllowAll");

// Enable static files and routing
app.UseStaticFiles();
app.UseRouting();

//  Enable sessions (after CORS)
app.UseSession();

// Enable authorization (optional)
app.UseAuthorization();

// =======================================
// 🔹 Map Controllers and MVC Routes
// =======================================
app.MapControllers();
app.MapDefaultControllerRoute(); //  enables default {controller}/{action}/{id?} pattern

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
