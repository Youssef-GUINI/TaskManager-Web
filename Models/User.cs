using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaskManager.Models
{
    public class User : IdentityUser
    {

        [Required(ErrorMessage = "First name is required")]
        [MaxLength(100, ErrorMessage = "First name cannot exceed 100 characters")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last name is required")]
        [MaxLength(100, ErrorMessage = "Last name cannot exceed 100 characters")]
        public string LastName { get; set; } = string.Empty;

        public string Name => $"{FirstName} {LastName}";

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public override string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Recovery email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string RecoveryEmail { get; set; } = string.Empty;

        public string Position { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string Bio { get; set; } = string.Empty;
        public string AvatarUrl { get; set; } = "/images/avatars/default.jpg";
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;

        public virtual ICollection<TaskModel> AssignedTasks { get; set; } = new List<TaskModel>();
        public string? ManagerId { get; set; }  // L'ID du manager (nullable)

    [ForeignKey("ManagerId")]
    public virtual User? Manager { get; set; }  // Référence navigationnelle vers le manager
    }
}


