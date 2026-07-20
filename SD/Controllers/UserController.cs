


using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SD.Data;
using SD.Models;
using System;
using System.Threading.Tasks;

namespace SD.Controllers
{
    public class UserController : Controller
    {
        private readonly MyDbContext _context;

        // Inject Database Context
        public UserController(MyDbContext context)
        {
            _context = context;
        }

        // =================================================================
        // READ / VIEW ALL USERS
        // =================================================================
        // GET: /User
        public async Task<IActionResult> Index()
        {
            var users = await _context.Users.ToListAsync();
            return View(users);
        }

        // =================================================================
        // CREATE / REGISTER ACTIONS
        // =================================================================
        // GET: /User/Create

        [Authorize(Policy = "CanCreateUsers")]
        public IActionResult Create()
        {
            return View();
        }

        // POST: /User/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(User user, string plainPassword)
        {
            // 1. Manually check the critical fields we actually need
            if (string.IsNullOrWhiteSpace(user.Email) || string.IsNullOrWhiteSpace(plainPassword))
            {
                ModelState.AddModelError("", "Both Email and Password are required fields.");
                return View(user);
            }

            // 2. Clear any automated framework validation noise (like PasswordHash structural rules)
            ModelState.Clear();

            // 3. Verify the email address does not already exist in the database
            bool emailExists = await _context.Users.AnyAsync(u => u.Email == user.Email);
            if (emailExists)
            {
                ModelState.AddModelError("Email", "This email address is already registered.");
                return View(user);
            }

            try
            {
                // 4. Securely hash the plain password string using BCrypt
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(plainPassword);
                user.CreatedAt = DateTime.UtcNow;

                // 5. Force add directly to the database context bypassing model state flags
                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // Redirect user back to index view layout after successful creation
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                // Writes the error reason directly to your Visual Studio Output logging window
                System.Diagnostics.Debug.WriteLine($"DB ERROR: {ex.Message}");
                ModelState.AddModelError("", $"Database saving failed: {ex.InnerException?.Message ?? ex.Message}");
                return View(user);
            }
        }

        // =================================================================
        // UPDATE / EDIT ACTIONS
        // =================================================================
        // GET: /User/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }
            return View(user);
        }

        // POST: /User/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, User updatedUser, string? newPassword)
        {
            if (id != updatedUser.Id)
            {
                return NotFound();
            }

            var existingUser = await _context.Users.FindAsync(id);
            if (existingUser == null)
            {
                return NotFound();
            }

            bool emailExists = await _context.Users.AnyAsync(u => u.Email == updatedUser.Email && u.Id != id);
            if (emailExists)
            {
                ModelState.AddModelError("Email", "This email address is already taken.");
                return View(updatedUser);
            }

            ModelState.Remove(nameof(updatedUser.PasswordHash));

            if (ModelState.IsValid)
            {
                existingUser.Email = updatedUser.Email;

                if (!string.IsNullOrWhiteSpace(newPassword))
                {
                    existingUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
                }

                _context.Users.Update(existingUser);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            return View(updatedUser);
        }

        // =================================================================
        // DELETE ACTIONS
        // =================================================================
        // GET: /User/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users.FirstOrDefaultAsync(m => m.Id == id);
            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        // POST: /User/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }
    }
}

