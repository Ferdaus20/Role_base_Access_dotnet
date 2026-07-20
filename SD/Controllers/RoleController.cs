using Microsoft.AspNetCore.Mvc;
using SD.Data;
using Microsoft.EntityFrameworkCore;
using SD.Models;

namespace SD.Controllers
{
    public class RoleController : Controller
    {

        private readonly MyDbContext _context;

        public RoleController(MyDbContext context)
        {
            _context = context;
        }


        // =================================================================
        // READ / VIEW ALL ROLES
        // =================================================================
        // GET: /Role
        public async Task<IActionResult> Index()
        {
            var roles = await _context.Roles.ToListAsync();
            return View(roles);
        }
        // =================================================================
        // CREATE ROLES
        // =================================================================
        // GET: /Role/Create
        public IActionResult Create()
        {
            return View();
        }
        // POST: /Role/Create

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Role role)
        {
            if (ModelState.IsValid)
            {
                // Check for duplicate role names
                bool roleExists = await _context.Roles.AnyAsync(r => r.Name == role.Name);

                if (roleExists)
                {
                    ModelState.AddModelError("Name", "A role with this name already exists.");
                    return View(role);
                }
                _context.Roles.Add(role);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));

            }
            return View(role);
        }

        // =================================================================
        // EDIT ROLES
        // =================================================================
        // GET: /Role/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            var role = await _context.Roles.FindAsync(id);
            if (role == null)
            {
                return NotFound();
            }
            return View(role);
        }
        // POST: /Role/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Role updatedRolee)
        {
            if (id != updatedRolee.Id)
            {
                return NotFound();
            }
            if (ModelState.IsValid)
            {
                // Check if the new name conflicts with a different existing role
                bool nameExists = await _context.Roles.AnyAsync(r => r.Name == updatedRolee.Name && r.Id != id);

                if (nameExists)
                {
                    ModelState.AddModelError("Name", "A role with this name already exists.");
                    return View(updatedRolee);
                }
                _context.Roles.Update(updatedRolee);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(updatedRolee);
        }
        // =================================================================
        // EDIT ROLES
        // =================================================================


        // GET: /Role/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            var role = await _context.Roles.FirstOrDefaultAsync(m => m.Id == id);
            if (role == null)
            {
                return NotFound();
            }
            return View(role);
        }
        // POST: /Role/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var role = await _context.Roles.FindAsync(id);
            if (role != null)
            {
                // EF Core cascades this deletion automatically to UserAssignRoles
                _context.Roles.Remove(role);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));


        }
    }
}
