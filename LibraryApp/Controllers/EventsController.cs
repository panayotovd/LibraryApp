using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LibraryApp.Models;
using LibraryApp.Models.ViewModels;

namespace LibraryApp.Controllers
{
    public class EventsController : Controller
    {
        private readonly LibraryDbContext _db;
        public EventsController(LibraryDbContext db) => _db = db;

        // GET: /Events
        public async Task<IActionResult> Index(string q, DateTime? from, DateTime? to)
        {
            // Fix for CS8620: Cast ICollection<EventMember>? to IEnumerable<EventMember> to match the expected nullability in ThenInclude

            var query = _db.Events
                .Include(e => (List<EventMember>)e.EventMembers!) // Cast to List<EventMember>
                .ThenInclude(em => em.Member)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
                query = query.Where(e => e.Title.Contains(q) || e.Description.Contains(q));

            if (from.HasValue) query = query.Where(e => e.StartAt >= from.Value);
            if (to.HasValue) query = query.Where(e => e.StartAt <= to.Value);

            var model = await query.OrderBy(e => e.StartAt).ToListAsync();
            ViewBag.Search = q; ViewBag.From = from?.ToString("yyyy-MM-dd"); ViewBag.To = to?.ToString("yyyy-MM-dd");
            return View(model);
        }

        // GET: /Events/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var ev = await _db.Events
                .Include(e => (List<EventMember>)e.EventMembers!) // Cast to List<EventMember>
                .ThenInclude(em => em.Member)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (ev == null) return NotFound();

            // списък на не-записани членове (за бързо добавяне)
            var signedIds = ev.EventMembers.Select(em => em.MemberId).ToHashSet();
            ViewBag.AvailableMembers = await _db.Members
                .Where(m => !signedIds.Contains(m.Id))
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
        [HttpPost]
        [ValidateAntiForgeryToken]
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
                EventMembers = vm.SelectedMemberIds.Distinct().Select(id => new EventMember
                {
                    MemberId = id
                }).ToList()
            };

            _db.Events.Add(ev);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: /Events/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var ev = await _db.Events.Include(e => e.EventMembers).FirstOrDefaultAsync(e => e.Id == id);
            if (ev == null) return NotFound();

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
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, EventFormViewModel vm)
        {
            if (id != vm.Id) return NotFound();

            if (!ModelState.IsValid)
            {
                vm.AllMembers = await _db.Members.OrderBy(m => m.FullName).ToListAsync();
                return View(vm);
            }

            var ev = await _db.Events.Include(e => e.EventMembers).FirstOrDefaultAsync(e => e.Id == id);
            if (ev == null) return NotFound();

            ev.Title = vm.Title;
            ev.Description = vm.Description;
            ev.StartAt = vm.StartAt;

            // синхронизация на много-към-много
            var newSet = vm.SelectedMemberIds.Distinct().ToHashSet();
            var toRemove = ev.EventMembers.Where(em => !newSet.Contains(em.MemberId)).ToList();
            foreach (var r in toRemove) _db.EventMembers.Remove(r);

            var existingIds = ev.EventMembers.Select(em => em.MemberId).ToHashSet();
            var toAdd = newSet.Where(id2 => !existingIds.Contains(id2))
                              .Select(id2 => new EventMember { EventId = ev.Id, MemberId = id2 });
            await _db.EventMembers.AddRangeAsync(toAdd);

            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Details), new { id = ev.Id });
        }

        // GET: /Events/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var ev = await _db.Events
                .Include(e => (List<EventMember>)e.EventMembers!)
                .ThenInclude(em => em.Member)
                .FirstOrDefaultAsync(e => e.Id == id);
            if (ev == null) return NotFound();
            return View(ev);
        }

        // POST: /Events/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var ev = await _db.Events.Include(e => e.EventMembers).FirstOrDefaultAsync(e => e.Id == id);
            if (ev != null)
            {
                _db.EventMembers.RemoveRange(ev.EventMembers);
                _db.Events.Remove(ev);
                await _db.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        // Бърза регистрация/отписване от Details
        [HttpPost]
        [ValidateAntiForgeryToken]
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

        [HttpPost]
        [ValidateAntiForgeryToken]
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
