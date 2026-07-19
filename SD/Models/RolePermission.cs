//namespace SD.Models
//{
//    public class RolePermission
//    {
//    }
//}

// 2. Models/RolePermission.cs (Junction Table)
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SD.Models
{
    [PrimaryKey(nameof(RoleId), nameof(PermissionId))]
    public class RolePermission
    {
        public int RoleId { get; set; }
        [ForeignKey("RoleId")]
        public Role? Role { get; set; }

        public int PermissionId { get; set; }
        [ForeignKey("PermissionId")]
        public Permission? Permission { get; set; }
    }
}
