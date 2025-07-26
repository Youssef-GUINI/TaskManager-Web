// TaskManager/Models/WorkspaceVM.cs
using System.Collections.Generic;

namespace TaskManager.Models
{
    public class WorkspaceVM
    {
        // Statistiques par priorité
        public int High { get; set; }
        public int Medium { get; set; }
        public int Low { get; set; }

        // Statistiques globales
        public int TotalTasks { get; set; }
        public int CompletedTasks { get; set; }
        public int InProgressTasks { get; set; }
        public int OverdueTasks { get; set; }
        public int CompletedPercent { get; set; }

        // Activité des utilisateurs
        public List<UserActivity> UserActivities { get; set; } = new List<UserActivity>();
        
        // Graphique d'activité
        public List<int> WeeklyCompletion { get; set; } = new List<int>();
    }

    public class UserActivity
    {
        public string UserName { get; set; }
        public string Avatar { get; set; }
        public int CompletedTasks { get; set; }
        public int PendingTasks { get; set; }
        public bool IsActive { get; set; }
    }
}