using WebApplication1.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Register controllers and MongoDBService as a singleton
builder.Services.AddControllers();
builder.Services.AddSingleton<MongoDBService>();

// Enable CORS for all origins (AllowAll)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Use CORS
app.UseCors("AllowAll");

app.UseAuthorization();

// Map controllers
app.MapControllers();

// Ensure MongoDBService is created at startup so it attempts connection and prints status
var mongo = app.Services.GetRequiredService<MongoDBService>();
Console.WriteLine("Server running with MongoDB connection.");

app.Run();

// keep the small WeatherForecast record if needed by samples
record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
