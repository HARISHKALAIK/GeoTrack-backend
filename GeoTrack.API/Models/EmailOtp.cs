using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace GeoTrack.API.Models
{
    public class EmailOtp
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        public string Email { get; set; } = string.Empty;

        public string OtpHash { get; set; } = string.Empty;

        public DateTime ExpiryTime { get; set; }

        public bool IsVerified { get; set; } = false;

        public DateTime? VerifiedAt { get; set; }

        public int Attempts { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}