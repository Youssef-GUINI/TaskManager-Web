using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using TaskManager.data;
using TaskManager.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Text;

namespace TaskManager.Controllers
{
    public class TasksController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        public TasksController(ApplicationDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // =================== AFFICHER LA LISTE AVEC FILTRES ET TRIAGE ===================
        public async Task<IActionResult> Index(string sortBy = "Id", string sortOrder = "desc",
            string filterStatus = "", string filterPriority = "", string filterUser = "", string search = "")
        {
            // Récupérer l'utilisateur connecté
            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var query = _context.Tasks
                .Include(t => t.AssignedToUser)
                .Include(t => t.CreatedByUser)
                .AsQueryable();

            // Filtrer selon le rôle
            if (User.IsInRole("Admin"))
            {
                var teamMemberIds = await _context.Users
                    .Where(u => u.ManagerId == currentUser.Id)
                    .Select(u => u.Id)
                    .ToListAsync();

                query = query.Where(t => teamMemberIds.Contains(t.AssignedToUserId));
            }
            else if (!User.IsInRole("SuperAdmin"))
            {
                query = query.Where(t => t.AssignedToUserId == currentUser.Id);
            }

            // Appliquer les filtres
            if (!string.IsNullOrEmpty(filterStatus))
                query = query.Where(t => t.Status == filterStatus);

            if (!string.IsNullOrEmpty(filterPriority))
                query = query.Where(t => t.Priority == filterPriority);

            if (!string.IsNullOrEmpty(filterUser))
                query = query.Where(t => t.AssignedToUserId == filterUser);

            if (!string.IsNullOrEmpty(search))
                query = query.Where(t => t.Title.Contains(search) ||
                                       (t.Description != null && t.Description.Contains(search)));

            // Trier
            query = sortBy.ToLower() switch
            {
                "title" => sortOrder == "asc" ? query.OrderBy(t => t.Title) : query.OrderByDescending(t => t.Title),
                "duedate" => sortOrder == "asc" ? query.OrderBy(t => t.DueDate) : query.OrderByDescending(t => t.DueDate),
                "priority" => sortOrder == "asc" ? query.OrderBy(t => t.Priority) : query.OrderByDescending(t => t.Priority),
                "status" => sortOrder == "asc" ? query.OrderBy(t => t.Status) : query.OrderByDescending(t => t.Status),
                "assignedto" => sortOrder == "asc" ? query.OrderBy(t => t.AssignedToUser.Name) : query.OrderByDescending(t => t.AssignedToUser.Name),
                "createddate" => sortOrder == "asc" ? query.OrderBy(t => t.CreatedDate) : query.OrderByDescending(t => t.CreatedDate),
                _ => sortOrder == "asc" ? query.OrderBy(t => t.Id) : query.OrderByDescending(t => t.Id)
            };

            var tasks = await query.ToListAsync();

            // Préparer les données pour la vue
            var users = await GetAssignableUsers(currentUser);

            ViewBag.Users = new SelectList(users, "Id", "Name");
            ViewBag.SortBy = sortBy;
            ViewBag.SortOrder = sortOrder;
            ViewBag.FilterStatus = filterStatus;
            ViewBag.FilterPriority = filterPriority;
            ViewBag.FilterUser = filterUser;
            ViewBag.Search = search;

            ViewBag.Stats = new
            {
                Total = tasks.Count,
                ToDo = tasks.Count(t => t.Status == "To Do"),
                InProgress = tasks.Count(t => t.Status == "In Progress"),
                Completed = tasks.Count(t => t.Status == "Completed"),
                HighPriority = tasks.Count(t => t.Priority == "High"),
                Overdue = tasks.Count(t => t.DueDate < DateTime.Today && t.Status != "Completed")
            };

            return View(tasks);
        }

        // =================== CRÉATION DE TÂCHE ===================
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await LoadFilteredUsers();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(TaskModel task)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            if (ModelState.IsValid)
            {
                // Validation de l'assignation
                if (!string.IsNullOrEmpty(task.AssignedToUserId))
                {
                    var allowedUsers = await GetAssignableUsers(currentUser);
                    if (!allowedUsers.Any(u => u.Id == task.AssignedToUserId))
                    {
                        ModelState.AddModelError("AssignedToUserId", "Vous ne pouvez pas assigner à cet utilisateur");
                        await LoadFilteredUsers();
                        return View(task);
                    }
                }

                task.CreatedDate = DateTime.Now;
                task.CreatedByUserId = currentUser.Id;
                task.AssignedToUserId ??= currentUser.Id; // Par défaut, s'assigner à soi-même

                _context.Tasks.Add(task);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Tâche créée avec succès !";
                return RedirectToAction("Index");
            }

            await LoadFilteredUsers();
            return View(task);
        }

        // =================== ÉDITION DE TÂCHE ===================
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var task = await _context.Tasks
                .Include(t => t.AssignedToUser)
                .Include(t => t.CreatedByUser)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (task == null)
            {
                TempData["Error"] = "Tâche non trouvée";
                return RedirectToAction("Index");
            }

            await LoadFilteredUsers();
            return View(task);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(TaskModel task)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            if (ModelState.IsValid)
            {
                // Validation de l'assignation
                if (!string.IsNullOrEmpty(task.AssignedToUserId))
                {
                    var allowedUsers = await GetAssignableUsers(currentUser);
                    if (!allowedUsers.Any(u => u.Id == task.AssignedToUserId))
                    {
                        ModelState.AddModelError("AssignedToUserId", "Vous ne pouvez pas assigner à cet utilisateur");
                        await LoadFilteredUsers();
                        return View(task);
                    }
                }

                try
                {
                    _context.Tasks.Update(task);
                    await _context.SaveChangesAsync();

                    TempData["Success"] = "Tâche modifiée avec succès !";
                    return RedirectToAction("Index");
                }
                catch (DbUpdateConcurrencyException)
                {
                    TempData["Error"] = "Erreur lors de la modification";
                    return RedirectToAction("Index");
                }
            }

            await LoadFilteredUsers();
            return View(task);
        }

        // =================== SUPPRESSION DE TÂCHE ===================
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var task = await _context.Tasks
                .Include(t => t.AssignedToUser)
                .Include(t => t.CreatedByUser)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (task == null)
            {
                TempData["Error"] = "Tâche non trouvée";
                return RedirectToAction("Index");
            }
            return View(task);
        }

        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var task = await _context.Tasks.FindAsync(id);
            if (task != null)
            {
                _context.Tasks.Remove(task);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Tâche supprimée avec succès !";
            }
            return RedirectToAction("Index");
        }

        // =================== MÉTHODES UTILITAIRES ===================
        private async Task LoadFilteredUsers()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var users = await GetAssignableUsers(currentUser);

            ViewBag.AssignedToUsers = new SelectList(users, "Id", "Name");
            ViewBag.AllUsers = users;
        }

        private async Task<List<User>> GetAssignableUsers(User currentUser)
        {
            IQueryable<User> query = _context.Users.Where(u => u.IsActive);

            if (User.IsInRole("Admin"))
            {
                query = query.Where(u => u.ManagerId == currentUser.Id);
            }
            else if (!User.IsInRole("SuperAdmin"))
            {
                query = query.Where(u => u.Id == currentUser.Id);
            }

            return await query.OrderBy(u => u.FirstName).ToListAsync();
        }

        // (le reste des méthodes existantes comme UpdateTaskStatus, ExportPdf, etc. reste inchangé)
        //=================== ACTIONS AJAX EXISTANTES ===================
        // [HttpPost]
        // public async Task<IActionResult> UpdateStatus(int id, string status)
        // {
        //     var task = await _context.Tasks.FindAsync(id);
        //     if (task != null)
        //     {
        //         task.Status = status;
        //         await _context.SaveChangesAsync();
        //         return Json(new { success = true });
        //     }
        //     return Json(new { success = false });
        // }

        // [HttpPost]
        // public async Task<IActionResult> UpdatePriority(int id, string priority)
        // {
        //     var task = await _context.Tasks.FindAsync(id);
        //     if (task != null)
        //     {
        //         task.Priority = priority;
        //         await _context.SaveChangesAsync();
        //         return Json(new { success = true });
        //     }
        //     return Json(new { success = false });
        // }

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
        // [HttpPost]
        // public async Task<IActionResult> UpdateTaskStatus(int id, string status)
        // {
        //     var task = await _context.Tasks.FindAsync(id);
        //     if (task == null)
        //         return Json(new { success = false, message = "Tâche non trouvée" });

        //     var oldStatus = task.Status;
        //     task.Status = status;

        //     try
        //     {
        //         await _context.SaveChangesAsync();
        //         return Json(new
        //         {
        //             success = true,
        //             message = $"Statut changé de '{oldStatus}' à '{status}'",
        //             newColor = GetTaskColor(task.Priority, task.Status)
        //         });
        //     }
        //     catch (Exception ex)
        //     {
        //         return Json(new { success = false, message = $"Erreur: {ex.Message}" });
        //     }
        // }
[HttpPost]
        public async Task<IActionResult> UpdateTaskStatus(int taskId, string newStatus)
        {
            try
            {
                var task = await _context.Tasks.FindAsync(taskId);
                if (task == null)
                    return Json(new { success = false, message = "Tâche non trouvée" });

                var oldStatus = task.Status;
                task.Status = newStatus;
                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = $"Statut changé de '{oldStatus}' à '{newStatus}'",
                    taskId = taskId,
                    oldStatus = oldStatus,
                    newStatus = newStatus
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Erreur: {ex.Message}" });
            }
        }
        [HttpPost]
        public async Task<IActionResult> AssignTask(int id, int? userId)
        {
            var task = await _context.Tasks.FindAsync(id);
            if (task != null)
            {
                task.AssignedToUserId = userId?.ToString();
                await _context.SaveChangesAsync();

                var assignedUser = userId.HasValue
                    ? await _context.Users.FindAsync(userId.Value)
                    : null;

                return Json(new
                {
                    success = true,
                    message = assignedUser != null
                        ? $"Tâche assignée à {assignedUser.Name}"
                        : "Assignation supprimée",
                    assignedUserName = assignedUser?.Name ?? "Non assigné"
                });
            }
            return Json(new { success = false, message = "Tâche non trouvée" });
        }

        // =================== MÉTHODES UTILITAIRES ===================
        private async Task LoadUserSelectLists()
        {
            var activeUsers = await _context.Users
                .Where(u => u.IsActive)
                .OrderBy(u => u.LastName)
                .ToListAsync();

            ViewBag.AssignedToUsers = new SelectList(activeUsers, "Id", "Name");
            ViewBag.CreatedByUsers = new SelectList(activeUsers, "Id", "Name");
            ViewBag.AllUsers = activeUsers;
        }

        [HttpGet]
        public async Task<IActionResult> GetTasksJson()
        {
            var tasks = await _context.Tasks
                .Include(t => t.AssignedToUser)
                .Include(t => t.CreatedByUser)
                .Select(t => new
                {
                    id = t.Id,
                    title = t.Title,
                    description = t.Description,
                    dueDate = t.DueDate.ToString("yyyy-MM-dd"),
                    priority = t.Priority,
                    status = t.Status,
                    assignedTo = t.AssignedToUser != null ? t.AssignedToUser.Name : "Non assigné",
                    assignedToId = t.AssignedToUserId,
                    createdBy = t.CreatedByUser != null ? t.CreatedByUser.Name : "Inconnu",
                    createdDate = t.CreatedDate.ToString("yyyy-MM-dd")
                })
                .ToListAsync();

            return Json(tasks);
        }

        [HttpGet]
        public async Task<IActionResult> GetTaskStats()
        {
            var today = DateTime.Today;
            var tasks = await _context.Tasks.ToListAsync();

            var stats = new
            {
                total = tasks.Count,
                completed = tasks.Count(t => t.Status == "Completed"),
                inProgress = tasks.Count(t => t.Status == "In Progress"),
                toDo = tasks.Count(t => t.Status == "To Do"),
                dueToday = tasks.Count(t => t.DueDate.Date == today),
                overdue = tasks.Count(t => t.DueDate.Date < today && t.Status != "Completed"),
                thisWeek = tasks.Count(t => t.DueDate.Date >= today && t.DueDate.Date <= today.AddDays(7)),
                high = tasks.Count(t => t.Priority == "High"),
                medium = tasks.Count(t => t.Priority == "Medium"),
                low = tasks.Count(t => t.Priority == "Low")
            };

            return Json(stats);
        }

        // =================== GÉNÉRATION HTML POUR PDF ===================
        private string GenerateTasksHtml(List<TaskModel> tasks)
        {
            var html = new StringBuilder();
            html.AppendLine("<!DOCTYPE html>");
            html.AppendLine("<html><head>");
            html.AppendLine("<meta charset='utf-8'>");
            html.AppendLine("<title>Rapport des Tâches</title>");
            html.AppendLine("<style>");
            html.AppendLine("body { font-family: Arial, sans-serif; margin: 20px; }");
            html.AppendLine("h1 { color: #667eea; text-align: center; }");
            html.AppendLine("table { width: 100%; border-collapse: collapse; margin-top: 20px; }");
            html.AppendLine("th, td { border: 1px solid #ddd; padding: 8px; text-align: left; }");
            html.AppendLine("th { background-color: #667eea; color: white; }");
            html.AppendLine(".priority-high { background-color: #ffebee; }");
            html.AppendLine(".priority-medium { background-color: #fff3e0; }");
            html.AppendLine(".priority-low { background-color: #e8f5e8; }");
            html.AppendLine(".status-completed { background-color: #e8f5e8; }");
            html.AppendLine("</style>");
            html.AppendLine("</head><body>");

            html.AppendLine($"<h1>📋 Rapport des Tâches</h1>");
            html.AppendLine($"<p><strong>Date de génération:</strong> {DateTime.Now:dd/MM/yyyy HH:mm}</p>");
            html.AppendLine($"<p><strong>Nombre total de tâches:</strong> {tasks.Count}</p>");

            html.AppendLine("<table>");
            html.AppendLine("<thead>");
            html.AppendLine("<tr>");
            html.AppendLine("<th>ID</th>");
            html.AppendLine("<th>Titre</th>");
            html.AppendLine("<th>Description</th>");
            html.AppendLine("<th>Priorité</th>");
            html.AppendLine("<th>Statut</th>");
            html.AppendLine("<th>Assigné à</th>");
            html.AppendLine("<th>Date d'échéance</th>");
            html.AppendLine("<th>Date de création</th>");
            html.AppendLine("</tr>");
            html.AppendLine("</thead>");
            html.AppendLine("<tbody>");

            foreach (var task in tasks)
            {
                var priorityClass = $"priority-{task.Priority.ToLower()}";
                var statusClass = task.Status == "Completed" ? "status-completed" : "";
                var rowClass = $"{priorityClass} {statusClass}".Trim();

                html.AppendLine($"<tr class='{rowClass}'>");
                html.AppendLine($"<td>{task.Id}</td>");
                html.AppendLine($"<td>{task.Title}</td>");
                html.AppendLine($"<td>{task.Description ?? "Aucune description"}</td>");
                html.AppendLine($"<td>{task.Priority}</td>");
                html.AppendLine($"<td>{task.Status}</td>");
                html.AppendLine($"<td>{task.AssignedToUser?.Name ?? "Non assigné"}</td>");
                html.AppendLine($"<td>{task.DueDate:dd/MM/yyyy}</td>");
                html.AppendLine($"<td>{task.CreatedDate:dd/MM/yyyy}</td>");
                html.AppendLine("</tr>");
            }

            html.AppendLine("</tbody>");
            html.AppendLine("</table>");
            html.AppendLine("</body></html>");

            return html.ToString();
        }

        // =================== GÉNÉRATION PDF (VERSION SIMPLE) ===================
        private byte[] GeneratePdfFromHtml(string html)
        {
            // Version simple : retourner le HTML encodé
            // Pour un vrai PDF, utilisez une bibliothèque comme IronPDF, SelectPdf, ou PuppeteerSharp
            return Encoding.UTF8.GetBytes(html);
        }

        // =================== GÉNÉRATION CSV ===================
        private string GenerateTasksCsv(List<TaskModel> tasks)
        {
            var csv = new StringBuilder();

            // En-têtes
            csv.AppendLine("ID,Titre,Description,Priorité,Statut,Assigné à,Date d'échéance,Date de création");

            // Données
            foreach (var task in tasks)
            {
                var line = $"{task.Id}," +
                          $"\"{task.Title}\"," +
                          $"\"{task.Description?.Replace("\"", "\"\"") ?? "Aucune description"}\"," +
                          $"{task.Priority}," +
                          $"{task.Status}," +
                          $"\"{task.AssignedToUser?.Name ?? "Non assigné"}\"," +
                          $"{task.DueDate:dd/MM/yyyy}," +
                          $"{task.CreatedDate:dd/MM/yyyy}";

                csv.AppendLine(line);
            }

            return csv.ToString();
        }
    }
    }
