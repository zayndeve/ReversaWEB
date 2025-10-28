using System;
using MongoDB.Bson;
using MongoDB.Driver;

namespace WebApplication1.Services
{
    /// <summary>
    /// Simple MongoDB service that connects using encoded credentials
    /// and exposes a generic GetCollection<T> helper.
    /// </summary>
    public class MongoDBService
    {
        private readonly IMongoClient _client;
        private readonly IMongoDatabase _database;

        // Credentials and host (as provided)
        private const string User = "w136138";
        private const string Password = "wp@aV5n$8rJ";
        private const string DbName = "w136138DB";
        private const string Host = "webp.flykorea.kr:27017";

        public IMongoClient Client => _client;
        public IMongoDatabase Database => _database;

        public MongoDBService()
        {
            // URL-encode credentials to handle special characters
            var encodedUser = Uri.EscapeDataString(User);
            var encodedPassword = Uri.EscapeDataString(Password);

            // Build connection string. authSource set to the database name.
            var connectionString = $"mongodb://{encodedUser}:{encodedPassword}@{Host}/{DbName}?authSource={DbName}";

            _client = new MongoClient(connectionString);
            _database = _client.GetDatabase(DbName);

            try
            {
                // Ping the server to verify connectivity
                var command = new BsonDocument("ping", 1);
                _database.RunCommand<BsonDocument>(command);
                Console.WriteLine("✅ MongoDB connected successfully!");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"❌ MongoDB connection failed: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Returns an IMongoCollection for the given type T.
        /// If collectionName is not provided, the collection name defaults to the type name of T.
        /// </summary>
        public IMongoCollection<T> GetCollection<T>(string? collectionName = null)
        {
            var name = collectionName ?? typeof(T).Name;
            return _database.GetCollection<T>(name);
        }
    }
}

