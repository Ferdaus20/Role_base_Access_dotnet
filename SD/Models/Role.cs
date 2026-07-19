using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace SD.Models
{
    public class Role
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        // Navigation property for the junction table
        // FIX: Add [ValidateNever] so ASP.NET doesn't mark the whole form invalid 
        // when creating a user without instant role records.
        [ValidateNever]
        public ICollection<UserAssignRole> UserAssignRoles { get; set; } = new List<UserAssignRole>();

        // FIX: Add this navigation property so the AccountController can pull permissions
        [ValidateNever]
        public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    }
}
