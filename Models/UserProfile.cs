using System.ComponentModel.DataAnnotations;

namespace TaskManager.Models
{
    public class UserProfileVM
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Nom complet")]
        public string Name { get; set; }

        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Phone]
        [Display(Name = "Téléphone")]
        public string Phone { get; set; }

        [Display(Name = "Localisation")]
        public string Location { get; set; }

        [Display(Name = "Rôle")]
        public string Role { get; set; }

        [Display(Name = "Département")]
        public string Department { get; set; }

        [Display(Name = "Biographie")]
        public string Bio { get; set; }

        [Display(Name = "Photo de profil")]
        public string Avatar { get; set; }

        [Display(Name = "Date d'inscription")]
        public DateTime JoinDate { get; set; }

        // Statistiques calculées automatiquement
        public int CompletedTasks { get; set; }
        public int InProgressTasks { get; set; }
        public int ProjectsCount { get; set; }
        public int ProductivityPercentage { get; set; }

        // Activité récente
        public List<RecentActivity> RecentActivities { get; set; } = new List<RecentActivity>();

        // Liste des tâches de l'utilisateur
        public List<TaskModel> UserTasks { get; set; } = new List<TaskModel>();

        // Statistiques des tâches par priorité
        public int HighPriorityTasks { get; set; }
        public int MediumPriorityTasks { get; set; }
        public int LowPriorityTasks { get; set; }

        // Statistiques des tâches par statut
        public int ToDoTasks { get; set; }
        public int InProgressTasksCount { get; set; }
        public int CompletedTasksCount { get; set; }
    }

    public class RecentActivity
    {
        public string Description { get; set; }
        public DateTime Timestamp { get; set; }
        public string Type { get; set; } // "completed", "created", "comment", "updated"
        public string TypeColor { get; set; } // "green", "blue", "orange", "purple"
    }
}