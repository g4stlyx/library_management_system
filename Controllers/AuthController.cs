using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Konscious.Security.Cryptography;
using MvcExample.Data;
using MvcExample.Models;
using web12.Models;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Collections.Generic;

namespace web12.Controllers
{
    public class AuthController : Controller
    {
        private readonly LibraryContext _context;
        private readonly IConfiguration _configuration;

        public AuthController(LibraryContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpGet("register")]
        public IActionResult Register()
        {
            return View();
        }

        [HttpGet("login")]
        public IActionResult Login()
        {
            return View();
        }

        [HttpGet("logout")]
        public IActionResult Logout()
        {
            // Clear the cookie-based token if you're using it
            Response.Cookies.Delete("jwtToken");
            return RedirectToAction("Index", "Home");
        }        
        
        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDto registerDto)
        {
            if (_context.Users.Any(u => u.Username == registerDto.Username))
            {
                ModelState.AddModelError("Username", "Username already exists.");
                return View(registerDto);
            }

            var salt = CreateSalt();
            var passwordHash = HashPassword(registerDto.Password, salt);

            var user = new User
            {
                Username = registerDto.Username,
                FullName = registerDto.Username, // Default to username if fullname not provided
                Email = $"{registerDto.Username}@example.com", // Default email
                Password = passwordHash, // Changed from PasswordHash
                Salt = salt,
                DateJoined = DateTime.UtcNow,
                IsAdmin = false // Default to regular user
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Login));
        }        
        
        [HttpPost("login")]
        public IActionResult Login(LoginDto loginDto)
        {
            var user = _context.Users.SingleOrDefault(u => u.Username == loginDto.Username);

            if (user == null || !VerifyPassword(loginDto.Password, user.Salt, user.Password)) // Changed from user.PasswordHash
            {
                ModelState.AddModelError(string.Empty, "Invalid username or password");
                return View(loginDto);
            }

            var token = GenerateJwtToken(user);
            
            // Store the token in a cookie
            Response.Cookies.Append("jwtToken", token, new CookieOptions 
            { 
                HttpOnly = true,
                Expires = DateTime.UtcNow.AddMinutes(120)
            });
            
            return RedirectToAction("Index", "Home");
        }

        private byte[] CreateSalt()
        {
            var buffer = new byte[16];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(buffer);
            }
            return buffer;
        }        
        
        private string HashPassword(string password, byte[] salt)
        {
            var pepper = Encoding.UTF8.GetBytes(_configuration["Jwt:Pepper"] ?? "DefaultPepperValue");
            
            // Combine password with pepper before hashing
            var passwordWithPepper = Encoding.UTF8.GetBytes(password).Concat(pepper).ToArray();
            
            // Create an Argon2id instance with the peppered password
            var argon2 = new Argon2id(passwordWithPepper);
            argon2.Salt = salt;
            argon2.DegreeOfParallelism = 8; // Adjust as needed
            argon2.MemorySize = 65536;      // Adjust as needed (KB)
            argon2.Iterations = 4;          // Adjust as needed

            var hashBytes = argon2.GetBytes(32); // 32 bytes for a 256-bit hash
            return Convert.ToBase64String(hashBytes);
        }        
        
        private bool VerifyPassword(string password, byte[] salt, string storedHash)
        {
            var pepper = Encoding.UTF8.GetBytes(_configuration["Jwt:Pepper"] ?? "DefaultPepperValue");
            var passwordWithPepper = Encoding.UTF8.GetBytes(password).Concat(pepper).ToArray();
            var argon2 = new Argon2id(passwordWithPepper);

            argon2.Salt = salt;
            argon2.DegreeOfParallelism = 8;
            argon2.MemorySize = 65536;
            argon2.Iterations = 4;

            var hashBytes = argon2.GetBytes(32);
            var newHash = Convert.ToBase64String(hashBytes);
            return newHash == storedHash;
        }

        private string GenerateJwtToken(User user)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim> // Changed to List<Claim> to conditionally add role
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Username),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim("userId", user.Id.ToString())
            };

            if (user.IsAdmin)
            {
                claims.Add(new Claim(ClaimTypes.Role, "Admin"));
            }

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(120),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
