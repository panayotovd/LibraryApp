using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LibraryApp.Models;

namespace LibraryApp.Controllers
{
    public class AuthorsController : Controller
    {
        private readonly LibraryDbContext _db;
        public AuthorsController(LibraryDbContext db) => _db = db;

        // GET: /Authors
        public async Task<IActionResult> Index(string q)
        {
            var query = _db.Authors.AsQueryable();
            if (!string.IsNullOrWhiteSpace(q))
                query = query.Where(a => a.Name.Contains(q));

            var model = await query
                .OrderBy(a => a.Name)
                .ToListAsync();

            ViewBag.Search = q;
            return View(model);
        }

        // GET: /Authors/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var author = await _db.Authors
                .Include(a => a.Books)
                .FirstOrDefaultAsync(a => a.Id == id);
            if (author == null) return NotFound();
            return View(author);
        }

        // GET: /Authors/Create
        public IActionResult Create() => View();

        // POST: /Authors/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Author author)
        {
            if (!ModelState.IsValid) return View(author);

            _db.Authors.Add(author);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: /Authors/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var author = await _db.Authors.FindAsync(id);
            if (author == null) return NotFound();
            return View(author);
        }

        // POST: /Authors/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Author author)
        {
            if (id != author.Id) return NotFound();
            if (!ModelState.IsValid) return View(author);

            try
            {
                _db.Update(author);
                await _db.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _db.Authors.AnyAsync(a => a.Id == id)) return NotFound();
                throw;
            }
        }

        // GET: /Authors/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var author = await _db.Authors
                .Include(a => a.Books)
                .FirstOrDefaultAsync(a => a.Id == id);
            if (author == null) return NotFound();

            if (author.Books?.Any() == true)
                ViewBag.BlockReason = "Авторът има свързани книги и не може да бъде изтрит.";

            return View(author);
        }

        // POST: /Authors/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var author = await _db.Authors
                .Include(a => a.Books)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (author == null) return RedirectToAction(nameof(Index));

            if (author.Books?.Any() == true)
            {
                TempData["Error"] = "Не може да изтриеш автор, който има книги.";
                return RedirectToAction(nameof(Delete), new { id });
            }

            _db.Authors.Remove(author);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
