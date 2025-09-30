using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LibraryApp.Data;
using LibraryApp.Models;
using LibraryApp.Models.ViewModels;

namespace LibraryApp.Controllers
{
    [Authorize]
    public class MembersController : Controller
    {
        private readonly LibraryDbContext _db;
        public MembersController(LibraryDbContext db) => _db = db;

        [AllowAnonymous]
        public async Task<IActionResult> Index(string? q, string? sort = "name", string? dir = "asc", int page = 1, int pageSize = 10)
        {
            var query = _db.Members.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                var p = $"%{q}%";
                query = query.Where(m => EF.Functions.Like(m.Name, p) || EF.Functions.Like(m.Email, p));
            }

            bool asc = string.Equals(dir, "asc", StringComparison.OrdinalIgnoreCase);
            query = (sort?.ToLower()) switch
            {
                "email" => asc ? query.OrderBy(m => m.Email) : query.OrderByDescending(m => m.Email),
                "joined" => asc ? query.OrderBy(m => m.JoinedAt) : query.OrderByDescending(m => m.JoinedAt),
                "name" => asc ? query.OrderBy(m => m.Name) : query.OrderByDescending(m => m.Name),
                _ => asc ? query.OrderBy(m => m.Id) : query.OrderByDescending(m => m.Id)
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
            var member = await _db.Members.FirstOrDefaultAsync(m => m.Id == id);
            if (member is null) return NotFound();
            return View(member);
        }

        [Authorize(Policy = "CanWrite")]
        public IActionResult Create() => View(new Member { JoinedAt = DateTime.UtcNow });

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Policy = "CanWrite")]
        public async Task<IActionResult> Create([Bind("Name,Email,JoinedAt")] Member member)
        {
            if (!ModelState.IsValid) return View(member);
            _db.Members.Add(member);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Член: създаден.";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Policy = "CanWrite")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id is null) return NotFound();
            var member = await _db.Members.FindAsync(id.Value);
            if (member is null) return NotFound();
            return View(member);
        }

        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Policy = "CanWrite")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Email,JoinedAt")] Member member)
        {
            if (id != member.Id) return NotFound();
            if (!ModelState.IsValid) return View(member);
            _db.Entry(member).State = EntityState.Modified;
            await _db.SaveChangesAsync();
            TempData["Success"] = "Член: обновен.";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Policy = "CanWrite")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id is null) return NotFound();
            var member = await _db.Members.FirstOrDefaultAsync(m => m.Id == id);
            if (member is null) return NotFound();
            return View(member);
        }

        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        [Authorize(Policy = "CanWrite")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var member = await _db.Members.FindAsync(id);
            if (member != null) { _db.Members.Remove(member); await _db.SaveChangesAsync(); TempData["Success"] = "Член: изтрит."; }
            return RedirectToAction(nameof(Index));
        }
    }
}
