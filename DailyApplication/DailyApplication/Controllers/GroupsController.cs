using DailyApplication.Data;
using DailyApplication.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace DailyApplication.Controllers
{
    public class GroupsController : Controller
    {
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
        private readonly UserManager<User> _userManager;

        public GroupsController(IDbContextFactory<ApplicationDbContext> contextFactory, UserManager<User> userManager)
        {
            _contextFactory = contextFactory;
            _userManager = userManager;
        }

        #region Все группы пользователя

        public async Task<List<Group>> GetUserGroups(ClaimsPrincipal User)
        {
            using (var _context = _contextFactory.CreateDbContext())
            {
                List<Group> Groups = new List<Group>(); //сюда запишу все группы текущего пользователя
                User currentUser = await _userManager.GetUserAsync(User); //найду текущего пользователя

                List<UserGroup> UserGroups = await _context.UserGroup.Where
                    (findGroup => findGroup.User == currentUser && findGroup.UserIsInGroup == true).Include("Group").ToListAsync();
                //найду все ЮзерГруппы, связанные с нашим юзером

                foreach (UserGroup ug in UserGroups) //благодаря юзергруппам найду все группы пользователя
                {
                    Group gr = new();

                    Groups.Add(_context.Group.FirstOrDefault(foundGroup => foundGroup.Id == ug.Group.Id));
                }
                return Groups;
            }
        }

        #endregion Все группы пользователя

        public async Task<IActionResult> DeleteGroup(int? id, EventsController eventsController)
        {
            using (var _context = _contextFactory.CreateDbContext())
            {
                if (id == null)
                {
                    return NotFound();
                }
                var removableGroup = await _context.Group
                    .FirstOrDefaultAsync(m => m.Id == id);
                if (removableGroup == null)
                {
                    return NotFound();
                }

                //Удалить все ивенты группы
                List<Event> removableGroupEvents = await _context.Event.Where(remGE => remGE.Group.Id == removableGroup.Id).ToListAsync();
                foreach (Event removableEvent in removableGroupEvents)
                {
                    await eventsController.DeleteEvent(removableEvent.Id);
                }
                //получать всех юзеров через юзер группы и у них удалять ссылки на юзер группы
                //Удалить все юзер группы

                List<UserGroup> removableUserGroups = await _context.UserGroup.Where(remGE => remGE.Group == removableGroup).ToListAsync();

                foreach (UserGroup userGroup in removableUserGroups)
                {
                    _context.UserGroup.Remove(userGroup);
                }
                //Удалить группу
                _context.Group.Remove(removableGroup);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(EventsController.GetAllUserEvent));
            }
        }

        #region Создание группы

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<Group> Create([Bind("Id,Name,Description")] Group @group, ClaimsPrincipal User)
        {
            using (var _context = _contextFactory.CreateDbContext())
            {
                if (ModelState.IsValid)
                {
                    UserGroup newUserGroup = new UserGroup();
                    newUserGroup.User = await _userManager.GetUserAsync(User);
                    newUserGroup.Group = group;
                    newUserGroup.UserIsInGroup = true;
                    _context.Add(@group);
                    _context.Add(newUserGroup);
                    await _context.SaveChangesAsync();
                    RedirectToAction(nameof(EventsController.GetUserEvents));
                }
                return group;
            }
        }

        #endregion Создание группы

        public async Task<IActionResult> EditGroup(int id, [Bind("Id,Name,Description")] Group group)
        {
            using (var _context = _contextFactory.CreateDbContext())
            {
                if (id != @group.Id)
                {
                    return NotFound();
                }
                if (ModelState.IsValid)
                {
                    try
                    {
                        _context.Update(@group);
                        await _context.SaveChangesAsync();
                    }
                    catch (DbUpdateConcurrencyException)
                    {
                        if (!GroupExists(@group.Id))
                        {
                            return NotFound();
                        }
                        else
                        {
                            throw;
                        }
                    }
                }
                return RedirectToAction(nameof(GetUserGroups));
            }
        }

        public async Task<bool> UserExists(string email)
        {
            using (var _context = _contextFactory.CreateDbContext())
            {
                DailyApplication.Models.User user = await _context.User.Where(requiredUser => requiredUser.Email == email).FirstOrDefaultAsync();
                if (user != null)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        public async Task InviteUser(string email, Group group)
        {
            using (var _context = _contextFactory.CreateDbContext())
            {
                User invitedUSer = await _context.User.Where(requiredUser => requiredUser.Email == email).FirstOrDefaultAsync();
                if (invitedUSer != null)
                {
                    UserGroup userGroup = await _context.UserGroup.FirstOrDefaultAsync(ug => ug.User == invitedUSer && ug.Group == group);
                    if (userGroup == null)
                    {
                        UserGroup invUserGroup = new UserGroup();
                        invUserGroup.Group = group;
                        invUserGroup.User = invitedUSer;
                        invUserGroup.UserIsInGroup = false;
                        _context.UserGroup.Add(invUserGroup);
                        await _context.SaveChangesAsync();
                    }
                }
            }
        }

        public async Task UserAgree(ClaimsPrincipal user, Group group)
        {
            using (var _context = _contextFactory.CreateDbContext())
            {
                UserGroup userGroup = await _context.UserGroup.FirstOrDefaultAsync(usGr => usGr.Group == group
         && usGr.User == _userManager.GetUserAsync(user).Result);
                if (userGroup != null)
                {
                    userGroup.UserIsInGroup = true;
                    _context.UserGroup.Update(userGroup);
                    await _context.SaveChangesAsync();
                }
            }
        }

        public async Task UserDisagree(ClaimsPrincipal user, Group group)
        {
            using (var _context = _contextFactory.CreateDbContext())
            {
                UserGroup userGroup = await _context.UserGroup.FirstOrDefaultAsync(usGr => usGr.Group == group
        && usGr.User == _userManager.GetUserAsync(user).Result);
                if (userGroup != null)
                {
                    _context.UserGroup.Remove(userGroup);
                    await _context.SaveChangesAsync();
                }
            }
        }

        public async Task Exit(ClaimsPrincipal user, Group group, EventsController eventsController)
        {
            using (var _context = _contextFactory.CreateDbContext())
            {
                UserGroup userGroup = await _context.UserGroup.FirstOrDefaultAsync(usGr => usGr.Group == group
        && usGr.User == _userManager.GetUserAsync(user).Result);
                if (userGroup != null)
                {
                    _context.UserGroup.Remove(userGroup);
                    await _context.SaveChangesAsync();
                }
                if (GetAllUsersInGroup(group).Result.Count == 0)
                {
                    await DeleteGroup(group.Id, eventsController);
                }
            }
        }

        public async Task<List<Group>> GetAllInvites(ClaimsPrincipal user)
        {
            using (var _context = _contextFactory.CreateDbContext())
            {
                List<UserGroup> userGroupWasUserInvited = await _context.UserGroup.Where(usGr =>
       usGr.User == _userManager.GetUserAsync(user).Result && usGr.UserIsInGroup == false).Include("Group").ToListAsync();
                List<Group> groupWasUserInvited = new List<Group>();
                foreach (UserGroup userGroup in userGroupWasUserInvited)
                {
                    groupWasUserInvited.Add(await _context.Group.FirstOrDefaultAsync(gr => gr == userGroup.Group));
                }
                return groupWasUserInvited;
            }
        }

        public async Task<List<User>> GetAllUsersInGroup(Group group)
        {
            using (var _context = _contextFactory.CreateDbContext())
            {
                List<User> usersInGroup = new List<User>();
                List<UserGroup> userGroups = await _context.UserGroup.Where(usGr => usGr.Group == group).Include("User").ToListAsync();
                foreach (UserGroup ug in userGroups)
                {
                    usersInGroup.Add(ug.User);
                }
                return usersInGroup;
            }
        }

        private bool GroupExists(int id)
        {
            using (var _context = _contextFactory.CreateDbContext())
            {
                return _context.Group.Any(e => e.Id == id);
            }
        }
    }
}