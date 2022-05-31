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
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private GroupsController _groupsController;

        //  private readonly IConfigureGroupSerializer configureGroupSerializer;
        public EventsController(ApplicationDbContext context, UserManager<User> userManager/*, GroupsController groupsController*/)
        {
            _context = context;
            _userManager = userManager;
            //_groupsController = groupsController;
        }

        #region Создать ивент

        //Созда событие(сделать асинхронным)
        public Event CreateEvent(string Name, string Description, ClaimsPrincipal User, DateTime DeadlineTime, List<string> subEvents, Group group)
        {
            List<Sub_event> tempSubEv = new List<Sub_event>();
            foreach (string descValue in subEvents)
            {
                Sub_event createdSubEvent = new Sub_event()
                {
                    Description = descValue,
                    isDone = false
                };
                tempSubEv.Add(createdSubEvent);
            }

            Event newEvent = new Event()
            {
                Name = Name,
                Description = Description,
                User = _userManager.GetUserAsync(User).Result,
                DeadlineTime = DeadlineTime,
                Group = group,
                SubEvents = tempSubEv,
                IsDone = false
            };
            _context.Event.Add(newEvent);
            _context.SaveChanges();
            return newEvent;
        }

        #endregion Создать ивент

        #region Вернуть ивенты

        //ALL EVENTS IN DB
        public List<Event> GetAllEvents()
        {
            return _context.Event.ToList();
        }

        //ONLY USER WITHOUT GROUP
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public List<Event> GetUserEvents(ClaimsPrincipal user)
        {
            List<Event> events = _context.Event.Where(ev => ev.User == _userManager.GetUserAsync(user).Result).ToList();
            SortByTime(events);
            return events;
        }

        //ONLY GROUP WITHOUT USER
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public List<Event> GetGroupEvents(ClaimsPrincipal user)
        {
            List<Group> UserGroups = _groupsController.GetUserGroups(user);
            List<Event> events = new List<Event>();

            //Найти все группы пользователя
            foreach (Group group in UserGroups)
            {
                events.AddRange(_context.Event.Where(GroupEvent => GroupEvent.Group == group));
            }
            SortByTime(events);
            return events;
            //найти все их ивенты и вернуть их отсортировав по времени
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public List<Event> GetAllUserEvent(ClaimsPrincipal user, GroupsController groupsController)
        {
            _groupsController = groupsController;
            List<Event> events = new List<Event>();
            events.AddRange(GetUserEvents(user));
            events.AddRange(GetGroupEvents(user));
            SortByTime(events);
            return events;
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
        public List<Sub_event> GetEventSubEvents(int Id)
        {
            Event curEv = _context.Event.FirstOrDefault(ev => ev.Id == Id);
            List<Sub_event> curSubEvents = _context.Sub_event.Where(curSub => curSub.Event.Id == Id).ToList();
            return curSubEvents;
        }

        #endregion Получить подсобытия

        #region Удалить подсобытие

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteEventSubEvents(int Id)
        {
            Sub_event removableSubEvent = await _context.Sub_event.FirstOrDefaultAsync(remSubEv => remSubEv.Id == Id);
            if (removableSubEvent == null)
            {
                return NotFound();
            }
            _context.Sub_event.Remove(removableSubEvent);
            return new RedirectToPageResult(nameof(GetUserEvents));
        }

        #endregion Удалить подсобытие

        #region Удалить события

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteEvent(int? id)
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

            List<Sub_event> removableSubEvents = GetEventSubEvents(removableEvent.Id);
            foreach (Sub_event sub_Event in removableSubEvents)
            {
                Sub_event currentRemSubEvent = _context.Sub_event.FirstOrDefault(remSubEv => remSubEv.Id == sub_Event.Id);
                _context.Sub_event.Remove(currentRemSubEvent);
            }
            _context.Event.Remove(removableEvent);

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(GetUserEvents));
        }

        #endregion Удалить события

        #region Изменить события

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
            }
            return RedirectToAction(nameof(GetUserEvents));
        }

        #endregion Изменить события

        #region Изменить подсобытия

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubEdit(int id, [Bind("Id,Name,Description")] Sub_event subEvent)
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

        #endregion Изменить подсобытия

        #region Выполнить событие

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DoneEvent(int? id)
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

        #endregion Выполнить событие

        #region Выполнить подсобытие

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DoneSubEvent(int? id)
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

        #endregion Выполнить подсобытие

        private bool EventExists(int id)
        {
            return _context.Event.Any(e => e.Id == id);
        }
    }
}