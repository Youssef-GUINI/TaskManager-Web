using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using TaskManager.data;
using TaskManager.Models;
using Microsoft.AspNetCore.Identity;


namespace TaskManager.Areas.Admin.Controllers

{
    [Area("Admin")]
    public class WorkspaceController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<WorkspaceController> _logger;

        private readonly SignInManager<User> _signInManager;
        private readonly UserManager<User> _userManager;

        public WorkspaceController(
            ApplicationDbContext context,
            ILogger<WorkspaceController> logger,
            SignInManager<User> signInManager,
            UserManager<User> userManager)
        {
            _context = context;
            _logger = logger;
            _signInManager = signInManager;
            _userManager = userManager;
        }
        [HttpGet]
        public IActionResult Index()
        {
            // Récupérer l'email de l'utilisateur connecté
            var userEmail = User.Identity?.Name;

            if (string.IsNullOrEmpty(userEmail))
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                // Récupérer l'utilisateur connecté depuis la base de données
                var currentUser = _context.Users
                    .Include(u => u.Manager) // Inclure le manager si besoin
                    .FirstOrDefault(u => u.Email == userEmail);

                if (currentUser == null)
                {
                    return RedirectToAction("Login", "Account");
                }

                // Déterminer les utilisateurs à afficher
                List<User> usersToDisplay;
                List<TaskModel> tasksToDisplay;

                if (User.IsInRole("Admin"))
                {
                    // Si c'est un admin, on récupère seulement les membres dont il est le manager
                    usersToDisplay = _context.Users
                        .Where(u => u.ManagerId == currentUser.Id)
                        .ToList();

                    // Récupérer les tâches de ces membres seulement
                    tasksToDisplay = _context.Tasks
                        .Include(t => t.AssignedToUser)
                        .Where(t => usersToDisplay.Select(u => u.Id).Contains(t.AssignedToUserId))
                        .ToList();
                }
                else
                {
                    // Si c'est un membre normal, on récupère seulement ses propres données
                    usersToDisplay = new List<User> { currentUser };
                    tasksToDisplay = _context.Tasks
                        .Include(t => t.AssignedToUser)
                        .Where(t => t.AssignedToUserId == currentUser.Id)
                        .ToList();
                }

                var oneWeekAgo = DateTime.Now.AddDays(-7);

                // Calculer les statistiques basées sur les tâches filtrées
                var vm = new WorkspaceVM
                {
                    High = tasksToDisplay.Count(t => t.Priority == "High"),
                    Medium = tasksToDisplay.Count(t => t.Priority == "Medium"),
                    Low = tasksToDisplay.Count(t => t.Priority == "Low"),
                    TotalTasks = tasksToDisplay.Count,
                    CompletedTasks = tasksToDisplay.Count(t => t.Status == "Completed"),
                    InProgressTasks = tasksToDisplay.Count(t => t.Status == "InProgress"),
                    OverdueTasks = tasksToDisplay.Count(t => t.DueDate < DateTime.Now && t.Status != "Completed"),
                    CompletedPercent = tasksToDisplay.Count > 0 ?
                        (int)((double)tasksToDisplay.Count(t => t.Status == "Completed") / tasksToDisplay.Count * 100) : 0,
                    UserActivities = usersToDisplay.Select(u => new UserActivity
                    {
                        UserName = u.Name,
                        Avatar = u.AvatarUrl,
                        CompletedTasks = tasksToDisplay.Count(t => t.AssignedToUserId == u.Id && t.Status == "Completed"),
                        PendingTasks = tasksToDisplay.Count(t => t.AssignedToUserId == u.Id && t.Status != "Completed"),
                        IsActive = tasksToDisplay.Any(t => t.AssignedToUserId == u.Id &&
                                                       (t.LastUpdated > oneWeekAgo || t.CreatedDate > oneWeekAgo))
                    }).ToList(),
                    WeeklyCompletion = Enumerable.Range(0, 7)
                        .Select(i => tasksToDisplay.Count(t => t.CompletedDate?.Date == DateTime.Now.AddDays(-i).Date))
                        .ToList()
                };

                // Passer l'utilisateur courant à la vue
                ViewBag.CurrentUser = new
                {
                    Name = currentUser.Name,
                    AvatarUrl = currentUser.AvatarUrl ?? "/images/default-avatar.jpg",
                    IsAdmin = User.IsInRole("Admin")
                };
return View("~/Areas/Admin/Views/Workspace/Index.cshtml", vm);

                // return View(vm);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du chargement du tableau de bord");
                return View("Error");
            }
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

                return View("~/Areas/Admin/Views/Workspace/Profile.cshtml",userProfile);
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
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            // return RedirectToAction("Login", "Account");
            return RedirectToAction("Login", "Account", new { area = "" });

        }
    }
}