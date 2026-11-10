using MongoDB.Driver;

namespace GeoTrack.API.Services
{
    public class MongoDBService
    {
        private readonly IMongoDatabase _database;

        public IMongoDatabase Database => _database;

        public MongoDBService(IConfiguration configuration)
        {
            var connectionString =
                configuration["MongoDB:ConnectionString"];

            var databaseName =
                configuration["MongoDB:DatabaseName"];

            var client = new MongoClient(connectionString);

            _database = client.GetDatabase(databaseName);
        }

        public IMongoCollection<T> GetCollection<T>(string name)
        {
            return _database.GetCollection<T>(name);
        }
    }
}