using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SD.Data;
using SD.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SD.Controllers
{
    public class AccountController : Controller
    {
        private readonly MyDbContext _context;

        public AccountController(MyDbContext context)
        {
            _context = context;
        }

        // =================================================================
        // REGISTER ACTIONS
        // =================================================================
        // GET: /Account/Register
        public IActionResult Register()
        {
            return View();
        }

        // POST: /Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(User user, string plainPassword)
        {
            if (string.IsNullOrWhiteSpace(user.Email) || string.IsNullOrWhiteSpace(plainPassword))
            {
                ModelState.AddModelError("", "Email and Password are required fields.");
                return View(user);
            }

            ModelState.Clear();

            bool emailExists = await _context.Users.AnyAsync(u => u.Email == user.Email);
            if (emailExists)
            {
                ModelState.AddModelError("Email", "This email address is already registered.");
                return View(user);
            }

            try
            {
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(plainPassword);
                user.CreatedAt = DateTime.UtcNow;

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // Automatically assign a default basic "User" role if it exists in the database
                var defaultRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "User");
                if (defaultRole != null)
                {
                    var assignment = new UserAssignRole { UserId = user.Id, RoleId = defaultRole.Id };
                    _context.UserAssignRoles.Add(assignment);
                    await _context.SaveChangesAsync();
                }

                return RedirectToAction(nameof(Login));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Registration failed: {ex.Message}");
                return View(user);
            }
        }

        // =================================================================
        // LOGIN ACTIONS
        // =================================================================
        // GET: /Account/Login
        public IActionResult Login()
        {
            return View();
        }

        // POST: /Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string password)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                ModelState.AddModelError("", "Please fill in all input fields.");
                return View();
            }

            // 1. Fetch user record from database
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

            // 2. Verify credentials using BCrypt matching
            if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            {
                ModelState.AddModelError("", "Invalid login credentials provided.");
                return View();
            }

            // 3. Query all assigned roles for this user from the junction table
            var userRoles = await _context.UserAssignRoles
                .Where(uar => uar.UserId == user.Id)
                .Select(uar => uar.Role!.Name)
                .ToListAsync();

            // 4. Query all dynamic permissions tied to those roles (Inner Join via SelectMany)
            var userPermissions = await _context.UserAssignRoles
                .Where(uar => uar.UserId == user.Id)
                .SelectMany(uar => uar.Role!.RolePermissions)
                .Select(rp => rp.Permission!.Name)
                .Distinct()
                .ToListAsync();

            // 5. Create local security claims payload identity
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Email)
            };

            // Bake every assigned role into user identity tokens dynamically
            foreach (var roleName in userRoles)
            {
                claims.Add(new Claim(ClaimTypes.Role, roleName));
            }

            // Bake every extracted database permission name as a custom "Permission" claim type
            foreach (var permissionName in userPermissions)
            {
                claims.Add(new Claim("Permission", permissionName));
            }

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties { IsPersistent = true };

            // 6. Issue the browser authentication cookie wrapper
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);

            // Redirect to User/Index to match your Program.cs default pattern
            return RedirectToAction("Index", "User");
        }

        // =================================================================
        // LOGOUT ACTION
        // =================================================================
        // POST: /Account/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction(nameof(Login));
        }
    }
}
