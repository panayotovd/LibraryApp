using LibraryApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryApp.Controllers
{
    public class MembersController(LibraryDbContext db) : Controller
    {
        private readonly LibraryDbContext _db = db;

        // GET: /Members
        public async Task<IActionResult> Index(
            string? q,
            string? sort = "name",   // name|email|joined
            string? dir = "asc",
            int page = 1,
            int pageSize = 10)
        {
            var query = _db.Members.AsNoTracking().AsQueryable();

            // Търсене по име или email
            if (!string.IsNullOrWhiteSpace(q))
            {
                var pattern = $"%{q}%";
                query = query.Where(m =>
                    EF.Functions.Like(m.FullName, pattern) ||
                    EF.Functions.Like(m.Email, pattern));
            }

            // Сортиране
            bool asc = string.Equals(dir, "asc", StringComparison.OrdinalIgnoreCase);
            query = (sort?.ToLower()) switch
            {
                "email" => asc ? query.OrderBy(m => m.Email) : query.OrderByDescending(m => m.Email),
                "joined" => asc ? query.OrderBy(m => m.JoinedAt) : query.OrderByDescending(m => m.JoinedAt),
                _ => asc ? query.OrderBy(m => m.FullName) : query.OrderByDescending(m => m.FullName)
            };

            // Пагинация
            if (page < 1) page = 1;
            if (pageSize < 5) pageSize = 5; if (pageSize > 50) pageSize = 50;

            var total = await query.CountAsync();
            var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

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


        // GET: /Members/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            // по желание можеш да Include-неш участията в събития
            var member = await _db.Members.FirstOrDefaultAsync(m => m.Id == id);
            if (member == null) return NotFound();
            return View(member);
        }

        // GET: /Members/Create
        public IActionResult Create() => View();

        // POST: /Members/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Member member)
        {
            if (!ModelState.IsValid) return View(member);

            _db.Members.Add(member);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: /Members/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var member = await _db.Members.FindAsync(id);
            if (member == null) return NotFound();
            return View(member);
        }

        // POST: /Members/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Member member)
        {
            if (id != member.Id) return NotFound();
            if (!ModelState.IsValid) return View(member);

            try
            {
                _db.Update(member);
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _db.Members.AnyAsync(m => m.Id == id)) return NotFound();
                throw;
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: /Members/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var member = await _db.Members.FirstOrDefaultAsync(m => m.Id == id);
            if (member == null) return NotFound();

            // по желание: блокирай, ако участва в събитие (лесно е ако имаш навигация Member.EventMembers)
            return View(member);
        }

        // POST: /Members/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var member = await _db.Members.FindAsync(id);
            if (member != null)
            {
                _db.Members.Remove(member);
                await _db.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
