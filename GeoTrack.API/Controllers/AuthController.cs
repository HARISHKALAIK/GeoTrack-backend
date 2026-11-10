using Microsoft.AspNetCore.Mvc;
using GeoTrack.API.DTOs;
using GeoTrack.API.Services;
using GeoTrack.API.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace GeoTrack.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly OtpService _otpService;
        private readonly EmailService _emailService;
        private readonly UserService _userService;
        private readonly IConfiguration _configuration;

        public AuthController(
            OtpService otpService,
            EmailService emailService,
            UserService userService,
            IConfiguration configuration)
        {
            _otpService = otpService;
            _emailService = emailService;
            _userService = userService;
            _configuration = configuration;
        }

        // ==========================================
        // 1. SEND OTP
        // ==========================================

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

                var existingUser = await _userService.GetByEmailAsync(email);

                if (existingUser != null)
                {
                    return BadRequest(new
                    {
                        message = "An account already exists with this email"
                    });
                }

                string otp = _otpService.GenerateOtp();

                await _otpService.SaveOtpAsync(email, otp);

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
                Console.WriteLine("OTP ERROR: " + ex);

                return StatusCode(500, new
                {
                    message = "Unable to send OTP"
                });
            }
        }

        // ==========================================
        // 2. VERIFY OTP
        // ==========================================

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

        // ==========================================
        // 3. REGISTER USER
        // ==========================================

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Name) ||
                    string.IsNullOrWhiteSpace(dto.Email) ||
                    string.IsNullOrWhiteSpace(dto.Password) ||
                    string.IsNullOrWhiteSpace(dto.ConfirmPassword))
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

                if (dto.Password.Length < 8)
                {
                    return BadRequest(new
                    {
                        message = "Password must contain at least 8 characters"
                    });
                }

                string email = dto.Email.Trim().ToLowerInvariant();

                bool exists = await _userService.EmailExistsAsync(email);

                if (exists)
                {
                    return BadRequest(new
                    {
                        message = "Email already registered"
                    });
                }

                bool otpVerified = await _otpService.IsEmailVerifiedAsync(email);

                if (!otpVerified)
                {
                    return BadRequest(new
                    {
                        message = "Please verify your email first"
                    });
                }

                string hashedPassword =
                    BCrypt.Net.BCrypt.HashPassword(dto.Password);

                var user = new User
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

        // ==========================================
        // 4. LOGIN WITH JWT
        // ==========================================

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Email) ||
                    string.IsNullOrWhiteSpace(dto.Password))
                {
                    return BadRequest(new
                    {
                        message = "Email and password are required"
                    });
                }

                string email = dto.Email.Trim().ToLowerInvariant();

                // Find user in MongoDB
                var user = await _userService.GetByEmailAsync(email);

                if (user == null)
                {
                    return Unauthorized(new
                    {
                        message = "Invalid email or password"
                    });
                }

                // Verify password using BCrypt
                bool passwordValid =
                    BCrypt.Net.BCrypt.Verify(dto.Password, user.Password);

                if (!passwordValid)
                {
                    return Unauthorized(new
                    {
                        message = "Invalid email or password"
                    });
                }

                if (!user.IsEmailVerified)
                {
                    return Unauthorized(new
                    {
                        message = "Please verify your email first"
                    });
                }

                // Create JWT claims
                var claims = new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id!),
                    new Claim(ClaimTypes.Name, user.Name),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Role, user.Role)
                };

                // Read JWT configuration
                var jwtKey = _configuration["Jwt:Key"]
                    ?? throw new InvalidOperationException("JWT Key missing");

                var issuer = _configuration["Jwt:Issuer"];
                var audience = _configuration["Jwt:Audience"];

                var expiryMinutes = Convert.ToDouble(
                    _configuration["Jwt:ExpiryMinutes"] ?? "60"
                );

                var key = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwtKey)
                );

                var credentials = new SigningCredentials(
                    key,
                    SecurityAlgorithms.HmacSha256
                );

                // Generate JWT token
                var token = new JwtSecurityToken(
                    issuer: issuer,
                    audience: audience,
                    claims: claims,
                    expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
                    signingCredentials: credentials
                );

                string jwtToken =
                    new JwtSecurityTokenHandler().WriteToken(token);

                return Ok(new
                {
                    message = "Login successful",
                    token = jwtToken,
                    expiresInMinutes = expiryMinutes,

                    user = new
                    {
                        id = user.Id,
                        name = user.Name,
                        email = user.Email,
                        role = user.Role
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine("LOGIN ERROR: " + ex);

                return StatusCode(500, new
                {
                    message = "Login failed"
                });
            }
        }
    }
}