using Microsoft.EntityFrameworkCore;
using SD.Models;
using System.Data;


namespace SD.Data
{
    public class MyDbContext : DbContext
    {
        // 1. Constructor passing configuration options to the base DbContext class
        public MyDbContext(DbContextOptions<MyDbContext> options) : base(options)
        {
        }

        // 2. Define the Database Tables
        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<UserAssignRole> UserAssignRoles { get; set; }

        public DbSet<Permission> Permissions { get; set; }

        public DbSet<RolePermission> RolePermissions { get; set; }

    }
}
