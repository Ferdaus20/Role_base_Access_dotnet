using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System;
using System.ComponentModel.DataAnnotations;

namespace SD.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property for the junction table
        // FIX: Add [ValidateNever] so ASP.NET doesn't mark the whole form invalid 
        // when creating a user without instant role records.
        [ValidateNever]
        public ICollection<UserAssignRole> UserAssignRoles { get; set; } = new List<UserAssignRole>();
    }
}
