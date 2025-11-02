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

        // === Check if view already exists === //
        public async Task<View?> CheckViewExistenceAsync(ViewInput input)
        {
            var filter = Builders<View>.Filter.And(
                Builders<View>.Filter.Eq(v => v.MemberId, input.MemberId),
                Builders<View>.Filter.Eq(v => v.ViewRefId, input.ViewRefId)
            );

            var existingView = await _viewCollection.Find(filter).FirstOrDefaultAsync();
            return existingView;
        }

        // === Insert new view === //
        public async Task<View> InsertMemberViewAsync(ViewInput input)
        {
            try
            {
                var newView = new View
                {
                    MemberId = input.MemberId,
                    ViewRefId = input.ViewRefId,
                    ViewGroup = input.ViewGroup,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _viewCollection.InsertOneAsync(newView);
                return newView;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR, InsertMemberViewAsync: {ex.Message}");
                throw new Exception("Failed to create new view entry.");
            }
        }
    }
}
