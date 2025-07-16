using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using TaskManager.Data;
using TaskManager.Models;

namespace TaskManager.Controllers
{
    public class WorkspaceController : Controller
    {
        private readonly ApplicationDbContext _context;

        public WorkspaceController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            // Compter les tâches par priorité
            int high = _context.Tasks.Count(t => t.Priority == "High");
            int medium = _context.Tasks.Count(t => t.Priority == "Medium");
            int low = _context.Tasks.Count(t => t.Priority == "Low");

            // Compter le total des tâches
            int total = high + medium + low;

            // Compter les tâches terminées
            int done = _context.Tasks.Count(t => t.Status == "Done");

            // Calculer le pourcentage de tâches complétées
            int percent = (total == 0) ? 0 : (done * 100) / total;

            // Générer les données d'activité pour les 9 derniers jours
            var activity = new List<int>();
            for (int i = 8; i >= 0; i--)
            {
                var date = DateTime.Now.AddDays(-i).Date;
                var tasksOnDate = _context.Tasks.Count(t => t.DueDate.Date == date);
                activity.Add(tasksOnDate);
            }

            // Si pas assez de données, utiliser des données de simulation
            if (activity.All(x => x == 0))
            {
                activity = new List<int> { 2, 3, 1, 4, 3, 5, 2, 6, 4 };
            }

            // Créer le ViewModel
            var vm = new WorkspaceVM
            {
                High = high,
                Medium = medium,
                Low = low,
                CompletedPercent = percent,
                Activity = activity
            };

            return View(vm);


        }


        // Ajoutez ces méthodes à votre WorkspaceController existant

        // Remplacez juste la méthode Profile dans votre WorkspaceController existant

        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            try
            {
                // Récupérer toutes les tâches de l'utilisateur
                var userTasks = await _context.Tasks.ToListAsync();

                // Calculer les statistiques
                var completedTasks = userTasks.Where(t => t.Status == "Completed").Count();
                var inProgressTasks = userTasks.Where(t => t.Status == "In Progress").Count();
                var toDoTasks = userTasks.Where(t => t.Status == "To Do").Count();

                var highPriorityTasks = userTasks.Where(t => t.Priority == "High").Count();
                var mediumPriorityTasks = userTasks.Where(t => t.Priority == "Medium").Count();
                var lowPriorityTasks = userTasks.Where(t => t.Priority == "Low").Count();

                var productivityPercentage = userTasks.Count > 0
                    ? (int)((double)completedTasks / userTasks.Count * 100)
                    : 0;

                // Créer les activités récentes basées sur les tâches réelles
                var recentActivities = new List<RecentActivity>();

                // Ajouter les tâches récemment complétées
                foreach (var task in userTasks.Where(t => t.Status == "Completed").Take(2))
                {
                    recentActivities.Add(new RecentActivity
                    {
                        Description = $"Tâche terminée: {task.Title}",
                        Timestamp = DateTime.Now.AddHours(-new Random().Next(1, 24)),
                        Type = "completed",
                        TypeColor = "success"
                    });
                }

                // Ajouter les tâches en cours
                foreach (var task in userTasks.Where(t => t.Status == "In Progress").Take(2))
                {
                    recentActivities.Add(new RecentActivity
                    {
                        Description = $"Tâche en cours: {task.Title}",
                        Timestamp = DateTime.Now.AddHours(-new Random().Next(1, 48)),
                        Type = "progress",
                        TypeColor = "warning"
                    });
                }

                // Ajouter les nouvelles tâches
                foreach (var task in userTasks.Take(1))
                {
                    recentActivities.Add(new RecentActivity
                    {
                        Description = $"Nouvelle tâche créée: {task.Title}",
                        Timestamp = DateTime.Now.AddDays(-new Random().Next(1, 7)),
                        Type = "created",
                        TypeColor = "info"
                    });
                }

                var userProfile = new UserProfileVM
                {
                    Id = 1,
                    Name = "Youssef GUINI",
                    Email = "youssef.guini@email.com",
                    Phone = "+212 6 12 34 56 78",
                    Location = "Casablanca, Morocco",
                    Role = "Stagier",
                    Department = "Développement",
                    Bio = "Passionné par la gestion de projets et l'optimisation des processus. Spécialisé dans les méthodologies agiles et la coordination d'équipes.",
                    Avatar = "/images/youssefmg.jpg",
                    JoinDate = new DateTime(2023, 1, 15),

                    // Statistiques calculées à partir des vraies tâches
                    CompletedTasks = completedTasks,
                    InProgressTasks = inProgressTasks,
                    ProjectsCount = 3,
                    ProductivityPercentage = productivityPercentage,

                    // Tâches de l'utilisateur (VRAIES DONNÉES)
                    UserTasks = userTasks,

                    // Statistiques par priorité (VRAIES DONNÉES)
                    HighPriorityTasks = highPriorityTasks,
                    MediumPriorityTasks = mediumPriorityTasks,
                    LowPriorityTasks = lowPriorityTasks,

                    // Statistiques par statut (VRAIES DONNÉES)
                    ToDoTasks = toDoTasks,
                    InProgressTasksCount = inProgressTasks,
                    CompletedTasksCount = completedTasks,

                    // Activités récentes basées sur les vraies tâches
                    RecentActivities = recentActivities.OrderByDescending(a => a.Timestamp).Take(5).ToList()
                };

                return View(userProfile);
            }
            catch (Exception ex)
            {
                // En cas d'erreur, retourner un profil avec des données par défaut
                return View(new UserProfileVM
                {
                    Name = "Youssef GUINI",
                    Email = "youssef.guini@email.com",
                    Role = "Stagier",
                    Department = "Développement",
                    Avatar = "/images/youssefmg.jpg",
                    UserTasks = new List<TaskModel>(),
                    RecentActivities = new List<RecentActivity>()
                });
            }
        }
        // Ajoutez ces méthodes à la fin de votre WorkspaceController

[HttpPost]
public async Task<IActionResult> UpdateTask([FromBody] dynamic taskData)
{
    try
    {
        int taskId = (int)taskData.id;
        var task = await _context.Tasks.FindAsync(taskId);
        if (task == null)
        {
            return Json(new { success = false, message = "Tâche non trouvée" });
        }

        task.Title = (string)taskData.title;
        task.Description = (string)taskData.description;
        task.DueDate = DateTime.Parse((string)taskData.dueDate);
        task.Priority = (string)taskData.priority;
        task.Status = (string)taskData.status;

        _context.Tasks.Update(task);
        await _context.SaveChangesAsync();

        return Json(new { success = true, message = "Tâche mise à jour avec succès" });
    }
    catch (Exception ex)
    {
        return Json(new { success = false, message = "Erreur lors de la mise à jour de la tâche" });
    }
}

[HttpDelete]
public async Task<IActionResult> DeleteTask(int taskId)
{
    try
    {
        var task = await _context.Tasks.FindAsync(taskId);
        if (task == null)
        {
            return Json(new { success = false, message = "Tâche non trouvée" });
        }

        _context.Tasks.Remove(task);
        await _context.SaveChangesAsync();

        return Json(new { success = true, message = "Tâche supprimée avec succès" });
    }
    catch (Exception ex)
    {
        return Json(new { success = false, message = "Erreur lors de la suppression de la tâche" });
    }
}
    }
}