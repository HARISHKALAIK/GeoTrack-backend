using Microsoft.AspNetCore.Mvc;
using GeoTrack.API.DTOs;
using GeoTrack.API.Services;

namespace GeoTrack.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly OtpService _otpService;
        private readonly EmailService _emailService;
        private readonly UserService _userService;

        public AuthController(
            OtpService otpService,
            EmailService emailService,
            UserService userService)
        {
            _otpService = otpService;
            _emailService = emailService;
            _userService = userService;
        }

        // SEND OTP
        [HttpPost("send-otp")]
        public async Task<IActionResult> SendOtp(SendOtpDto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Email))
                {
                    return BadRequest(new
                    {
                        message = "Email is required"
                    });
                }

                string email = dto.Email.Trim().ToLowerInvariant();

                // Check whether user already exists
                var existingUser = await _userService.GetByEmailAsync(email);

                if (existingUser != null)
                {
                    return BadRequest(new
                    {
                        message = "An account already exists with this email"
                    });
                }

                // Generate OTP
                string otp = _otpService.GenerateOtp();

                // Save hashed OTP in MongoDB
                await _otpService.SaveOtpAsync(email, otp);

                // Send OTP through Gmail
                await _emailService.SendOtpEmailAsync(email, otp);

                return Ok(new
                {
                    message = "OTP sent successfully to your email",
                    email = email,
                    expiryMinutes = 5
                });
            }
            catch (Exception ex)
{
    Console.WriteLine("OTP ERROR: " + ex.ToString());

    return StatusCode(500, new
    {
        message = "Unable to send OTP",
        error = ex.Message
    });
}
        }
        // VERIFY OTP
[HttpPost("verify-otp")]
public async Task<IActionResult> VerifyOtp(VerifyOtpDto dto)
{
    try
    {
        if (string.IsNullOrWhiteSpace(dto.Email) ||
            string.IsNullOrWhiteSpace(dto.Otp))
        {
            return BadRequest(new
            {
                message = "Email and OTP are required"
            });
        }

        string email = dto.Email.Trim().ToLowerInvariant();

        bool isValid = await _otpService.VerifyOtpAsync(
            email,
            dto.Otp.Trim()
        );

        if (!isValid)
        {
            return BadRequest(new
            {
                message = "Invalid or expired OTP"
            });
        }

        return Ok(new
        {
            message = "Email verified successfully",
            email = email,
            verified = true
        });
    }
    catch (Exception ex)
    {
        Console.WriteLine("VERIFY OTP ERROR: " + ex);

        return StatusCode(500, new
        {
            message = "OTP verification failed"
        });
    }
}
[HttpPost("register")]
public async Task<IActionResult> Register(RegisterDto dto)
{
    try
    {
        if (string.IsNullOrWhiteSpace(dto.Name) ||
            string.IsNullOrWhiteSpace(dto.Email) ||
            string.IsNullOrWhiteSpace(dto.Password))
        {
            return BadRequest(new
            {
                message = "All fields are required"
            });
        }

        if (dto.Password != dto.ConfirmPassword)
        {
            return BadRequest(new
            {
                message = "Passwords do not match"
            });
        }

        string email = dto.Email.Trim().ToLowerInvariant();

        // Check whether account already exists
        bool exists = await _userService.EmailExistsAsync(email);

        if (exists)
        {
            return BadRequest(new
            {
                message = "Email already registered"
            });
        }

        // Verify email OTP status
        var otpVerified = await _otpService
            .IsEmailVerifiedAsync(email);

        if (!otpVerified)
        {
            return BadRequest(new
            {
                message = "Please verify your email first"
            });
        }

        // Hash password
        string hashedPassword =
            BCrypt.Net.BCrypt.HashPassword(dto.Password);

        var user = new Models.User
        {
            Name = dto.Name.Trim(),
            Email = email,
            Password = hashedPassword,
            Role = "User",
            IsEmailVerified = true,
            CreatedAt = DateTime.UtcNow
        };

        await _userService.CreateAsync(user);

        return Ok(new
        {
            message = "Registration successful",
            email = user.Email
        });
    }
    catch (Exception ex)
    {
        Console.WriteLine("REGISTER ERROR: " + ex);

        return StatusCode(500, new
        {
            message = "Registration failed"
        });
    }
}
    }
    
}