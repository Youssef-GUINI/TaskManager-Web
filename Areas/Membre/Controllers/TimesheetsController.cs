using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskManager.data;
using TaskManager.Models;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Identity;

namespace TaskManager.Areas.Membre.Controllers

{
    [Area("Membre")]
        public class TimesheetsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        // ✨ Cache des JsonSerializerOptions pour éviter les créations répétées
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        public TimesheetsController(ApplicationDbContext context,UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // =================== PAGE PRINCIPALE CALENDRIER ===================
        public async Task<IActionResult> Index()
        {
            // Récupérer toutes les tâches avec leurs utilisateurs assignés
            var tasks = await _context.Tasks
                .Include(t => t.AssignedToUser)
                .Include(t => t.CreatedByUser)
                .OrderBy(t => t.DueDate)
                .ToListAsync();

            // Récupérer tous les utilisateurs pour les filtres
            var users = await _context.Users
                .Where(u => u.IsActive)
                .OrderBy(u => u.LastName)
                .ToListAsync();

            // Statistiques rapides
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

            // Convertir les tâches en format FullCalendar
            var calendarEvents = tasks.Select(t => new
            {
                id = t.Id,
                title = t.Title,
                start = t.DueDate.ToString("yyyy-MM-dd"),
                end = t.DueDate.ToString("yyyy-MM-dd"),
                description = t.Description,
                priority = t.Priority,
                status = t.Status,
                assignedTo = t.AssignedToUser?.LastName ?? "Non assigné",
                assignedToId = t.AssignedToUserId,
                createdBy = t.CreatedByUser?.LastName ?? "Inconnu",
                createdDate = t.CreatedDate.ToString("yyyy-MM-dd"),
                backgroundColor = GetTaskColor(t.Priority, t.Status),
                borderColor = GetTaskBorderColor(t.Status),
                textColor = GetTaskTextColor(t.Priority)
            }).ToList();

            // Passer les données à la vue
            ViewBag.CalendarEventsJson = JsonSerializer.Serialize(calendarEvents, JsonOptions);

            ViewBag.Users = users;
            ViewBag.Stats = stats;
            ViewBag.TasksCount = tasks.Count;

            return View("~/Areas/Membre/Views/Timesheets/Index.cshtml",tasks);
        }

        // =================== API POUR RÉCUPÉRER LES TÂCHES EN JSON ===================
[HttpGet]
public async Task<IActionResult> GetTasksJson(DateTime? start = null, DateTime? end = null, string? userId = null)
{
    var query = _context.Tasks
        .Include(t => t.AssignedToUser)
        .Include(t => t.CreatedByUser)
        .AsQueryable();

    // Filtrer par date de début si précisée
    if (start.HasValue)
        query = query.Where(t => t.DueDate >= start.Value);

    // Filtrer par date de fin si précisée
    if (end.HasValue)
        query = query.Where(t => t.DueDate <= end.Value);

    // Filtrer par utilisateur si précisé (string non nul et non vide)
    if (!string.IsNullOrEmpty(userId))
        query = query.Where(t => t.AssignedToUserId == userId);

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
        assignedTo = t.AssignedToUser?.LastName ?? "Non assigné",
        assignedToId = t.AssignedToUserId,
        createdBy = t.CreatedByUser?.LastName ?? "Inconnu",
        backgroundColor = GetTaskColor(t.Priority, t.Status),
        borderColor = GetTaskBorderColor(t.Status),
        textColor = GetTaskTextColor(t.Priority),
        extendedProps = new
        {
            taskId = t.Id,
            description = t.Description ?? "",
            priority = t.Priority,
            status = t.Status,
            assignedTo = t.AssignedToUser?.LastName ?? "Non assigné",
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
                assignedTo = task.AssignedToUser?.LastName ?? "Non assigné",
                assignedToEmail = task.AssignedToUser?.Email ?? "",
                createdBy = task.CreatedByUser?.LastName ?? "Inconnu",
                createdDate = task.CreatedDate.ToString("dd/MM/yyyy HH:mm"),
                backgroundColor = GetTaskColor(task.Priority, task.Status)
            };

            return Json(taskDetails);
        }

        // =================== MISE À JOUR RAPIDE DU STATUT ===================
        [HttpPost]
        public async Task<IActionResult> UpdateTaskStatus(int id, string status)
        {
            var task = await _context.Tasks.FindAsync(id);
            if (task == null)
                return Json(new { success = false, message = "Tâche non trouvée" });

            var oldStatus = task.Status;
            task.Status = status;

            try
            {
                await _context.SaveChangesAsync();
                return Json(new
                {
                    success = true,
                    message = $"Statut changé de '{oldStatus}' à '{status}'",
                    newColor = GetTaskColor(task.Priority, task.Status)
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Erreur: {ex.Message}" });
            }
        }

        // =================== MISE À JOUR DE LA DATE D'ÉCHÉANCE ===================
        [HttpPost]
        public async Task<IActionResult> UpdateTaskDate(int id, DateTime newDate)
        {
            var task = await _context.Tasks.FindAsync(id);
            if (task == null)
                return Json(new { success = false, message = "Tâche non trouvée" });

            var oldDate = task.DueDate;
            task.DueDate = newDate;

            try
            {
                await _context.SaveChangesAsync();
                return Json(new
                {
                    success = true,
                    message = $"Date changée du {oldDate:dd/MM/yyyy} au {newDate:dd/MM/yyyy}"
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Erreur: {ex.Message}" });
            }
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
// GET: Membre/Timesheets/Edit/5
public async Task<IActionResult> Edit(int? id)
{
	if (id == null) return NotFound();

	var taskModel = await _context.Tasks.FindAsync(id);
	if (taskModel == null) return NotFound();

	var currentUser = await _userManager.GetUserAsync(User);
	if (currentUser == null) return RedirectToAction("Login", "Account", new { area = "" });

	// Vérifie si la tâche est assignée au membre connecté
	var isAssignedToMe = taskModel.AssignedToUserId == currentUser.Id;

	if (!isAssignedToMe)
	{
		// Interdit si ce n’est pas sa tâche
		return Forbid();
	}

	// On ne charge pas de liste d’assignés ici, car l’utilisateur ne peut pas modifier l’assignation
	return View("~/Areas/Membre/Views/Timesheets/Edit.cshtml", taskModel);
}
// [HttpPost]
// public async Task<IActionResult> Edit(int id, TaskModel taskModel)
// {
// 	var currentUser = await _userManager.GetUserAsync(User);
// 	if (currentUser == null)
// 		return RedirectToAction("Login", "Account", new { area = "" });

// 	var originalTask = await _context.Tasks.FindAsync(id);
// 	if (originalTask == null)
// 		return NotFound();

// 	// Vérifie que l'utilisateur connecté est bien celui à qui la tâche est assignée
// 	if (originalTask.AssignedToUserId != currentUser.Id)
// 		return Forbid();

// 	if (ModelState.IsValid)
// 	{
// 		// Autoriser uniquement la modification du statut
// 		originalTask.Status = taskModel.Status;
// 		originalTask.LastUpdated = DateTime.Now;

// 		// Si la tâche est marquée comme terminée, on définit la date de complétion
// 		if (taskModel.Status == "Completed" && originalTask.CompletedDate == null)
// 		{
// 			originalTask.CompletedDate = DateTime.Now;
// 		}
// 		else if (taskModel.Status != "Completed")
// 		{
// 			originalTask.CompletedDate = null;
// 		}

// 		// Assigne la tâche à l’utilisateur connecté (dans le cas où elle ne l’était pas)
// 		originalTask.AssignedToUserId = currentUser.Id;

// 		_context.Update(originalTask);
// 		await _context.SaveChangesAsync();

// 		TempData["Success"] = "Statut mis à jour avec succès!";
// 		return RedirectToAction(nameof(Index));
// 	}

// 	// Si ModelState invalide
// 	return View("~/Areas/Membre/Views/Timesheets/Edit.cshtml", taskModel);
// }
[HttpPost]
public async Task<IActionResult> Edit(int id, TaskModel taskModel)
{
    var currentUser = await _userManager.GetUserAsync(User);

    var task = await _context.Tasks
        .Where(t => t.Id == id && t.AssignedToUserId == currentUser.Id)
        .FirstOrDefaultAsync();

    if (task == null)
    {
        return NotFound();
    }

    // Mise à jour du statut uniquement
    task.Status = taskModel.Status;

    await _context.SaveChangesAsync();
    return RedirectToAction("Index");
}

    }
}