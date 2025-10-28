using ReversaWEB.Services;
using WebApplication1.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers()
.AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    }); ;
builder.Services.AddOpenApi(); // Optional OpenAPI/Swagger

// Register MongoDBService first (since others depend on it)
builder.Services.AddSingleton<MongoDBService>();

// Register your business services
builder.Services.AddSingleton<MemberService>();
builder.Services.AddSingleton<AuthService>();   // ✅ Add this line


// Enable CORS for all origins
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

var app = builder.Build();

// Configure middleware
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("AllowAll");

// optional: disable HTTPS redirection if you only test with Postman HTTP
// app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

// Ensure MongoDBService connects at startup
var mongo = app.Services.GetRequiredService<MongoDBService>();
Console.WriteLine("✅ Server running with MongoDB connection.");

// Run the app
app.Run();

// keep WeatherForecast if you need it for sample controllers
record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}



