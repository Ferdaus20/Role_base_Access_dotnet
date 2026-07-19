using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SD.Data;
using SD.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SD.Controllers
{
    [Authorize(Roles = "Admin")] // Keep this secured for Administrators only
    public class RolePermissionController : Controller
    {
        private readonly MyDbContext _context;

        public RolePermissionController(MyDbContext context)
        {
            _context = context;
        }

        // =================================================================
        // GET: LOAD CONFIGURATION MATRIX PAGE
        // =================================================================
        // FIX: Added explicit attribute tokens to ensure the app routing engine maps /Manage/{id}
        [HttpGet("RolePermission/Manage/{id:int}")]
        public async Task<IActionResult> Manage(int id)
        {
            var role = await _context.Roles.FindAsync(id);
            if (role == null)
            {
                return NotFound();
            }

            // 1. Get all available permissions from the database table
            var allPermissions = await _context.Permissions.ToListAsync();

            // 2. Get the IDs of permissions currently assigned to this specific role
            var assignedPermissionIds = await _context.RolePermissions
                .Where(rp => rp.RoleId == id)
                .Select(rp => rp.PermissionId)
                .ToListAsync();

            ViewBag.RoleName = role.Name;
            ViewBag.RoleId = id;
            ViewBag.AssignedPermissionIds = assignedPermissionIds;

            return View(allPermissions);
        }

        // =================================================================
        // POST: SAVE UPDATED CHECKBOX MATRIX
        // =================================================================
        [HttpPost("RolePermission/Manage/{roleId:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Manage(int roleId, List<int> selectedPermissionIds)
        {
            // 1. Clear out all existing permission mappings for this role to prevent duplicates
            var existingMappings = _context.RolePermissions.Where(rp => rp.RoleId == roleId);
            _context.RolePermissions.RemoveRange(existingMappings);
            await _context.SaveChangesAsync();

            // 2. Insert the new checkbox selections into your RolePermissions junction table
            if (selectedPermissionIds != null && selectedPermissionIds.Any())
            {
                foreach (var permissionId in selectedPermissionIds)
                {
                    _context.RolePermissions.Add(new RolePermission
                    {
                        RoleId = roleId,
                        PermissionId = permissionId
                    });
                }
                await _context.SaveChangesAsync();
            }

            TempData["SuccessMessage"] = "Role permissions matrix updated successfully!";
            return RedirectToAction("Index", "Role");
        }
    }
}
