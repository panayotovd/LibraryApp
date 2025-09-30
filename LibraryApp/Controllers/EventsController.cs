using System;
using System.Linq;
using System.Threading.Tasks;
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
    [Route("Events")]
    public class EventsController : Controller
    {
        private readonly LibraryDbContext _db;
        public EventsController(LibraryDbContext db) => _db = db;

        // GET /Events
        [AllowAnonymous]
        [HttpGet("")]
        public async Task<IActionResult> Index(
            string? q, DateTime? from, DateTime? to,
            string? sort = "start", string? dir = "asc",
            int page = 1, int pageSize = 10)
        {
            var query = _db.Events
                .AsNoTracking()
                .Include(e => e.EventMembers)
                .ThenInclude(em => em.Member)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                var p = $"%{q}%";
                query = query.Where(e =>
                    EF.Functions.Like(e.Title, p) ||
                    EF.Functions.Like(e.Description!, p));
            }
            if (from.HasValue) query = query.Where(e => e.StartAt >= from.Value);
            if (to.HasValue) query = query.Where(e => e.StartAt <= to.Value);

            bool asc = string.Equals(dir, "asc", StringComparison.OrdinalIgnoreCase);
            query = (sort?.ToLower()) switch
            {
                "title" => asc ? query.OrderBy(e => e.Title) : query.OrderByDescending(e => e.Title),
                "members" => asc ? query.OrderBy(e => e.EventMembers.Count) : query.OrderByDescending(e => e.EventMembers.Count),
                _ => asc ? query.OrderBy(e => e.StartAt) : query.OrderByDescending(e => e.StartAt)
            };

            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 5, 50);

            var total = await query.CountAsync();
            var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            string baseQs = "?" + string.Join("&", new[]
            {
                q is null ? null : $"q={Uri.EscapeDataString(q)}",
                from is null ? null : $"from={from:yyyy-MM-dd}",
                to   is null ? null : $"to={to:yyyy-MM-dd}",
                $"sort={sort}", $"dir={dir}", $"pageSize={pageSize}"
            }.Where(s => s != null));

            ViewBag.Pager = new PagedResult<object>
            {
                Page = page,
                PageSize = pageSize,
                TotalItems = total,
                QueryString = baseQs
            };

            return View(items);
        }

        // GET /Events/Details/5
        [AllowAnonymous]
        [HttpGet("Details/{id:int}")]
        public async Task<IActionResult> Details(int id)
        {
            var ev = await _db.Events
                .Include(e => e.EventMembers).ThenInclude(em => em.Member)
                .FirstOrDefaultAsync(e => e.Id == id);
            if (ev is null) return NotFound();

            // свободни членове за дропдауна
            var used = ev.EventMembers.Select(em => em.MemberId).ToHashSet();
            var free = await _db.Members.AsNoTracking()
                .Where(m => !used.Contains(m.Id))
                .OrderBy(m => m.Name)
                .ToListAsync();
            ViewBag.Members = new SelectList(free, "Id", "Name");

            return View(ev);
        }

        // GET /Events/Create
        [Authorize(Policy = "CanWrite")]
        [HttpGet("Create")]
        public IActionResult Create() => View(new Event { StartAt = DateTime.UtcNow });

        // POST /Events/Create
        [Authorize(Policy = "CanWrite")]
        [HttpPost("Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Title,Description,StartAt")] Event ev)
        {
            if (!ModelState.IsValid) return View(ev);
            _db.Events.Add(ev);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Събитие: създадено.";
            return RedirectToAction(nameof(Index));
        }

        // GET /Events/Edit/5
        [Authorize(Policy = "CanWrite")]
        [HttpGet("Edit/{id:int}")]
        public async Task<IActionResult> Edit(int id)
        {
            var ev = await _db.Events
                .Include(e => e.EventMembers).ThenInclude(em => em.Member)
                .FirstOrDefaultAsync(e => e.Id == id);
            if (ev is null) return NotFound();

            await PopulateMembersSelect(id); // свободни членове за дропдауна
            return View(ev);
        }

        // POST /Events/Edit/5
        [Authorize(Policy = "CanWrite")]
        [HttpPost("Edit/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            [Bind("Id,Title,Description,StartAt")] Event ev,
            [FromForm] string? op,
            [FromForm] int? memberId)
        {
            // добавяне
            if (op == "add-member")
            {
                if (memberId is null || memberId <= 0)
                {
                    TempData["Error"] = "Избери участник.";
                    return RedirectToAction(nameof(Edit), new { id });
                }

                bool exists = await _db.EventMembers.AnyAsync(x => x.EventId == id && x.MemberId == memberId);
                if (exists)
                {
                    TempData["Error"] = "Този участник вече е добавен.";
                    return RedirectToAction(nameof(Edit), new { id });
                }

                _db.EventMembers.Add(new EventMember { EventId = id, MemberId = memberId.Value });
                await _db.SaveChangesAsync();
                TempData["Success"] = "Участникът е добавен.";
                return RedirectToAction(nameof(Edit), new { id });
            }

            // премахване
            if (op == "remove-member")
            {
                if (memberId is null || memberId <= 0)
                { TempData["Error"] = "Липсва участник."; return RedirectToAction(nameof(Edit), new { id }); }

                var link = await _db.EventMembers
                    .FirstOrDefaultAsync(x => x.EventId == id && x.MemberId == memberId);
                if (link != null)
                {
                    _db.EventMembers.Remove(link);
                    await _db.SaveChangesAsync();
                    TempData["Success"] = "Участникът е премахнат.";
                }
                return RedirectToAction(nameof(Edit), new { id });
            }

            // нормална редакция
            if (id != ev.Id) return NotFound();
            if (!ModelState.IsValid)
            {
                await PopulateMembersSelect(id);
                return View(ev);
            }

            _db.Entry(ev).State = EntityState.Modified;
            await _db.SaveChangesAsync();
            TempData["Success"] = "Събитието е обновено.";
            return RedirectToAction(nameof(Index));
        }

        // GET /Events/Delete/5
        [Authorize(Policy = "CanWrite")]
        [HttpGet("Delete/{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var ev = await _db.Events.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id);
            if (ev is null) return NotFound();
            return View(ev);
        }

        // POST /Events/Delete/5
        [Authorize(Policy = "CanWrite")]
        [HttpPost("Delete/{id:int}")]
        [ValidateAntiForgeryToken]
        [ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var ev = await _db.Events.FindAsync(id);
            if (ev != null)
            {
                _db.Events.Remove(ev);
                await _db.SaveChangesAsync();
                TempData["Success"] = "Събитие: изтрито.";
            }
            return RedirectToAction(nameof(Index));
        }

        // helpers
        private async Task PopulateMembersSelect(int eventId)
        {
            var usedIds = await _db.EventMembers
                .Where(em => em.EventId == eventId)
                .Select(em => em.MemberId)
                .ToListAsync();

            var members = await _db.Members
                .AsNoTracking()
                .Where(m => !usedIds.Contains(m.Id))
                .OrderBy(m => m.Name)
                .ToListAsync();

            ViewBag.Members = new SelectList(members, "Id", "Name");
        }
    }
}
