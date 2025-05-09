using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MvcExample.Models{
    public class Loan{
        public int Id { get; set; }
        
        [Required]
        public int BookId { get; set; }
        
        [Required]
        [Display(Name = "User")]
        public int UserId { get; set; }
        
        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Borrowed Date")]
        public DateTime BorrowedDate { get; set; }
        
        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Due Date")]
        public DateTime DueDate { get; set; }
        
        [DataType(DataType.Date)]
        [Display(Name = "Returned Date")]
        public DateTime? ReturnedDate { get; set; }

        // Navigation properties for relationships
        [ForeignKey("BookId")]
        public virtual Book? Book { get; set; } = null!;
        
        [ForeignKey("UserId")]
        public virtual User? User { get; set; } = null!;
    }
}