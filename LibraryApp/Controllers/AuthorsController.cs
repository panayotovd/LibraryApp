using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LibraryApp.Data;
using LibraryApp.Models;
using LibraryApp.Models.ViewModels;

namespace LibraryApp.Controllers
{
    [Authorize]
    public class AuthorsController : Controller
    {
        private readonly LibraryDbContext _db;
        public AuthorsController(LibraryDbContext db) => _db = db;

        [AllowAnonymous]
        public async Task<IActionResult> Index(string? q, string? sort = "name", string? dir = "asc", int page = 1, int pageSize = 10)
        {
            var query = _db.Authors.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                var p = $"%{q}%";
                query = query.Where(a => EF.Functions.Like(a.Name, p));
            }

            bool asc = string.Equals(dir, "asc", StringComparison.OrdinalIgnoreCase);
            query = (sort?.ToLower()) switch
            {
                "name" => asc ? query.OrderBy(a => a.Name) : query.OrderByDescending(a => a.Name),
                _ => asc ? query.OrderBy(a => a.Id) : query.OrderByDescending(a => a.Id)
            };

            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 5, 50);

            var total = await query.CountAsync();
            var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            ViewBag.Pager = new PagedResult<object> { Page = page, PageSize = pageSize, TotalItems = total, QueryString = $"?q={q}&sort={sort}&dir={dir}&pageSize={pageSize}" };
            return View(items);
        }

        [AllowAnonymous]
        public async Task<IActionResult> Details(int? id)
        {
            if (id is null) return NotFound();
            var author = await _db.Authors.FirstOrDefaultAsync(a => a.Id == id);
            if (author is null) return NotFound();

            ViewBag.Books = await _db.Books.AsNoTracking()
                .Where(b => b.AuthorId == id)
                .OrderBy(b => b.Title)
                .ToListAsync();

            return View(author);
        }

        [Authorize(Policy = "CanWrite")]
        public IActionResult Create() => View();

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Policy = "CanWrite")]
        public async Task<IActionResult> Create([Bind("Name")] Author author)
        {
            if (!ModelState.IsValid) return View(author);
            _db.Authors.Add(author);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Автор: създаден.";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Policy = "CanWrite")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id is null) return NotFound();
            var author = await _db.Authors.FindAsync(id.Value);
            if (author is null) return NotFound();
            return View(author);
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Policy = "CanWrite")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name")] Author author)
        {
            if (id != author.Id) return NotFound();
            if (!ModelState.IsValid) return View(author);
            _db.Entry(author).State = EntityState.Modified;
            await _db.SaveChangesAsync();
            TempData["Success"] = "Автор: обновен.";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Policy = "CanWrite")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id is null) return NotFound();
            var author = await _db.Authors.FirstOrDefaultAsync(a => a.Id == id);
            if (author is null) return NotFound();
            return View(author);
        }

        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        [Authorize(Policy = "CanWrite")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var author = await _db.Authors.FindAsync(id);
            if (author == null) return RedirectToAction(nameof(Index));

            var booksCount = await _db.Books.CountAsync(b => b.AuthorId == id);
            if (booksCount > 0)
            {
                TempData["Error"] = $"Авторът има свързани книги. Първо ги прехвърли на друг автор или изтрий тези книги.";
                return RedirectToAction(nameof(Details), new { id });
            }

            _db.Authors.Remove(author);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Автор: изтрит.";
            return RedirectToAction(nameof(Index));
        }
    }
}
