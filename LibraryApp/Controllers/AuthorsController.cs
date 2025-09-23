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
        public async Task<IActionResult> Index(
            string? q,
            string? sort = "name",   // name|id
            string? dir = "asc",     // asc|desc
            int page = 1,
            int pageSize = 10)
        {
            var query = _db.Authors.AsNoTracking().AsQueryable();

            // Търсене по име
            if (!string.IsNullOrWhiteSpace(q))
            {
                var pattern = $"%{q}%";
                query = query.Where(a => EF.Functions.Like(a.Name, pattern));
            }

            // Сортиране
            bool asc = string.Equals(dir, "asc", StringComparison.OrdinalIgnoreCase);
            query = (sort?.ToLower()) switch
            {
                "id" => asc ? query.OrderBy(a => a.Id) : query.OrderByDescending(a => a.Id),
                _ => asc ? query.OrderBy(a => a.Name) : query.OrderByDescending(a => a.Name)
            };

            // Пагинация
            if (page < 1) page = 1;
            if (pageSize < 5) pageSize = 5; if (pageSize > 50) pageSize = 50;

            var total = await query.CountAsync();
            var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            // Query string за pager
            string baseQs = "?" + string.Join("&", new[]
            {
                q is null ? null : $"q={Uri.EscapeDataString(q)}",
                $"sort={sort}", $"dir={dir}", $"pageSize={pageSize}"
            }.Where(s => s != null));

            ViewBag.Pager = new LibraryApp.Models.ViewModels.PagedResult<object>
            {
                Page = page,
                PageSize = pageSize,
                TotalItems = total,
                QueryString = baseQs
            };
            ViewBag.Filters = new { q, sort, dir, pageSize };

            return View(items);
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
