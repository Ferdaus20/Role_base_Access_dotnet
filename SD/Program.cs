using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using SD.Data;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<MyDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

// =================================================================
// AUTHORIZATION POLICIES SETUP
// =================================================================
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("CanDeleteUsers", policy => policy.RequireClaim("Permission", "User.Delete"));
    options.AddPolicy("CanCreateUsers", policy => policy.RequireClaim("Permission", "User.Create"));
});

// =================================================================
// AUTHENTICATION & LIVE DATABASE COOKIE RE-VALIDATION
// =================================================================
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";       // Fallback redirect if not logged in
        options.AccessDeniedPath = "/Account/Login"; // Redirect if role rules fail
        options.ExpireTimeSpan = TimeSpan.FromMinutes(60);

        // This event runs on every request to keep claims synchronized with the database
        options.Events = new CookieAuthenticationEvents
        {
            OnValidatePrincipal = async context =>
            {
                // 1. Extract the unique User ID from the cookie payload
                var userIdClaim = context.Principal?.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                {
                    context.RejectPrincipal();
                    await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    return;
                }

                // 2. Resolve your DbContext out of the request container instance
                var db = context.HttpContext.RequestServices.GetRequiredService<MyDbContext>();
                int userId = int.Parse(userIdClaim.Value);

                // 3. Perform an inner-join query to find the absolute latest active permissions
                var freshPermissions = await db.UserAssignRoles
                    .Where(uar => uar.UserId == userId)
                    .SelectMany(uar => uar.Role!.RolePermissions)
                    .Select(rp => rp.Permission!.Name)
                    .Distinct()
                    .ToListAsync();

                // 4. Extract current claims identity layer
                var identity = (ClaimsIdentity)context.Principal!.Identity!;

                // 5. Purge outdated permission strings to clear memory bloat
                var existingPermissionClaims = identity.FindAll("Permission").ToList();
                foreach (var claim in existingPermissionClaims)
                {
                    identity.RemoveClaim(claim);
                }

                // 6. Inject the fresh database permission tags directly back into the cookie identity
                foreach (var permName in freshPermissions)
                {
                    identity.AddClaim(new Claim("Permission", permName));
                }
            }
        };
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication(); // <-- CRITICAL PIPELINE LAYER
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=User}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
