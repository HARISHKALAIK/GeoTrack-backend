using GeoTrack.API.Models;
using MongoDB.Driver;
using System.Security.Cryptography;

namespace GeoTrack.API.Services
{
    public class OtpService
    {
        private readonly IMongoCollection<EmailOtp> _otps;

        public OtpService(MongoDBService mongoDBService)
        {
            _otps = mongoDBService.GetCollection<EmailOtp>("EmailOtps");
        }

        // Generate a random 6-digit OTP
        public string GenerateOtp()
        {
            return RandomNumberGenerator.GetInt32(100000, 1000000)
                .ToString();
        }

        // Save or update hashed OTP in MongoDB
        public async Task SaveOtpAsync(string email, string otp)
        {
            var existingOtp = await _otps
                .Find(x => x.Email == email)
                .FirstOrDefaultAsync();

            var otpHash = BCrypt.Net.BCrypt.HashPassword(otp);

            if (existingOtp != null)
            {
                // Update existing document without changing MongoDB ID
                var update = Builders<EmailOtp>.Update
    .Set(x => x.OtpHash, otpHash)
    .Set(x => x.ExpiryTime, DateTime.UtcNow.AddMinutes(5))
    .Set(x => x.IsVerified, false)
    .Set(x => x.VerifiedAt, null)
    .Set(x => x.Attempts, 0)
    .Set(x => x.CreatedAt, DateTime.UtcNow);

                await _otps.UpdateOneAsync(
                    x => x.Email == email,
                    update
                );
            }
            else
            {
                // Insert new document
                var otpData = new EmailOtp
{
    Email = email,
    OtpHash = otpHash,
    ExpiryTime = DateTime.UtcNow.AddMinutes(5),
    IsVerified = false,
    VerifiedAt = null,
    Attempts = 0,
    CreatedAt = DateTime.UtcNow
};

                await _otps.InsertOneAsync(otpData);
            }
        }

        // Verify OTP
        public async Task<bool> VerifyOtpAsync(string email, string otp)
        {
            var otpData = await _otps
                .Find(x => x.Email == email)
                .FirstOrDefaultAsync();

            if (otpData == null)
                return false;

            if (otpData.IsVerified)
                return false;

            if (otpData.ExpiryTime < DateTime.UtcNow)
                return false;

            if (otpData.Attempts >= 5)
                return false;

            bool isValid = BCrypt.Net.BCrypt.Verify(
                otp,
                otpData.OtpHash
            );

            if (!isValid)
            {
                var update = Builders<EmailOtp>.Update
                    .Inc(x => x.Attempts, 1);

                await _otps.UpdateOneAsync(
                    x => x.Email == email,
                    update
                );

                return false;
            }

            var verifyUpdate = Builders<EmailOtp>.Update
    .Set(x => x.IsVerified, true)
    .Set(x => x.VerifiedAt, DateTime.UtcNow);

            await _otps.UpdateOneAsync(
                x => x.Email == email,
                verifyUpdate
            );

            return true;
        }
        public async Task<bool> IsEmailVerifiedAsync(string email)
{
    var otpData = await _otps
        .Find(x => x.Email == email)
        .FirstOrDefaultAsync();

    if (otpData == null)
        return false;

    if (!otpData.IsVerified)
        return false;

    if (otpData.VerifiedAt == null)
        return false;

    // Allow registration within 10 minutes of verification
    return otpData.VerifiedAt.Value >
           DateTime.UtcNow.AddMinutes(-10);
}
    }
}