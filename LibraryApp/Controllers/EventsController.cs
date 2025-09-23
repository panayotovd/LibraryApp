using LibraryApp.Models;
using LibraryApp.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryApp.Controllers
{
    public class EventsController : Controller
    {
        private readonly LibraryDbContext _db;
        public EventsController(LibraryDbContext db) => _db = db;

        // GET: /Events
        public async Task<IActionResult> Index(
            string? q,
            DateTime? from,
            DateTime? to,
            string? sort = "date", // date|title|count
            string? dir = "asc",
            int page = 1,
            int pageSize = 10)
        {
            var query = _db.Events
                .AsNoTracking()
                .Include(e => e.EventMembers)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                var pattern = $"%{q}%";
                query = query.Where(e => EF.Functions.Like(e.Title, pattern) ||
                                         EF.Functions.Like(e.Description!, pattern));
            }
            if (from.HasValue) query = query.Where(e => e.StartAt >= from.Value);
            if (to.HasValue) query = query.Where(e => e.StartAt <= to.Value);

            bool asc = string.Equals(dir, "asc", StringComparison.OrdinalIgnoreCase);
            query = (sort?.ToLower()) switch
            {
                "title" => asc ? query.OrderBy(e => e.Title) : query.OrderByDescending(e => e.Title),
                "count" => asc ? query.OrderBy(e => e.EventMembers.Count)
                               : query.OrderByDescending(e => e.EventMembers.Count),
                _ => asc ? query.OrderBy(e => e.StartAt) : query.OrderByDescending(e => e.StartAt)
            };

            if (page < 1) page = 1;
            if (pageSize < 5) pageSize = 5; if (pageSize > 50) pageSize = 50;

            var total = await query.CountAsync();
            var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            string baseQs = "?" + string.Join("&", new[]
            {
                q is null ? null : $"q={Uri.EscapeDataString(q)}",
                from is null ? null : $"from={from:yyyy-MM-dd}",
                to   is null ? null : $"to={to:yyyy-MM-dd}",
                $"sort={sort}", $"dir={dir}", $"pageSize={pageSize}"
            }.Where(s => s != null));

            ViewBag.Pager = new LibraryApp.Models.ViewModels.PagedResult<object>
            {
                Page = page,
                PageSize = pageSize,
                TotalItems = total,
                QueryString = baseQs
            };

            ViewBag.Filters = new { q, from, to, sort, dir, pageSize };

            return View(items);
        }


        // GET: /Events/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id is null) return NotFound();

            var ev = await _db.Events
                .Include(e => e.EventMembers)
                    .ThenInclude(em => em.Member)
                .FirstOrDefaultAsync(e => e.Id == id.Value);

            if (ev is null) return NotFound();

            // За dropdown: членове, които още НЕ са записани
            var signed = ev.EventMembers.Select(em => em.MemberId).ToHashSet();
            ViewBag.AvailableMembers = await _db.Members
                .Where(m => !signed.Contains(m.Id))
                .OrderBy(m => m.FullName)
                .ToListAsync();

            return View(ev);
        }

        // GET: /Events/Create
        public async Task<IActionResult> Create()
        {
            var vm = new EventFormViewModel
            {
                StartAt = DateTime.Today.AddDays(1).AddHours(18),
                AllMembers = await _db.Members.OrderBy(m => m.FullName).ToListAsync()
            };
            return View(vm);
        }

        // POST: /Events/Create
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(EventFormViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                vm.AllMembers = await _db.Members.OrderBy(m => m.FullName).ToListAsync();
                return View(vm);
            }

            var ev = new Event
            {
                Title = vm.Title,
                Description = vm.Description,
                StartAt = vm.StartAt,
                EventMembers = vm.SelectedMemberIds
                    .Distinct()
                    .Select(id => new EventMember { MemberId = id })
                    .ToList()
            };

            _db.Events.Add(ev);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: /Events/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id is null) return NotFound();

            var ev = await _db.Events
                .Include(e => e.EventMembers)
                .FirstOrDefaultAsync(e => e.Id == id.Value);

            if (ev is null) return NotFound();

            var vm = new EventFormViewModel
            {
                Id = ev.Id,
                Title = ev.Title,
                Description = ev.Description,
                StartAt = ev.StartAt,
                SelectedMemberIds = ev.EventMembers.Select(em => em.MemberId).ToList(),
                AllMembers = await _db.Members.OrderBy(m => m.FullName).ToListAsync()
            };
            return View(vm);
        }

        // POST: /Events/Edit/5
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, EventFormViewModel vm)
        {
            if (id != vm.Id) return NotFound();

            if (!ModelState.IsValid)
            {
                vm.AllMembers = await _db.Members.OrderBy(m => m.FullName).ToListAsync();
                return View(vm);
            }

            var ev = await _db.Events
                .Include(e => e.EventMembers)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (ev is null) return NotFound();

            ev.Title = vm.Title;
            ev.Description = vm.Description;
            ev.StartAt = vm.StartAt;

            // Sync many-to-many
            var desired = vm.SelectedMemberIds.Distinct().ToHashSet();
            var existing = ev.EventMembers.Select(em => em.MemberId).ToHashSet();

            // Remove
            var remove = ev.EventMembers.Where(em => !desired.Contains(em.MemberId)).ToList();
            if (remove.Count > 0) _db.EventMembers.RemoveRange(remove);

            // Add
            var add = desired.Where(mid => !existing.Contains(mid))
                             .Select(mid => new EventMember { EventId = ev.Id, MemberId = mid });
            await _db.EventMembers.AddRangeAsync(add);

            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Details), new { id = ev.Id });
        }

        // GET: /Events/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id is null) return NotFound();

            var ev = await _db.Events
                .Include(e => e.EventMembers)
                    .ThenInclude(em => em.Member)
                .FirstOrDefaultAsync(e => e.Id == id.Value);

            if (ev is null) return NotFound();
            return View(ev);
        }

        // POST: /Events/Delete/5
        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var ev = await _db.Events
                .Include(e => e.EventMembers)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (ev != null)
            {
                _db.EventMembers.RemoveRange(ev.EventMembers);
                _db.Events.Remove(ev);
                await _db.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        // POST: /Events/AddMember
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AddMember(int eventId, int memberId)
        {
            var exists = await _db.EventMembers.AnyAsync(x => x.EventId == eventId && x.MemberId == memberId);
            if (!exists)
            {
                _db.EventMembers.Add(new EventMember { EventId = eventId, MemberId = memberId });
                await _db.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Details), new { id = eventId });
        }

        // POST: /Events/RemoveMember
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveMember(int eventId, int memberId)
        {
            var em = await _db.EventMembers.FirstOrDefaultAsync(x => x.EventId == eventId && x.MemberId == memberId);
            if (em != null)
            {
                _db.EventMembers.Remove(em);
                await _db.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Details), new { id = eventId });
        }
    }
}
