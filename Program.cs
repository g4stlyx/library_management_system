using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using MvcExample.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using MvcExample.Models;
using web12.Models;
using System.Linq;
using System.Security.Cryptography;
using Konscious.Security.Cryptography;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Add DbContext using SQLite
builder.Services.AddDbContext<LibraryContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            // Try to get the token from the cookie
            context.Token = context.Request.Cookies["jwtToken"];
            return Task.CompletedTask;
        }
    };
    
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
    };
});

// builder.Services.AddAuthorization(options =>
// {
//     options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
// });
builder.Services.AddAuthorization(); // This enables [Authorize] attribute

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication(); // Ensure UseAuthentication is called before UseAuthorization
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Delete and recreate the database on startup
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<LibraryContext>();
    var configuration = services.GetRequiredService<IConfiguration>(); // Get IConfiguration
    
    // Delete and recreate database
    context.Database.EnsureDeleted();
    context.Database.EnsureCreated();

    // Seed an admin user
    if (!context.Users.Any(u => u.Username == "g4"))
    {
        var salt = CreateSalt();
        var passwordHash = HashPassword("123", salt, configuration["Jwt:Pepper"] ?? "DefaultPepperValue"); // Ensure pepper is available

        var adminUser = new User
        {
            Username = "g4",
            Password = passwordHash,
            Salt = salt,
            IsAdmin = true,
            FullName = "sefa", // Add other required fields
            Email = "sefa@g4stly.tr" // Add other required fields
        };
        context.Users.Add(adminUser);
        context.SaveChanges();
    }
}

// Helper methods for password hashing (can be moved to a service later)
byte[] CreateSalt()
{
    var buffer = new byte[16];
    using (var rng = RandomNumberGenerator.Create())
    {
        rng.GetBytes(buffer);
    }
    return buffer;
}

string HashPassword(string password, byte[] salt, string pepperString)
{
    var pepper = Encoding.UTF8.GetBytes(pepperString);
    var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password));

    argon2.Salt = salt;
    argon2.DegreeOfParallelism = 8; 
    argon2.MemorySize = 65536;    
    argon2.Iterations = 4;        

    var passwordWithPepper = Encoding.UTF8.GetBytes(password).Concat(pepper).ToArray();
    
    var argon2ForPepperedPassword = new Argon2id(passwordWithPepper);
    argon2ForPepperedPassword.Salt = salt;
    argon2ForPepperedPassword.DegreeOfParallelism = 8;
    argon2ForPepperedPassword.MemorySize = 65536;
    argon2ForPepperedPassword.Iterations = 4;

    var hashBytes = argon2ForPepperedPassword.GetBytes(32); 
    return Convert.ToBase64String(hashBytes);
}

app.Run();