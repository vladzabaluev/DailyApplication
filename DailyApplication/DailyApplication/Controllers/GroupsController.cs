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
using System.Security.Claims;

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

        public List<Group> GetUserGroups(ClaimsPrincipal User)
        {
            List<Group> Groups = new List<Group>(); //сюда запишу все группы текущего пользователя
            User currentUser = _userManager.GetUserAsync(User).Result; //найду текущего пользователя
            List<UserGroup> UserGroups = _context.UserGroup.Where
                (findGroup => findGroup.User == currentUser).ToList(); //найду все ЮзерГруппы, связанные с нашим юзером
            foreach (UserGroup group in UserGroups) //благодаря юзергруппам найду все группы пользователя
            {
                Groups.Add(_context.Group.FirstOrDefaultAsync(foundGroup => foundGroup.Id == group.Id).Result);
            }
            return Groups;
        }

        #endregion Все группы пользователя

        // GET: Groups/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var @group = await _context.Group
                .FirstOrDefaultAsync(m => m.Id == id);
            if (@group == null)
            {
                return NotFound();
            }

            return View(@group);
        }

        #region Создание группы

        // POST: Groups/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,Description")] Group @group, ClaimsPrincipal User)
        {
            if (ModelState.IsValid)
            {
                UserGroup newUserGroup = new UserGroup();
                newUserGroup.User = _userManager.GetUserAsync(User).Result;
                newUserGroup.Group = group;
                _context.Add(@group);
                _context.Add(newUserGroup);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(EventsController.GetUserEvents));
        }

        #endregion Создание группы

        // GET: Groups/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var @group = await _context.Group.FindAsync(id);
            if (@group == null)
            {
                return NotFound();
            }
            return View(@group);
        }

        // POST: Groups/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description")] Group @group)
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
                return RedirectToAction(nameof(Index));
            }
            return View(@group);
        }

        // GET: Groups/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var @group = await _context.Group
                .FirstOrDefaultAsync(m => m.Id == id);
            if (@group == null)
            {
                return NotFound();
            }

            return View(@group);
        }

        // POST: Groups/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var @group = await _context.Group.FindAsync(id);
            _context.Group.Remove(@group);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool GroupExists(int id)
        {
            return _context.Group.Any(e => e.Id == id);
        }
    }
}