using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MvcExample.Models
{
    public class Book
    {
        public Book()
        {
            // Initialize the collection to avoid null reference warnings
            Loans = new HashSet<Loan>();
            Title = string.Empty;
            Author = string.Empty;
            ISBN = string.Empty;
            Publisher = string.Empty;
            Category = string.Empty;
        }

        public int Id { get; set; }
        
        [Required]
        public string Title { get; set; }
        
        [Required]
        public string Author { get; set; }
        
        public string ISBN { get; set; }
        
        public string Publisher { get; set; }
        
        public int PublishedYear { get; set; }
        
        public int TotalCopies { get; set; }
        
        public int AvailableCopies { get; set; }
        
        public string Category { get; set; }

        // Navigation property for relationship
        public virtual ICollection<Loan> Loans { get; set; }
    }
}