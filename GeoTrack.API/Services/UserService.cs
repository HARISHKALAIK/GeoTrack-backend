using GeoTrack.API.Models;
using MongoDB.Driver;

namespace GeoTrack.API.Services
{
    public class UserService
    {
        private readonly IMongoCollection<User> _users;

        public UserService(MongoDBService mongoDBService)
        {
            _users = mongoDBService.GetCollection<User>("Users");
        }

        public async Task<List<User>> GetAllAsync()
        {
            return await _users.Find(_ => true).ToListAsync();
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _users.Find(x => x.Email == email)
                               .FirstOrDefaultAsync();
        }

        public async Task CreateAsync(User user)
        {
            await _users.InsertOneAsync(user);
        }
        public async Task<bool> EmailExistsAsync(string email)
{
    return await _users
        .Find(x => x.Email == email)
        .AnyAsync();
}
    }
    
}