using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaskManager.Models
{
    public class TaskModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Le titre est requis")]
        [StringLength(200, ErrorMessage = "Le titre ne peut pas dépasser 200 caractères")]
        public string Title { get; set; } = "";

        [StringLength(1000, ErrorMessage = "La description ne peut pas dépasser 1000 caractères")]
        public string Description { get; set; } = "";

        [Required(ErrorMessage = "La date d'échéance est requise")]
        public DateTime DueDate { get; set; } = DateTime.Now.AddDays(7);

        [Required(ErrorMessage = "La priorité est requise")]
        public string Priority { get; set; } = "Medium";

        [Required(ErrorMessage = "Le statut est requis")]
        public string Status { get; set; } = "To Do";

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // ✨ NOUVEAUX CHAMPS POUR LIAISON UTILISATEUR
        [Display(Name = "Assigné à")]
        public int? AssignedToUserId { get; set; }

        [Display(Name = "Créé par")]
        public int? CreatedByUserId { get; set; }

        // Navigation properties
        [ForeignKey("AssignedToUserId")]
        public virtual User? AssignedToUser { get; set; }

        [ForeignKey("CreatedByUserId")]
        public virtual User? CreatedByUser { get; set; }
    }
}