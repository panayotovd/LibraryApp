using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using LibraryApp.Models;

namespace LibraryApp.Controllers
{
    public class BooksController(LibraryDbContext db) : Controller
    {
        private readonly LibraryDbContext _db = db;

        // GET: /Books
        public async Task<IActionResult> Index(string q, int? authorId)
        {
            var query = _db.Books
                .Include(b => b.Author)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
                query = query.Where(b => b.Title.Contains(q) || b.ISBN.Contains(q));

            if (authorId.HasValue)
                query = query.Where(b => b.AuthorId == authorId.Value);

            ViewBag.Authors = new SelectList(await _db.Authors.OrderBy(a => a.Name).ToListAsync(), "Id", "Name", authorId);
            ViewBag.Search = q;

            var model = await query.OrderBy(b => b.Title).ToListAsync();
            return View(model);
        }

        // GET: /Books/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var book = await _db.Books.Include(b => b.Author).FirstOrDefaultAsync(m => m.Id == id);
            if (book == null) return NotFound();

            return View(book);
        }

        // GET: /Books/Create
        public async Task<IActionResult> Create()
        {
            await PopulateAuthors();
            return View();
        }

        // POST: /Books/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Book book)
        {
            if (!ModelState.IsValid)
            {
                await PopulateAuthors(book.AuthorId);
                return View(book);
            }

            _db.Add(book);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: /Books/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var book = await _db.Books.FindAsync(id);
            if (book == null) return NotFound();

            await PopulateAuthors(book.AuthorId);
            return View(book);
        }

        // POST: /Books/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Book book)
        {
            if (id != book.Id) return NotFound();

            if (!ModelState.IsValid)
            {
                await PopulateAuthors(book.AuthorId);
                return View(book);
            }

            try
            {
                _db.Update(book);
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _db.Books.AnyAsync(e => e.Id == id)) return NotFound();
                throw;
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: /Books/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var book = await _db.Books.Include(b => b.Author).FirstOrDefaultAsync(m => m.Id == id);
            if (book == null) return NotFound();
            return View(book);
        }

        // POST: /Books/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var book = await _db.Books.FindAsync(id);
            if (book != null)
            {
                _db.Books.Remove(book);
                await _db.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateAuthors(int? selectedId = null)
        {
            var authors = await _db.Authors.OrderBy(a => a.Name).ToListAsync();
            ViewBag.Authors = new SelectList(authors, "Id", "Name", selectedId);
        }
    }
}