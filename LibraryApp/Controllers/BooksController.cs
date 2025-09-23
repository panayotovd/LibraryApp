using LibraryApp.Helpers;
using LibraryApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace LibraryApp.Controllers
{
    public class BooksController(LibraryDbContext db) : Controller
    {
        private readonly LibraryDbContext _db = db;

        // GET: /Books
        public async Task<IActionResult> Index(
            string? q,
            int? authorId,
            int? yearFrom,
            int? yearTo,
            string? sort = "title",   // title|year|author
            string? dir = "asc",      // asc|desc
            int page = 1,
            int pageSize = 10)
        {
            var query = _db.Books.Include(b => b.Author).AsQueryable();

            // Търсене
            if (!string.IsNullOrWhiteSpace(q))
            {
                var pattern = $"%{q}%";
                query = query.Where(b =>
                    EF.Functions.Like(b.Title, pattern) ||
                    EF.Functions.Like(b.ISBN, pattern) ||
                    EF.Functions.Like(b.Author.Name, pattern));
            }

            if (authorId.HasValue) query = query.Where(b => b.AuthorId == authorId.Value);
            if (yearFrom.HasValue) query = query.Where(b => b.Year >= yearFrom.Value);
            if (yearTo.HasValue) query = query.Where(b => b.Year <= yearTo.Value);

            // Сортиране
            bool asc = string.Equals(dir, "asc", StringComparison.OrdinalIgnoreCase);
            query = (sort?.ToLower()) switch
            {
                "year" => asc ? query.OrderBy(b => b.Year) : query.OrderByDescending(b => b.Year),
                "author" => asc ? query.OrderBy(b => b.Author.Name).ThenBy(b => b.Title)
                                : query.OrderByDescending(b => b.Author.Name).ThenByDescending(b => b.Title),
                _ => asc ? query.OrderBy(b => b.Title) : query.OrderByDescending(b => b.Title)
            };

            // Пагинация
            if (page < 1) page = 1;
            if (pageSize < 5) pageSize = 5; if (pageSize > 50) pageSize = 50;

            var total = await query.CountAsync();
            var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            ViewBag.Authors = new SelectList(await _db.Authors.OrderBy(a => a.Name).ToListAsync(), "Id", "Name", authorId);

            // Изграждаме QS без page, за да работи pager-ът
            string baseQs = "?" + string.Join("&", new[]
            {
                q is null ? null : $"q={Uri.EscapeDataString(q)}",
                authorId is null ? null : $"authorId={authorId}",
                yearFrom is null ? null : $"yearFrom={yearFrom}",
                yearTo   is null ? null : $"yearTo={yearTo}",
                $"sort={sort}", $"dir={dir}", $"pageSize={pageSize}"
            }.Where(s => s != null));

            ViewBag.Pager = new LibraryApp.Models.ViewModels.PagedResult<object>
            {
                Page = page,
                PageSize = pageSize,
                TotalItems = total,
                QueryString = baseQs
            };

            ViewBag.Filters = new { q, authorId, yearFrom, yearTo, sort, dir, pageSize };

            return View(items);
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
            if (!ModelState.IsValid) { await PopulateAuthors(book.AuthorId); return View(book); }

            _db.Add(book);
            await _db.SaveChangesAsync();

            // PRG + пазим филтрите
            var route = RouteStateHelper.BuildFromRequest(Request);
            return RedirectToAction(nameof(Index), route);
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
            if (!ModelState.IsValid) { await PopulateAuthors(book.AuthorId); return View(book); }

            _db.Update(book);
            await _db.SaveChangesAsync();

            var route = RouteStateHelper.BuildFromRequest(Request);
            return RedirectToAction(nameof(Index), route);
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
            if (book != null) { _db.Books.Remove(book); await _db.SaveChangesAsync(); }

            var route = RouteStateHelper.BuildFromRequest(Request);
            return RedirectToAction(nameof(Index), route);
        }

        private async Task PopulateAuthors(int? selectedId = null)
        {
            var authors = await _db.Authors.OrderBy(a => a.Name).ToListAsync();
            ViewBag.Authors = new SelectList(authors, "Id", "Name", selectedId);
        }
    }
}