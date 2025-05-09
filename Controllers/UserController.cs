using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Cryptography;
using Konscious.Security.Cryptography;
using Microsoft.Extensions.Configuration;
using System.Text;
using MvcExample.Data;
using MvcExample.Models;

namespace web12.Controllers
{
    [Authorize(Roles = "Admin")]
    public class UserController : Controller
    {
        private readonly LibraryContext _context;
        private readonly IConfiguration _configuration;

        public UserController(LibraryContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // GET: User
        public async Task<IActionResult> Index()
        {
            return View(await _context.Users.ToListAsync());
        }

        // GET: User/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(m => m.Id == id);
            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        // GET: User/Create
        public IActionResult Create()
        {
            return View();
        }    

        // POST: User/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("FullName,Email,Username,Password,PhoneNumber,IsAdmin")] User user)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    if (_context.Users.Any(u => u.Username == user.Username))
                    {
                        ModelState.AddModelError("Username", "Username already exists.");
                        return View(user);
                    }

                    // Generate salt and hash password
                    var salt = CreateSalt();
                    if (salt == null || salt.Length == 0)
                    {
                        ModelState.AddModelError(string.Empty, "Failed to generate salt.");
                        return View(user);
                    }

                    user.Salt = salt;
                    
                    try
                    {
                        user.Password = HashPassword(user.Password, salt);
                    }
                    catch (Exception ex)
                    {
                        ModelState.AddModelError(string.Empty, $"Password hashing error: {ex.Message}");
                        return View(user);
                    }
                    
                    user.DateJoined = DateTime.UtcNow;

                    _context.Add(user);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                
                // If we got this far, something failed with ModelState
                foreach(var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    // This will output error details to help with debugging
                    Console.WriteLine($"ModelState error: {error.ErrorMessage}");
                }
            }
            catch (Exception ex)
            {
                // Handle any other exceptions
                ModelState.AddModelError(string.Empty, $"An error occurred: {ex.Message}");
            }
            
            return View(user);
        }

        // GET: User/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }
            return View(user);
        }

        // POST: User/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,FullName,Email,Username,PhoneNumber,DateJoined,IsAdmin")] User user, string? newPassword)
        {
            if (id != user.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var userToUpdate = await _context.Users.FindAsync(id);
                    if (userToUpdate == null)
                    {
                        return NotFound();
                    }

                    userToUpdate.FullName = user.FullName;
                    userToUpdate.Email = user.Email;
                    userToUpdate.Username = user.Username;
                    userToUpdate.PhoneNumber = user.PhoneNumber;
                    userToUpdate.DateJoined = user.DateJoined;
                    userToUpdate.IsAdmin = user.IsAdmin;

                    if (!string.IsNullOrEmpty(newPassword))
                    {
                        var salt = CreateSalt();
                        userToUpdate.Salt = salt;
                        userToUpdate.Password = HashPassword(newPassword, salt);
                    }

                    _context.Update(userToUpdate);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!UserExists(user.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(user);
        }

        // GET: User/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(m => m.Id == id);
            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        // POST: User/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private bool UserExists(int id)
        {
            return _context.Users.Any(e => e.Id == id);
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
            // Get the pepper from configuration with a default value to avoid null warnings
            string pepperString = _configuration["Jwt:Pepper"] ?? "DefaultPepperValue";
            
            // Convert the password to bytes
            byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
            byte[] pepperBytes = Encoding.UTF8.GetBytes(pepperString);
            
            // Combine the password with the pepper
            byte[] passwordWithPepper = new byte[passwordBytes.Length + pepperBytes.Length];
            Buffer.BlockCopy(passwordBytes, 0, passwordWithPepper, 0, passwordBytes.Length);
            Buffer.BlockCopy(pepperBytes, 0, passwordWithPepper, passwordBytes.Length, pepperBytes.Length);
            
            // Create the Argon2id hasher with the peppered password
            var argon2 = new Argon2id(passwordWithPepper);
            
            // Configure the hasher
            argon2.Salt = salt;
            argon2.DegreeOfParallelism = 4; // Lower value to reduce resource usage
            argon2.MemorySize = 32768;      // Lower memory requirement
            argon2.Iterations = 2;          // Fewer iterations for faster hashing
            
            // Generate the hash
            var hashBytes = argon2.GetBytes(32); // 32 bytes for a 256-bit hash
            
            // Convert the hash to a base64 string for storage
            return Convert.ToBase64String(hashBytes);
        }
    }
}