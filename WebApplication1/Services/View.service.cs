using System;
using System.Threading.Tasks;
using MongoDB.Driver;
using ViewModel = WebApplication1.Models.View;
using WebApplication1.Types;

namespace WebApplication1.Services
{
    public class ViewService
    {
        private readonly IMongoCollection<View> _viewCollection;

        public ViewService(MongoDBService mongoService)
        {
            _viewCollection = mongoService.Database.GetCollection<View>("Views");
        }

        // === Insert view only if it doesn't already exist (Atomic Upsert) === //
        public async Task<bool> AddUniqueViewAsync(ViewInput input)
        {
            try
            {
                var filter = Builders<View>.Filter.And(
                    Builders<View>.Filter.Eq(v => v.MemberId, input.MemberId),
                    Builders<View>.Filter.Eq(v => v.ViewRefId, input.ViewRefId),
                    Builders<View>.Filter.Eq(v => v.ViewGroup, input.ViewGroup)
                );

                var update = Builders<View>.Update
                    .SetOnInsert(v => v.MemberId, input.MemberId)
                    .SetOnInsert(v => v.ViewRefId, input.ViewRefId)
                    .SetOnInsert(v => v.ViewGroup, input.ViewGroup)
                    .SetOnInsert(v => v.CreatedAt, DateTime.UtcNow)
                    .SetOnInsert(v => v.UpdatedAt, DateTime.UtcNow);

                // Perform atomic upsert
                var result = await _viewCollection.UpdateOneAsync(
                    filter,
                    update,
                    new UpdateOptions { IsUpsert = true }
                );

                // Return true only if a new view was inserted (not existing)
                if (result.UpsertedId != null)
                {
                    Console.WriteLine($"✅ New view inserted for {input.ViewGroup}: {input.ViewRefId} by {input.MemberId}");
                    return true;
                }

                Console.WriteLine($"ℹ️ View already exists: {input.ViewGroup}/{input.ViewRefId}/{input.MemberId}");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error, AddUniqueViewAsync: {ex.Message}");
                throw;
            }
        }
    }
}
