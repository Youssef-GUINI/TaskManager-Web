// Remplacez votre WorkspaceVM par cette version

using System.Collections.Generic;

namespace TaskManager.Models
{
    public class WorkspaceVM
    {
        // Statistiques par priorité (existantes)
        public int High { get; set; }
        public int Medium { get; set; }
        public int Low { get; set; }

        // Statistiques générales (nouvelles)
        public int TotalTasks { get; set; }
        public int CompletedTasks { get; set; }
        public int InProgressTasks { get; set; }
        public int ToDoTasks { get; set; }
        public int CompletedPercent { get; set; }

        // Données pour le graphique (existante)
        public List<int> Activity { get; set; } = new List<int>();

        // Listes de tâches (nouvelles)
        public List<TaskModel> RecentTasks { get; set; } = new List<TaskModel>();
        public List<TaskModel> UrgentTasks { get; set; } = new List<TaskModel>();
    }
}