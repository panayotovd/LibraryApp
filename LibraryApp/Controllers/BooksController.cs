using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using LibraryApp.Data;
using LibraryApp.Models;
using LibraryApp.Models.ViewModels;

namespace LibraryApp.Controllers
{
    [Authorize]
    public class BooksController : Controller
    {
        private readonly LibraryDbContext _db;
        public BooksController(LibraryDbContext db) => _db = db;

        [AllowAnonymous]
        public async Task<IActionResult> Index(
            string? q, int? authorId, int? yearFrom, int? yearTo,
            string? sort = "title", string? dir = "asc",
            int page = 1, int pageSize = 10)
        {
            var query = _db.Books.AsNoTracking().Include(b => b.Author).AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                var p = $"%{q}%";
                query = query.Where(b => EF.Functions.Like(b.Title, p) || EF.Functions.Like(b.Author!.Name, p));
            }
            if (authorId.HasValue) query = query.Where(b => b.AuthorId == authorId);
            if (yearFrom.HasValue) query = query.Where(b => b.Year >= yearFrom);
            if (yearTo.HasValue) query = query.Where(b => b.Year <= yearTo);

            bool asc = string.Equals(dir, "asc", StringComparison.OrdinalIgnoreCase);
            query = (sort?.ToLower()) switch
            {
                "year" => asc ? query.OrderBy(b => b.Year) : query.OrderByDescending(b => b.Year),
                "author" => asc ? query.OrderBy(b => b.Author!.Name).ThenBy(b => b.Title)
                                : query.OrderByDescending(b => b.Author!.Name).ThenByDescending(b => b.Title),
                _ => asc ? query.OrderBy(b => b.Title) : query.OrderByDescending(b => b.Title)
            };

            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 5, 50);

            var total = await query.CountAsync();
            var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            ViewBag.Authors = new SelectList(await _db.Authors.OrderBy(a => a.Name).ToListAsync(), "Id", "Name", authorId);

            string baseQs = "?" + string.Join("&", new[]
            {
                q is null ? null : $"q={Uri.EscapeDataString(q)}",
                authorId is null ? null : $"authorId={authorId}",
                yearFrom is null ? null : $"yearFrom={yearFrom}",
                yearTo   is null ? null : $"yearTo={yearTo}",
                $"sort={sort}", $"dir={dir}", $"pageSize={pageSize}"
            }.Where(s => s != null));

            ViewBag.Pager = new PagedResult<object> { Page = page, PageSize = pageSize, TotalItems = total, QueryString = baseQs };
            return View(items);
        }

        [AllowAnonymous]
        public async Task<IActionResult> Details(int? id)
        {
            if (id is null) return NotFound();
            var book = await _db.Books.Include(b => b.Author).FirstOrDefaultAsync(m => m.Id == id);
            if (book is null) return NotFound();
            return View(book);
        }

        [Authorize(Policy = "CanWrite")]
        public async Task<IActionResult> Create()
        {
            await PopulateAuthors();
            return View(new Book());
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Policy = "CanWrite")]
        public async Task<IActionResult> Create([Bind("Title,AuthorId,Year")] Book book)
        {
            if (!ModelState.IsValid) { await PopulateAuthors(book.AuthorId); return View(book); }
            _db.Books.Add(book);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Книга: успешно създадено.";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Policy = "CanWrite")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id is null) return NotFound();
            var book = await _db.Books.FindAsync(id.Value);
            if (book is null) return NotFound();
            await PopulateAuthors(book.AuthorId);
            return View(book);
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Policy = "CanWrite")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,AuthorId,Year")] Book book)
        {
            if (id != book.Id) return NotFound();
            if (!ModelState.IsValid) { await PopulateAuthors(book.AuthorId); return View(book); }
            _db.Entry(book).State = EntityState.Modified;
            await _db.SaveChangesAsync();
            TempData["Success"] = "Книга: обновено.";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Policy = "CanWrite")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id is null) return NotFound();
            var book = await _db.Books.Include(b => b.Author).FirstOrDefaultAsync(m => m.Id == id);
            if (book is null) return NotFound();
            return View(book);
        }

        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        [Authorize(Policy = "CanWrite")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var book = await _db.Books.FindAsync(id);
            if (book != null) { _db.Books.Remove(book); await _db.SaveChangesAsync(); TempData["Success"] = "Книга: изтрито."; }
            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateAuthors(int? selectedId = null)
        {
            var authors = await _db.Authors.AsNoTracking().OrderBy(a => a.Name).ToListAsync();
            ViewBag.Authors = new SelectList(authors, "Id", "Name", selectedId);
        }
    }
}
