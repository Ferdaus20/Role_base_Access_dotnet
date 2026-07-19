using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SD.Data;
using SD.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SD.Controllers
{
    [Authorize(Roles = "Admin")] // Keep this secured for Administrators only
    public class PermissionManagementController : Controller
    {
        private readonly MyDbContext _context;

        public PermissionManagementController(MyDbContext context)
        {
            _context = context;
        }

        // =================================================================
        // READ / VIEW ALL PERMISSIONS
        // =================================================================
        // GET: /PermissionManagement
        public async Task<IActionResult> Index()
        {
            var permissions = await _context.Permissions.ToListAsync();
            return View(permissions);
        }

        // =================================================================
        // CREATE PERMISSION
        // =================================================================
        // POST: /PermissionManagement/CreateCustom
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCustom(string name, string? description)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                TempData["ErrorMessage"] = "Permission Name cannot be blank.";
                return RedirectToAction(nameof(Index));
            }

            bool exists = await _context.Permissions.AnyAsync(p => p.Name == name.Trim());
            if (exists)
            {
                TempData["ErrorMessage"] = "This permission tag already exists.";
                return RedirectToAction(nameof(Index));
            }

            var permission = new Permission
            {
                Name = name.Trim(),
                Description = description
            };

            _context.Permissions.Add(permission);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Permission '{name}' successfully created!";
            return RedirectToAction(nameof(Index));
        }

        // =================================================================
        // QUICK SEED LOGIC (UPDATED WITH EDIT & DETAILS PERMISSIONS)
        // =================================================================
        // POST: /PermissionManagement/QuickSeed
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> QuickSeed()
        {
            try
            {
                // 1. Insert core permission rows safely (Includes User.Edit and User.Details)
                var corePermissions = new List<Permission>
                {
                    new Permission { Name = "User.Create", Description = "Allows creating new user accounts" },
                    new Permission { Name = "User.Delete", Description = "Allows deleting user accounts" },
                    new Permission { Name = "User.Edit", Description = "Allows modifying existing user account information" },
                    new Permission { Name = "User.Details", Description = "Allows viewing specific user account profile details" }
                };

                foreach (var perm in corePermissions)
                {
                    bool exists = await _context.Permissions.AnyAsync(p => p.Name == perm.Name);
                    if (!exists)
                    {
                        _context.Permissions.Add(perm);
                    }
                }
                await _context.SaveChangesAsync();

                // 2. Automatically link ALL these permissions to the 'Admin' role matrix ledger
                var adminRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Admin");
                if (adminRole != null)
                {
                    var seededPermissions = await _context.Permissions
                        .Where(p => p.Name == "User.Create" || p.Name == "User.Delete" || p.Name == "User.Edit" || p.Name == "User.Details")
                        .ToListAsync();

                    foreach (var permission in seededPermissions)
                    {
                        bool mappingExists = await _context.RolePermissions
                            .AnyAsync(rp => rp.RoleId == adminRole.Id && rp.PermissionId == permission.Id);

                        if (!mappingExists)
                        {
                            _context.RolePermissions.Add(new RolePermission
                            {
                                RoleId = adminRole.Id,
                                PermissionId = permission.Id
                            });
                        }
                    }
                    await _context.SaveChangesAsync();
                }

                TempData["SuccessMessage"] = "Core system permissions successfully seeded and linked to Admin!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Seeding failed: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        // =================================================================
        // UPDATE (EDIT) ACTIONS
        // =================================================================
        // GET: /PermissionManagement/Edit/5
        [HttpGet("PermissionManagement/Edit/{id:int}")] // Explicit route mapping template token parameters to bypass 404s
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var permission = await _context.Permissions.FindAsync(id);
            if (permission == null)
            {
                return NotFound();
            }

            return View(permission);
        }

        // POST: /PermissionManagement/Edit/5
        [HttpPost("PermissionManagement/Edit/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Permission updatedPermission)
        {
            if (id != updatedPermission.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                // Check if the updated name conflicts with another different permission entry
                bool nameExists = await _context.Permissions.AnyAsync(p => p.Name == updatedPermission.Name.Trim() && p.Id != id);
                if (nameExists)
                {
                    ModelState.AddModelError("Name", "This permission tag string is already taken.");
                    return View(updatedPermission);
                }

                var existingPermission = await _context.Permissions.FindAsync(id);
                if (existingPermission == null)
                {
                    return NotFound();
                }

                existingPermission.Name = updatedPermission.Name.Trim();
                existingPermission.Description = updatedPermission.Description;

                _context.Permissions.Update(existingPermission);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Permission configuration changes updated successfully!";
                return RedirectToAction(nameof(Index));
            }
            return View(updatedPermission);
        }

        // =================================================================
        // DELETE ACTION
        // =================================================================
        // POST: /PermissionManagement/Delete/5
        [HttpPost("PermissionManagement/Delete/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var permission = await _context.Permissions.FindAsync(id);
            if (permission != null)
            {
                // Entity Framework Core will automatically handle cascade constraints 
                // and wipe mapping linkages inside your RolePermissions table first.
                _context.Permissions.Remove(permission);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Permission tag removed out of master directory ledger successfully.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
