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

namespace DailyApplication.Controllers
{
    public class EventsController : Controller
    {
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
        private readonly UserManager<User> _userManager;
        private GroupsController _groupsController;

        //  private readonly IConfigureGroupSerializer configureGroupSerializer;
        public EventsController(IDbContextFactory<ApplicationDbContext> contextFactory, UserManager<User> userManager/*, GroupsController groupsController*/)
        {
            _contextFactory = contextFactory;
            _userManager = userManager;
            //_groupsController = groupsController;
        }

        #region Создать ивент

        //Созда событие(сделать асинхронным)
        public async Task<Event> CreateEvent(string Name, string Description, ClaimsPrincipal User, DateTime DeadlineTime, List<Sub_event> subEvents, Group group)
        {
            using (var _context = _contextFactory.CreateDbContext())
            {
                User user = await _userManager.GetUserAsync(User);
                _context.Entry(user).State = EntityState.Unchanged;
                Event newEvent = new Event()
                {
                    Name = Name,
                    Description = Description,
                    User = user,
                    DeadlineTime = DeadlineTime,
                    Group = group,
                    SubEvents = subEvents,
                    IsDone = false
                };
                _context.Event.Add(newEvent);
                await _context.SaveChangesAsync();
                return newEvent;
            }
        }

        #endregion Создать ивент

        #region Вернуть ивенты

        //ALL EVENTS IN DB
        public async Task<List<Event>> GetAllEvents()
        {
            using (var _context = _contextFactory.CreateDbContext())
            {
                return await _context.Event.ToListAsync();
            }
        }

        //ONLY USER WITHOUT GROUP
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<List<Event>> GetUserEvents(ClaimsPrincipal user)
        {
            using (var _context = _contextFactory.CreateDbContext())
            {
                IdentityUser userTest = new IdentityUser();
                userTest = await _userManager.GetUserAsync(user);
                List<Event> events = await _context.Event.Where(ev => ev.User == userTest).
                    OrderBy(ev => ev.DeadlineTime).ToListAsync();
                return events;
            }
        }

        //ONLY GROUP WITHOUT USER
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<List<Event>> GetGroupEvents(ClaimsPrincipal user)
        {
            using (var _context = _contextFactory.CreateDbContext())
            {
                List<Group> UserGroups = await _groupsController.GetUserGroups(user);
                List<Event> events = new List<Event>();

                //Найти все группы пользователя
                foreach (Group group in UserGroups)
                {
                    events.AddRange(_context.Event.Where(GroupEvent => GroupEvent.Group == group).Include("SubEvents").OrderBy(ev => ev.DeadlineTime));
                }
                return events;
            }

            //найти все их ивенты и вернуть их отсортировав по времени
        }

        // ONLY ONE GROUP WITHOUT USER
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public List<Event> GetOneGroupEvents(Group group)
        {
            using (var _context = _contextFactory.CreateDbContext())
            {
                List<Event> events = new();

                // Найти все ивенты группы
                events.AddRange(_context.Event.Where(GroupEvent => GroupEvent.Group == group).Include("SubEvents").OrderBy(ev => ev.DeadlineTime));
                return events;
            }

            // Вернуть их, отсортировав по времени
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<List<Event>> GetAllUserEvent(ClaimsPrincipal user, GroupsController groupsController)
        {
            using (var _context = _contextFactory.CreateDbContext())
            {
                _groupsController = groupsController;
                List<Event> events = new List<Event>();
                events.AddRange(await GetUserEvents(user));
                events.AddRange(await GetGroupEvents(user));
                events = events.Distinct().ToList();
                SortByTime(events);
                return events;
            }
        }

        #region SORTIROVKA

        private List<Event> SortByTime(List<Event> events)
        {
            events.Sort((Event x, Event y) => x.DeadlineTime.CompareTo(y.DeadlineTime));
            return events;
        }

        #endregion SORTIROVKA

        #endregion Вернуть ивенты

        #region Получить подсобытия

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<List<Sub_event>> GetEventSubEvents(int Id)
        {
            using (var _context = _contextFactory.CreateDbContext())
            {
                Event curEv = _context.Event.FirstOrDefault(ev => ev.Id == Id);
                List<Sub_event> curSubEvents = await _context.Sub_event.Where(curSub => curSub.Event.Id == Id).ToListAsync();
                return curSubEvents;
            }
        }

        #endregion Получить подсобытия

        #region Удалить подсобытие

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteEventSubEvents(int Id)
        {
            using (var _context = _contextFactory.CreateDbContext())
            {
                Sub_event removableSubEvent = await _context.Sub_event.FirstOrDefaultAsync(remSubEv => remSubEv.Id == Id);
                if (removableSubEvent == null)
                {
                    return NotFound();
                }
                _context.Sub_event.Remove(removableSubEvent);
                return new RedirectToPageResult(nameof(GetUserEvents));
            }
        }

        #endregion Удалить подсобытие

        #region Удалить события

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteEvent(int? id)
        {
            using (var _context = _contextFactory.CreateDbContext())
            {
                if (id == null)
                {
                    return NotFound();
                }
                Event removableEvent = await _context.Event.FirstOrDefaultAsync(curEv => curEv.Id == id);
                if (removableEvent == null)
                {
                    return NotFound();
                }

                List<Sub_event> removableSubEvents = await GetEventSubEvents(removableEvent.Id);
                foreach (Sub_event sub_Event in removableSubEvents)
                {
                    Sub_event currentRemSubEvent = _context.Sub_event.FirstOrDefault(remSubEv => remSubEv.Id == sub_Event.Id);
                    _context.Sub_event.Remove(currentRemSubEvent);
                }
                _context.Event.Remove(removableEvent);

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(GetUserEvents));
            }
        }

        #endregion Удалить события

        #region Изменить события

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description,DeadlineTime")] Event @event)
        {
            using (var _context = _contextFactory.CreateDbContext())
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
                }
                return RedirectToAction(nameof(GetUserEvents));
            }
        }

        #endregion Изменить события

        #region Изменить подсобытия

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubEdit(int id, [Bind("Id,Name,Description")] Sub_event subEvent)
        {
            using (var _context = _contextFactory.CreateDbContext())
            {
                if (id != subEvent.Id)
                {
                    return NotFound();
                }

                if (ModelState.IsValid)
                {
                    try
                    {
                        _context.Update(subEvent);
                        await _context.SaveChangesAsync();
                    }
                    catch (DbUpdateConcurrencyException)
                    {
                        if (!EventExists(subEvent.Id))
                        {
                            return NotFound();
                        }
                        else
                        {
                            throw;
                        }
                    }
                }
                return RedirectToAction(nameof(GetUserEvents));
            }
        }

        #endregion Изменить подсобытия

        #region Выполнить событие

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DoneEvent(int? id)
        {
            using (var _context = _contextFactory.CreateDbContext())
            {
                if (id == null)
                {
                    return NotFound();
                }
                var processEvent = await _context.Event.FirstOrDefaultAsync(procEv => procEv.Id == id);
                if (processEvent == null)
                {
                    return NotFound();
                }
                processEvent.IsDone = true;
                _context.Update(processEvent);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(GetUserEvents));
            }
        }

        #endregion Выполнить событие

        #region Выполнить подсобытие

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DoneSubEvent(int? id)
        {
            using (var _context = _contextFactory.CreateDbContext())
            {
                if (id == null)
                {
                    return NotFound();
                }
                var processSUBEvent = await _context.Sub_event.FirstOrDefaultAsync(procEv => procEv.Id == id);
                if (processSUBEvent == null)
                {
                    return NotFound();
                }
                processSUBEvent.isDone = !processSUBEvent.isDone;
                _context.Update(processSUBEvent);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(GetUserEvents));
            }
        }

        #endregion Выполнить подсобытие

        private bool EventExists(int id)
        {
            using (var _context = _contextFactory.CreateDbContext())
            {
                return _context.Event.Any(e => e.Id == id);
            }
        }
    }
}