using Microsoft.AspNetCore.Mvc;
using GeoTrack.API.Services;
using MongoDB.Bson;

namespace GeoTrack.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestController : ControllerBase
    {
        private readonly MongoDBService _mongoDBService;

        public TestController(MongoDBService mongoDBService)
        {
            _mongoDBService = mongoDBService;
        }

        [HttpGet("connection")]
        public async Task<IActionResult> TestConnection()
        {
            try
            {
                var database = _mongoDBService.Database;

                var result = await database.RunCommandAsync<BsonDocument>(
                    new BsonDocument("ping", 1)
                );

                return Ok(new
                {
                    message = "MongoDB connected successfully!",
                    database = database.DatabaseNamespace.DatabaseName,
                    status = result["ok"].ToDouble() == 1 ? "Connected" : "Failed"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "MongoDB connection failed",
                    error = ex.Message
                });
            }
        }
    }
}