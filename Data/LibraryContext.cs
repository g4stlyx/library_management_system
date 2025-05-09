using Microsoft.EntityFrameworkCore;
using MvcExample.Models;

namespace MvcExample.Data{
    public class LibraryContext : DbContext{
        public LibraryContext(DbContextOptions<LibraryContext> options) : base(options){
        }

        public DbSet<Book> Books { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Loan> Loans { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder){
            base.OnModelCreating(modelBuilder);

            // Configure relationships
            modelBuilder.Entity<Loan>()
                .HasOne(l => l.Book)
                .WithMany(b => b.Loans)
                .HasForeignKey(l => l.BookId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Loan>()
                .HasOne(l => l.User)
                .WithMany(u => u.Loans)
                .HasForeignKey(l => l.UserId)
                .OnDelete(DeleteBehavior.Restrict);
                
            // Add some default data if needed
            // modelBuilder.Entity<User>().HasData(
            //    new User { Id = 1, FullName = "Admin User", Email = "admin@library.com", Username = "admin", Password = "Admin123", IsAdmin = true }
            // );
        }
    }
}