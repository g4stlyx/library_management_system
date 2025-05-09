using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MvcExample.Models;
using MvcExample.Data;
using Microsoft.EntityFrameworkCore;

//* return View() dersen fonksiyonla (Details) aynı isimdeki view'e gider. (Details.cshtml)
//* return redirect(Edit) dersen Edit.cshtml'e gider gider.

//! derste sadece book'la alakalı olan kısımlar işlendi. kalanlar ekstra

namespace MvcExample.Controllers{
    public class BookController : Controller{
        private readonly LibraryContext _context;

        public BookController(LibraryContext context){
            _context = context;
        }
        
        public async Task<ActionResult> Index(){
            var books = await _context.Books.ToListAsync();
            return View(books);
        }
        
        public ActionResult Create(){
            return View();
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(Book book){
            if (ModelState.IsValid){
                _context.Books.Add(book);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(book);
        }
        
        public async Task<ActionResult> Edit(int id){
            var book = await _context.Books.FindAsync(id);
            if (book == null){
                return NotFound();
            }
            return View(book);
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(int id, Book book){
            if (id != book.Id){
                return NotFound();
            }

            if (ModelState.IsValid){
                try
                {
                    _context.Update(book);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!BookExists(book.Id))
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
            return View(book);
        }
        
        public async Task<ActionResult> Delete(int id){
            var book = await _context.Books.FindAsync(id);
            if (book == null){
                return NotFound();
            }
            return View(book);
        }
        
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(int id){
            var book = await _context.Books.FindAsync(id);
            if (book != null){
                _context.Books.Remove(book);
                await _context.SaveChangesAsync();
            }
            
            return RedirectToAction(nameof(Index));
        }
        
        private bool BookExists(int id){
            return _context.Books.Any(e => e.Id == id);
        }
    }
}