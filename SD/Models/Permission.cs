//namespace SD.Models
//{
//    public class Permission
//    {
//    }
//}

// 1. Models/Permission.cs
// 1. Models/Permission.cs
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace SD.Models
{
    public class Permission
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)] // e.g., "User.Delete", "User.Create", "Role.View"
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        [ValidateNever]
        public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    }
}
