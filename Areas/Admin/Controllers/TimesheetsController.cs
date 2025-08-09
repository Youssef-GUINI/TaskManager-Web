using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskManager.data;
using TaskManager.Models;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Rendering;


namespace TaskManager.Areas.Admin.Controllers

{
    [Area("Admin")]
    public class TimesheetsController : Controller
    {
        private readonly ApplicationDbContext _context;

        // ✨ Cache des JsonSerializerOptions pour éviter les créations répétées
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        public TimesheetsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // Récupérer l'utilisateur connecté
            var currentUserEmail = User.Identity?.Name;
            var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == currentUserEmail);

            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            IQueryable<TaskModel> tasksQuery = _context.Tasks
                .Include(t => t.AssignedToUser)
                .Include(t => t.CreatedByUser);

            // Filtrer selon le rôle
            if (User.IsInRole("Admin"))
            {
                var teamMemberIds = await _context.Users
                    .Where(u => u.ManagerId == currentUser.Id)
                    .Select(u => u.Id)
                    .ToListAsync();

                tasksQuery = tasksQuery.Where(t => teamMemberIds.Contains(t.AssignedToUserId));
            }
            else if (!User.IsInRole("SuperAdmin")) // Pour les membres normaux
            {
                tasksQuery = tasksQuery.Where(t => t.AssignedToUserId == currentUser.Id);
            }

            var tasks = await tasksQuery
                .OrderBy(t => t.DueDate)
                .ToListAsync();

            // Préparer les données pour FullCalendar
            var calendarEvents = tasks.Select(t => new
            {
                id = t.Id,
                title = t.Title,
                start = t.DueDate.ToString("yyyy-MM-dd"),
                end = t.DueDate.ToString("yyyy-MM-dd"),
                description = t.Description,
                priority = t.Priority,
                status = t.Status,
                assignedTo = t.AssignedToUser?.Name ?? "Non assigné",
                assignedToId = t.AssignedToUserId,
                createdBy = t.CreatedByUser?.Name ?? "Inconnu",
                createdDate = t.CreatedDate.ToString("yyyy-MM-dd"),
                backgroundColor = GetTaskColor(t.Priority, t.Status),
                borderColor = GetTaskBorderColor(t.Status),
                textColor = GetTaskTextColor(t.Priority)
            }).ToList();

            // Récupérer les utilisateurs pour les filtres
            var users = await _context.Users
                .Where(u => u.IsActive &&
                       (User.IsInRole("SuperAdmin") ||
                        User.IsInRole("Admin") && u.ManagerId == currentUser.Id ||
                        !User.IsInRole("Admin") && u.Id == currentUser.Id))
                .OrderBy(u => u.LastName)
                .ToListAsync();

            // Statistiques
            var today = DateTime.Today;
            var stats = new
            {
                TotalTasks = tasks.Count,
                DueToday = tasks.Count(t => t.DueDate.Date == today),
                Overdue = tasks.Count(t => t.DueDate.Date < today && t.Status != "Completed"),
                ThisWeek = tasks.Count(t => t.DueDate.Date >= today && t.DueDate.Date <= today.AddDays(7)),
                Completed = tasks.Count(t => t.Status == "Completed"),
                InProgress = tasks.Count(t => t.Status == "In Progress"),
                ToDo = tasks.Count(t => t.Status == "To Do")
            };

            ViewBag.CalendarEventsJson = JsonSerializer.Serialize(calendarEvents, JsonOptions);
            ViewBag.Users = users;
            ViewBag.Stats = stats;
            ViewBag.TasksCount = tasks.Count;

            return View("~/Areas/Admin/Views/Timesheets/Index.cshtml",tasks);
        }

[HttpGet]
public async Task<IActionResult> GetTasksJson(DateTime? start = null, DateTime? end = null, int? userId = null)
{
    var query = _context.Tasks
        .Include(t => t.AssignedToUser)
        .Include(t => t.CreatedByUser)
        .AsQueryable();

    // Filtrage des tâches selon les paramètres
    if (start.HasValue)
        query = query.Where(t => t.DueDate >= start.Value);
    if (end.HasValue)
        query = query.Where(t => t.DueDate <= end.Value);
    if (userId.HasValue)
        query = query.Where(t => t.AssignedToUserId == userId.Value.ToString());

    var tasks = await query.ToListAsync();

    var events = tasks.Select(t => new
    {
        id = t.Id,
        title = t.Title,
        start = t.DueDate.ToString("yyyy-MM-dd"),
        end = t.DueDate.ToString("yyyy-MM-dd"),
        description = t.Description ?? "",
        priority = t.Priority,
        status = t.Status,
        assignedTo = t.AssignedToUser?.Name ?? "Non assigné",
        assignedToId = t.AssignedToUserId,
        createdBy = t.CreatedByUser?.Name ?? "Inconnu",
        backgroundColor = GetTaskColor(t.Priority, t.Status),
        borderColor = GetTaskBorderColor(t.Status),
        textColor = GetTaskTextColor(t.Priority),
        extendedProps = new
        {
            taskId = t.Id,
            description = t.Description ?? "",
            priority = t.Priority,
            status = t.Status,
            assignedTo = t.AssignedToUser?.Name ?? "Non assigné",
            createdDate = t.CreatedDate.ToString("dd/MM/yyyy")
        }
    }).ToList();

    return Json(events);
}
        // =================== DÉTAILS D'UNE TÂCHE ===================
        [HttpGet]
        public async Task<IActionResult> GetTaskDetails(int id)
        {
            var task = await _context.Tasks
                .Include(t => t.AssignedToUser)
                .Include(t => t.CreatedByUser)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (task == null)
                return NotFound();

            var taskDetails = new
            {
                id = task.Id,
                title = task.Title,
                description = task.Description ?? "",
                dueDate = task.DueDate.ToString("dd/MM/yyyy"),
                priority = task.Priority,
                status = task.Status,
                assignedTo = task.AssignedToUser?.Name ?? "Non assigné",
                assignedToEmail = task.AssignedToUser?.Email ?? "",
                createdBy = task.CreatedByUser?.Name ?? "Inconnu",
                createdDate = task.CreatedDate.ToString("dd/MM/yyyy HH:mm"),
                backgroundColor = GetTaskColor(task.Priority, task.Status)
            };

            return Json(taskDetails);
        }

        //=================== MISE À JOUR RAPIDE DU STATUT ===================
        [HttpPost]
        public async Task<IActionResult> UpdateTaskStatus(int id, string status)
        {
            var task = await _context.Tasks.FindAsync(id);
            if (task == null)
                return Json(new { success = false, message = "Tâche non trouvée" });

            var oldStatus = task.Status;
            task.Status = status;
            await _context.SaveChangesAsync();

            // ➡ Renvoyez les Nouvelles données pour mise à jour
            return Json(new
            {
                success = true,
                taskId = task.Id,
                newStatus = status,
                newColor = GetTaskColor(task.Priority, status),
                // ➡ Ajoutez les stats mises à jour si nécessaire
                stats = new
                {
                    overdue = await _context.Tasks.CountAsync(t => t.DueDate < DateTime.Today && t.Status != "Completed"),
                    dueToday = await _context.Tasks.CountAsync(t => t.DueDate.Date == DateTime.Today)
                }
            });
        }

        // =================== MISE À JOUR DE LA DATE D'ÉCHÉANCE ===================
        [HttpPost]
        public async Task<IActionResult> UpdateTaskDate([FromBody] UpdateTaskRequest request)
        {
            // Debug: Affiche les données reçues
            Console.WriteLine($"Reçu - ID: {request?.Id}, Date: {request?.NewDate}");

            if (request == null || request.Id <= 0)
                return Json(new { success = false, message = "Données invalides" });

            var task = await _context.Tasks.FindAsync(request.Id);

            if (task == null)
            {
                // Debug: Affiche toutes les tâches existantes
                var existingTasks = await _context.Tasks.ToListAsync();
                Console.WriteLine($"Tâches existantes: {JsonSerializer.Serialize(existingTasks)}");

                return Json(new { success = false, message = $"Tâche ID {request.Id} non trouvée" });
            }

            try
            {
                task.DueDate = request.NewDate;
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur: {ex.Message}");
                return Json(new { success = false, message = ex.Message });
            }
        }

        public class UpdateTaskRequest
        {
            public int Id { get; set; }
            public DateTime NewDate { get; set; }
        }
        // =================== FONCTIONS UTILITAIRES POUR LES COULEURS (STATIC) ===================
        private static string GetTaskColor(string priority, string status)
        {
            // Si la tâche est terminée, toujours vert
            if (status == "Completed")
                return "#28a745"; // Vert

            // Sinon, couleur selon la priorité
            return priority switch
            {
                "High" => "#dc3545",    // Rouge
                "Medium" => "#ffc107",  // Jaune/Orange
                "Low" => "#17a2b8",     // Bleu
                _ => "#6c757d"          // Gris par défaut
            };
        }

        private static string GetTaskBorderColor(string status)
        {
            return status switch
            {
                "Completed" => "#1e7e34",
                "In Progress" => "#0056b3",
                "To Do" => "#6c757d",
                _ => "#6c757d"
            };
        }

        private static string GetTaskTextColor(string priority)
        {
            return priority switch
            {
                "High" => "#ffffff",
                "Medium" => "#000000",
                "Low" => "#ffffff",
                _ => "#ffffff"
            };
        }
    }
}