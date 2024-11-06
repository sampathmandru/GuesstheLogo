using MongoDB.Driver;
using Microsoft.Extensions.Configuration;

namespace WiseWork.Services
{
    public class MongoDBService
    {
        private readonly IMongoDatabase _database;

        public MongoDBService(IConfiguration config)
        {
            var client = new MongoClient(config.GetSection("MongoDB:ConnectionString").Value);
            _database = client.GetDatabase(config.GetSection("MongoDB:DatabaseName").Value);
        }

        public IMongoCollection<T> GetCollection<T>(string name)
        {
            return _database.GetCollection<T>(name);
        }
    }
}