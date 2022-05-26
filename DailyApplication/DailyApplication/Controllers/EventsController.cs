using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using DailyApplication.Data;
using DailyApplication.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using DailyApplication.Pages.Events;

namespace DailyApplication.Controllers
{
    public class EventsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public EventsController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // Созда событие (сделать асинхронным)
        public void CreateEvent(string Name, string Description, ClaimsPrincipal User, DateTime DeadlineTime)
        {
            Event newEvent = new Event()
            {
                Name = Name,
                Description = Description,
                User = _userManager.GetUserAsync(User).Result,
                DeadlineTime = DeadlineTime,
                IsDone = false
            };
            _context.Event.Add(newEvent);
            _context.SaveChanges();
        }

        // Чтение всех ивентов
        //public IEnumerable<Event> Index()
        //{
        //    return _context.Event;
        //}

        //public async Task<> Index()
        //{
        //    return await _context.Event.ToArrayAsync();
        //    //await _context.Event.ToListAsync();
        //    //  return _context.Event.ToListAsync();
        //    //   return RedirectToPage("Events");// (Pages.Events.Events, await _context.Event.ToListAsync());
        //}
        public List<Event> GetAllEvents()
        {
            return _context.Event.ToList();
        }

        public List<Event> GetUserEvents()
        {
            return _context.Event.Where(ev => ev.User == _userManager.GetUserAsync(User).Result).ToList();
        }

        public async Task<IActionResult> Prikol()
        {
            return new RedirectToPageResult("Events", new { events = await _context.Event.ToListAsync() });
        }

        // U
        public void UpdateEvent(Event changedEvent)
        {
            var item = _context.Event.Attach(changedEvent);
            item.State = Microsoft.EntityFrameworkCore.EntityState.Modified;

            _context.SaveChanges();
        }

        // D
        public void DeleteEvent(int Id)
        {
            var deletedItem = _context.Event.Find(Id);

            if (deletedItem != null)
            {
                _context.Event.Remove(deletedItem);
                _context.SaveChanges();
            }
        }

        // GET: Events
        //public async Task<IActionResult> Index()
        //{
        //    return View(await _context.Event.ToListAsync());
        //}

        //[Authorize]
        //public async Task<IActionResult> ShowUsersEvents()
        //{
        //    return View("Index", await _context.Event.
        //        Where(ev => ev.User == _userManager.GetUserAsync(User).Result).ToListAsync());
        //}

        [Authorize]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var @event = await _context.Event
                .FirstOrDefaultAsync(m => m.Id == id);
            if (@event == null)
            {
                return NotFound();
            }

            return View(@event);
        }

        // GET: Events/Create
        [Authorize]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Events/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,Description,DeadlineTime")] Event @event)
        {
            if (ModelState.IsValid)
            {
                @event.User = _userManager.GetUserAsync(User).Result;
                @event.IsDone = false;
                _context.Add(@event);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(@event);
        }

        public async Task<IActionResult> DoneEvent(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            var processEvent = await _context.Event.FindAsync(id);
            if (processEvent == null)
            {
                return NotFound();
            }
            processEvent.IsDone = true;
            _context.Update(processEvent);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(GetUserEvents));
        }

        // GET: NewEvents/Edit/5
        [Authorize]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var @event = await _context.Event.FindAsync(id);
            if (@event == null)
            {
                return NotFound();
            }
            return View(@event);
        }

        // POST: NewEvents/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description,DeadlineTime")] Event @event)
        {
            if (id != @event.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(@event);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!EventExists(@event.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(@event);
        }

        // GET: NewEvents/Delete/5
        [Authorize]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var @event = await _context.Event
                .FirstOrDefaultAsync(m => m.Id == id);
            if (@event == null)
            {
                return NotFound();
            }

            return View(@event);
        }

        // POST: NewEvents/Delete/5
        [Authorize]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var @event = await _context.Event.FindAsync(id);
            _context.Event.Remove(@event);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool EventExists(int id)
        {
            return _context.Event.Any(e => e.Id == id);
        }
    }
}