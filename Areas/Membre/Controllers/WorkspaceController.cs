using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using TaskManager.data;
using Microsoft.AspNetCore.Identity;
using TaskManager.Models;


namespace TaskManager.Areas.Membre.Controllers
{
    [Area("Membre")]
    public class WorkspaceController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

    public WorkspaceController(ApplicationDbContext context,UserManager<User> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

        // public IActionResult Index()
        // {
        //     // Compter les tâches par priorité
        //     int high = _context.Tasks.Count(t => t.Priority == "High");
        //     int medium = _context.Tasks.Count(t => t.Priority == "Medium");
        //     int low = _context.Tasks.Count(t => t.Priority == "Low");

        //     // Compter le total des tâches
        //     int total = high + medium + low;

        //     // Compter les tâches terminées
        //     int done = _context.Tasks.Count(t => t.Status == "Done");

        //     // Calculer le pourcentage de tâches complétées
        //     int percent = (total == 0) ? 0 : (done * 100) / total;

        //     // Générer les données d'activité pour les 9 derniers jours
        //     var activity = new List<int>();
        //     for (int i = 8; i >= 0; i--)
        //     {
        //         var date = DateTime.Now.AddDays(-i).Date;
        //         var tasksOnDate = _context.Tasks.Count(t => t.DueDate.Date == date);
        //         activity.Add(tasksOnDate);
        //     }

        //     // Si pas assez de données, utiliser des données de simulation
        //     if (activity.All(x => x == 0))
        //     {
        //         activity = new List<int> { 2, 3, 1, 4, 3, 5, 2, 6, 4 };
        //     }

        //     // Créer le ViewModel
        //     var vm = new WorkspaceVM
        //     {
        //         High = high,
        //         Medium = medium,
        //         Low = low,
        //         CompletedPercent = percent,
        //         Activity = activity
        //     };

        //     return View("~/Areas/Membre/Views/Workspace/Index.cshtml",vm);


        // }
public async Task<IActionResult> Index()
{
    // Récupérer l'utilisateur connecté
    var user = await _userManager.GetUserAsync(User);
    if (user == null)
    {
        return RedirectToAction("Login", "Account");
    }

    string currentUserId = user.Id;

    // Filtrer les tâches de l'utilisateur connecté
    var userTasks = _context.Tasks.Where(t => t.AssignedToUserId == currentUserId);

    // Statistiques par priorité
    int high = await userTasks.CountAsync(t => t.Priority == "High");
    int medium = await userTasks.CountAsync(t => t.Priority == "Medium");
    int low = await userTasks.CountAsync(t => t.Priority == "Low");

    // Total et complétées
    int total = high + medium + low;
    int done = await userTasks.CountAsync(t => t.Status == "Done");
    int percent = (total == 0) ? 0 : (done * 100) / total;

    // Activité sur les 9 derniers jours
    var activity = new List<int>();
    for (int i = 8; i >= 0; i--)
    {
        var date = DateTime.Now.AddDays(-i).Date;
        int count = await userTasks.CountAsync(t => t.DueDate.Date == date);
        activity.Add(count);
    }

    // Créer l'objet UserActivity
    var userActivity = new UserActivity
    {
        UserName = user.Name,
        Avatar = user.AvatarUrl ?? "/images/default-avatar.jpg",
        CompletedTasks = done,
        PendingTasks = total - done,
        IsActive = true // Vous pouvez ajouter votre propre logique ici
    };

    // ViewModel
    var vm = new WorkspaceVM
    {
        High = high,
        Medium = medium,
        Low = low,
        CompletedPercent = percent,
        Activity = activity,
        UserActivity = userActivity
    };

    return View("~/Areas/Membre/Views/Workspace/Index.cshtml", vm);
}
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            // Récupérer l'email de l'utilisateur connecté
            var userEmail = User.Identity?.Name;

            if (string.IsNullOrEmpty(userEmail))
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                // Récupérer l'utilisateur depuis la base de données
                var currentUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == userEmail);

                if (currentUser == null)
                {
                    return View(new UserProfileVM
                    {
                        Name = "Utilisateur inconnu",
                        Email = userEmail,
                        Avatar = "/images/default-avatar.jpg"
                    });
                }

                // Récupérer les tâches de l'utilisateur
                var userTasks = await _context.Tasks
                    .Where(t => t.AssignedToUserId == currentUser.Id)
                    .ToListAsync();

                // Calculer les statistiques
                var completedTasks = userTasks.Count(t => t.Status == "Completed");
                var inProgressTasks = userTasks.Count(t => t.Status == "In Progress");
                var toDoTasks = userTasks.Count(t => t.Status == "To Do");

                var userProfile = new UserProfileVM
                {
                    Id = currentUser.Id,
                    Name = currentUser.Name,
                    Email = currentUser.Email,
                    Phone = currentUser.PhoneNumber,
                    Location = "Casablanca, Morocco", // Vous pouvez ajouter ce champ dans votre modèle User si nécessaire
                    Role = currentUser.Position,
                    Department = currentUser.Department,
                    Bio = currentUser.Bio,
                    Avatar = currentUser.AvatarUrl,
                    JoinDate = currentUser.CreatedDate,
                    RecoveryEmail = currentUser.RecoveryEmail,

                    // Statistiques (conservées de la version originale)
                    CompletedTasks = completedTasks,
                    InProgressTasks = inProgressTasks,
                    ProjectsCount = 3, // Valeur par défaut comme dans votre code original
                    HighPriorityTasks = userTasks.Count(t => t.Priority == "High"),
                    MediumPriorityTasks = userTasks.Count(t => t.Priority == "Medium"),
                    LowPriorityTasks = userTasks.Count(t => t.Priority == "Low"),
                    ToDoTasks = toDoTasks,
                    InProgressTasksCount = inProgressTasks,
                    CompletedTasksCount = completedTasks,
                    UserTasks = userTasks
                };

                return View("~/Areas/Membre/Views/Workspace/Profile.cshtml",userProfile);
            }
            catch (Exception ex)
            {
                // En cas d'erreur, retourner un profil minimal
                return View(new UserProfileVM
                {
                    Name = "Erreur de chargement",
                    Email = userEmail,
                    Avatar = "/images/default-avatar.jpg"
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