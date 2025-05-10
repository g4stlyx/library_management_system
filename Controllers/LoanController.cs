using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MvcExample.Data;
using MvcExample.Models;
using MvcExample.Utils;
using MvcExample.Filters;

namespace MvcExample.Controllers
{
    public class LoanController : Controller
    {
        private readonly LibraryContext _context;

        public LoanController(LibraryContext context)
        {
            _context = context;
        }

        // GET: Loan - Admin only
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index()
        {
            var loans = await _context.Loans
                .Include(l => l.Book)
                .Include(l => l.User)
                .ToListAsync();
            return View(loans);
        }

        // GET: Loan/MyLoans
        [Microsoft.AspNetCore.Authorization.Authorize]
        public async Task<IActionResult> MyLoans()
        {
            // Get the current user's ID from the JWT token claims
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "userId");

            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Unauthorized();
            }

            var loans = await _context.Loans
                .Include(l => l.Book)
                .Include(l => l.User)
                .Where(l => l.UserId == userId)
                .ToListAsync();

            return View(loans);
        }

        // GET: Loan/Borrow/5
        [Microsoft.AspNetCore.Authorization.Authorize]
        public async Task<IActionResult> Borrow(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var book = await _context.Books.FindAsync(id);
            if (book == null || book.AvailableCopies <= 0)
            {
                TempData["ErrorMessage"] = "This book is not available for borrowing.";
                return RedirectToAction("Index", "Book");
            }

            // Get the current user's ID from the JWT token claims
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "userId");
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Unauthorized();
            }

            // Create a new loan
            var loan = new Loan
            {
                BookId = book.Id,
                UserId = userId,
                BorrowedDate = DateTime.Now,
                DueDate = DateTime.Now.AddDays(14)  // 2 weeks loan period
            };

            // Reduce available copies
            book.AvailableCopies--;

            _context.Loans.Add(loan);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"You have successfully borrowed '{book.Title}'. It is due on {loan.DueDate:yyyy-MM-dd}.";
            return RedirectToAction(nameof(MyLoans));
        }
        // GET: Loan/Details/5
        // [Microsoft.AspNetCore.Authorization.Authorize]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var loan = await _context.Loans
                .Include(l => l.Book)
                .Include(l => l.User)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (loan == null)
            {
                return NotFound();
            }

            // Check if the user has access to this loan
            var accessCheck = AuthUtils.EnsureUserAccess(this, User, loan.UserId);
            if (accessCheck != null)
            {
                return accessCheck;
            }

            return View(loan);
        }

        // GET: Loan/Create
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            ViewData["BookId"] = new SelectList(_context.Books.Where(b => b.AvailableCopies > 0), "Id", "Title");
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "FullName");
            return View();
        }

        // POST: Loan/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([Bind("Id,BookId,UserId,BorrowedDate,DueDate")] Loan loan)
        {
            if (ModelState.IsValid)
            {
                var book = await _context.Books.FindAsync(loan.BookId);
                if (book != null && book.AvailableCopies > 0)
                {
                    book.AvailableCopies--;
                    _context.Add(loan);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    ModelState.AddModelError("BookId", "No available copies of this book.");
                }
            }

            ViewData["BookId"] = new SelectList(_context.Books.Where(b => b.AvailableCopies > 0), "Id", "Title", loan.BookId);
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "FullName", loan.UserId);
            return View(loan);
        }

        // GET: Loan/Edit/5
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var loan = await _context.Loans.FindAsync(id);
            if (loan == null)
            {
                return NotFound();
            }

            ViewData["BookId"] = new SelectList(_context.Books, "Id", "Title", loan.BookId);
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "FullName", loan.UserId);
            return View(loan);
        }

        // POST: Loan/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,BookId,UserId,BorrowedDate,DueDate,ReturnedDate")] Loan loan)
        {
            if (id != loan.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // If the book is being returned now
                    var originalLoan = await _context.Loans.AsNoTracking().FirstOrDefaultAsync(l => l.Id == id);
                    if (originalLoan != null && originalLoan.ReturnedDate == null && loan.ReturnedDate != null)
                    {
                        // Increment available copies when a book is returned
                        var book = await _context.Books.FindAsync(loan.BookId);
                        if (book != null)
                        {
                            book.AvailableCopies++;
                        }
                    }

                    _context.Update(loan);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!LoanExists(loan.Id))
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

            ViewData["BookId"] = new SelectList(_context.Books, "Id", "Title", loan.BookId);
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "FullName", loan.UserId);
            return View(loan);
        }

        // GET: Loan/Return/5
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
        public async Task<IActionResult> Return(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var loan = await _context.Loans
                .Include(l => l.Book)
                .Include(l => l.User)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (loan == null || loan.ReturnedDate != null)
            {
                return NotFound();
            }

            return View(loan);
        }

        // POST: Loan/Return/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
        public async Task<IActionResult> ReturnConfirmed(int id)
        {
            var loan = await _context.Loans.FindAsync(id);
            if (loan != null && loan.ReturnedDate == null)
            {
                loan.ReturnedDate = DateTime.Now;

                // Increment available copies when a book is returned
                var book = await _context.Books.FindAsync(loan.BookId);
                if (book != null)
                {
                    book.AvailableCopies++;
                }

                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Loan/ReturnMy/5
        [Microsoft.AspNetCore.Authorization.Authorize]
        public async Task<IActionResult> ReturnMy(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // Get the current user's ID
            if (!AuthUtils.TryGetUserId(User, out int userId))
            {
                return Unauthorized();
            }

            var loan = await _context.Loans
                .Include(l => l.Book)
                .Include(l => l.User)
                .FirstOrDefaultAsync(m => m.Id == id && m.UserId == userId);

            if (loan == null || loan.ReturnedDate != null)
            {
                TempData["ErrorMessage"] = "The requested loan was not found or already returned.";
                return RedirectToAction(nameof(MyLoans));
            }

            return View(loan);
        }

        // POST: Loan/ReturnMyConfirmed/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public async Task<IActionResult> ReturnMyConfirmed(int id)
        {
            // Get the current user's ID
            if (!AuthUtils.TryGetUserId(User, out int userId))
            {
                return Unauthorized();
            }

            var loan = await _context.Loans
                .Include(l => l.Book)
                .FirstOrDefaultAsync(m => m.Id == id && m.UserId == userId);

            if (loan == null || loan.ReturnedDate != null)
            {
                TempData["ErrorMessage"] = "The requested loan was not found or already returned.";
                return RedirectToAction(nameof(MyLoans));
            }

            // Mark as returned
            loan.ReturnedDate = DateTime.Now;

            // Increment available copies
            var book = await _context.Books.FindAsync(loan.BookId);
            if (book != null)
            {
                book.AvailableCopies++;
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"You have successfully returned '{loan.Book.Title}'.";
            return RedirectToAction(nameof(MyLoans));
        }

        // GET: Loan/Delete/5
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var loan = await _context.Loans
                .Include(l => l.Book)
                .Include(l => l.User)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (loan == null)
            {
                return NotFound();
            }

            return View(loan);
        }

        // POST: Loan/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var loan = await _context.Loans
                .Include(l => l.Book)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (loan != null)
            {
                // If the book hasn't been returned, make it available again
                if (loan.ReturnedDate == null)
                {
                    var book = await _context.Books.FindAsync(loan.BookId);
                    if (book != null)
                    {
                        book.AvailableCopies++;
                    }
                }

                _context.Loans.Remove(loan);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private bool LoanExists(int id)
        {
            return _context.Loans.Any(e => e.Id == id);
        }
    }
}