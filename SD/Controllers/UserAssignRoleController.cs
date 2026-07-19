using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SD.Data;
using SD.Models;

namespace SD.Controllers
{
    public class UserAssignRoleController : Controller
    {
        private readonly MyDbContext _context;

        public UserAssignRoleController(MyDbContext context)
        {
            _context = context;
        }
        // =================================================================
        // READ / VIEW ALL AssignRoles
        // =================================================================
        // GET: /UserAssignRole
        public async Task<IActionResult> Index()
        {
            // Fetch assignments along with full User and Role details
            var assignments=await _context.UserAssignRoles
                .Include(uar=>uar.User)
                .Include(uar=>uar.Role)
                .ToListAsync();
            return View(assignments);

        }
        // =================================================================
        //Update Role
        // =================================================================
        // GET: /UserAssignRole/Assign
        public async Task<IActionResult> Assign()
        {
            // Populate select list dropdown elements for the web UI view
            ViewBag.Users = new SelectList(await _context.Users.ToListAsync(), "Id", "Email");
            ViewBag.Roles = new SelectList(await _context.Roles.ToListAsync(), "Id", "Name");

            return View();
        }
        // POST: /UserAssignRole/Assign
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Assign(UserAssignRole assignment)
        {
            if(ModelState.IsValid)
            {
                // Verify the user does not already possess this exact tier role assignment
                bool assignmentExists = await _context.UserAssignRoles
                   .AnyAsync(uar => uar.UserId == assignment.UserId && uar.RoleId == assignment.RoleId);

                if (assignmentExists)
                {
                    ModelState.AddModelError("", "This user is already assigned to this role.");

                    ViewBag.Users = new SelectList(await _context.Users.ToListAsync(), "Id", "Email", assignment.UserId);
                    ViewBag.Roles = new SelectList(await _context.Roles.ToListAsync(), "Id", "Name", assignment.RoleId);
                    return View(assignment);
                }
                assignment.AssignedAt = DateTime.UtcNow;
                _context.UserAssignRoles.Add(assignment);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));


            }
            ViewBag.Users = new SelectList(await _context.Users.ToListAsync(), "Id", "Email", assignment.UserId);
            ViewBag.Roles = new SelectList(await _context.Roles.ToListAsync(), "Id", "Name", assignment.RoleId);
            return View(assignment);


        }

        // =================================================================
        //Remove Role
        // =================================================================
        // POST: /UserAssignRole/Revoke
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Revoke(int userId, int roleId)
        {
            var assignment = await _context.UserAssignRoles
                .FirstOrDefaultAsync(uar => uar.UserId == userId && uar.RoleId == roleId);

            if (assignment != null)
            {
                _context.UserAssignRoles.Remove(assignment);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }


    }
}
