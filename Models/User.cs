using System.ComponentModel.DataAnnotations;

namespace TaskManager.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Le nom est requis")]
        [StringLength(100, ErrorMessage = "Le nom ne peut pas dépasser 100 caractères")]
        public string Name { get; set; } = "";

        [Required(ErrorMessage = "L'email est requis")]
        [EmailAddress(ErrorMessage = "Format d'email invalide")]
        [StringLength(200, ErrorMessage = "L'email ne peut pas dépasser 200 caractères")]
        public string Email { get; set; } = "";

        [StringLength(20, ErrorMessage = "Le téléphone ne peut pas dépasser 20 caractères")]
        public string? Phone { get; set; }

        [StringLength(100, ErrorMessage = "Le poste ne peut pas dépasser 100 caractères")]
        public string? Position { get; set; }

        [StringLength(50, ErrorMessage = "Le département ne peut pas dépasser 50 caractères")]
        public string? Department { get; set; }

        [StringLength(500, ErrorMessage = "La bio ne peut pas dépasser 500 caractères")]
        public string? Bio { get; set; }

        [StringLength(200, ErrorMessage = "L'avatar ne peut pas dépasser 200 caractères")]
        public string? AvatarUrl { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public DateTime? LastLoginDate { get; set; }

        // Navigation property pour les tâches assignées à cet utilisateur
        public virtual ICollection<TaskModel>? AssignedTasks { get; set; }
    }
}