using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MvcExample.Models
{
    public class User
    {        
        public User()
        {
            DateJoined = DateTime.Now;
            IsAdmin = false;
            // Initialize properties to avoid null reference warnings
            Loans = new HashSet<Loan>();
            FullName = string.Empty;
            Email = string.Empty;
            Username = string.Empty;
            Password = string.Empty;
            PhoneNumber = string.Empty;
            Salt = Array.Empty<byte>(); // Initialize Salt with an empty array
        }

        public int Id { get; set; }
        
        [Required]
        public string FullName { get; set; }
        
        [Required]
        [EmailAddress]
        public string Email { get; set; }
        
        [Required]
        public string Username { get; set; }
        
        public string PhoneNumber { get; set; }
        
        public DateTime DateJoined { get; set; }
        
        public bool IsAdmin { get; set; }
        
        [Required]
        [DataType(DataType.Password)]
        public string Password{ get; set; }
        
        public byte[] Salt { get; set; }
        
        // Navigation property for relationship
        public virtual ICollection<Loan> Loans { get; set; }
    }
}