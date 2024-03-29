﻿using DailyApplication.Data;
using DailyApplication.Models;
using Microsoft.AspNetCore.Authorization;
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
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        public GroupsController(ApplicationDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        #region Все группы пользователя
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public List<Group> GetUserGroups(ClaimsPrincipal User)
        {
            List<Group> Groups = new List<Group>(); //сюда запишу все группы текущего пользователя

            List<UserGroup> UserGroups = _context.UserGroup.Where
                (findGroup => findGroup.User == _userManager.GetUserAsync(User).Result && findGroup.UserIsInGroup == true).Include("Group").ToList();
            //найду все ЮзерГруппы, связанные с нашим юзером

            foreach (UserGroup ug in UserGroups) //благодаря юзергруппам найду все группы пользователя
            {
                Group gr = new();

                Groups.Add(_context.Group.FirstOrDefault(foundGroup => foundGroup.Id == ug.Group.Id));
            }
            return Groups;
        }

        #endregion Все группы пользователя
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteGroup(int? id, EventsController eventsController)
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

        #region Создание группы
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<Group> Create([Bind("Id,Name,Description")] Group @group, ClaimsPrincipal User)
        {
            if (ModelState.IsValid)
            {
                UserGroup newUserGroup = new UserGroup();
                newUserGroup.User = _userManager.GetUserAsync(User).Result;
                newUserGroup.Group = group;
                newUserGroup.UserIsInGroup = true;
                _context.Add(@group);
                _context.Add(newUserGroup);
                await _context.SaveChangesAsync();
            }
            return group;
        }

        #endregion Создание группы
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditGroup(int id, [Bind("Id,Name,Description")] Group group)
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
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<bool> UserExists(string email)
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
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task InviteUser(string email, Group group)
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
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task UserAgree(ClaimsPrincipal user, Group group)
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
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task UserDisagree(ClaimsPrincipal user, Group group)
        {
            UserGroup userGroup = await _context.UserGroup.FirstOrDefaultAsync(usGr => usGr.Group == group
                    && usGr.User == _userManager.GetUserAsync(user).Result);
            if (userGroup != null)
            {
                _context.UserGroup.Remove(userGroup);
                await _context.SaveChangesAsync();
            }
        }
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task Exit(ClaimsPrincipal user, Group group, EventsController eventsController)
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
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<List<Group>> GetAllInvites(ClaimsPrincipal user)
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
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<List<User>> GetAllUsersInGroup(Group group)
        {
            List<User> usersInGroup = new List<User>();
            List<UserGroup> userGroups = await _context.UserGroup.Where(usGr => usGr.Group == group).Include("User").ToListAsync();
            foreach (UserGroup ug in userGroups)
            {
                usersInGroup.Add(ug.User);
            }
            return usersInGroup;
        }

        private bool GroupExists(int id)
        {
            return _context.Group.Any(e => e.Id == id);
        }
    }
}