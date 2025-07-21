using Microsoft.AspNetCore.Mvc;
using TaskManager.Data;
using TaskManager.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Text;

namespace TaskManager.Controllers
{
    public class TasksController(ApplicationDbContext context) : Controller
    {
        private readonly ApplicationDbContext _context = context;

        // ✨ Cache JsonSerializerOptions pour éviter les créations répétées
        //private static readonly JsonSerializerOptions JsonOptions = new()
        //{
        //    PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
        //    WriteIndented = false
        //};

        // =================== AFFICHER LA LISTE AVEC FILTRES ET TRIAGE ===================
        public async Task<IActionResult> Index(string sortBy = "Id", string sortOrder = "desc",
            string filterStatus = "", string filterPriority = "", string filterUser = "", string search = "")
        {
            var query = _context.Tasks
                .Include(t => t.AssignedToUser)
                .Include(t => t.CreatedByUser)
                .AsQueryable();

            // ✨ FILTRAGE
            if (!string.IsNullOrEmpty(filterStatus))
                query = query.Where(t => t.Status == filterStatus);

            if (!string.IsNullOrEmpty(filterPriority))
                query = query.Where(t => t.Priority == filterPriority);

            if (!string.IsNullOrEmpty(filterUser))
                query = query.Where(t => t.AssignedToUserId.ToString() == filterUser);

            if (!string.IsNullOrEmpty(search))
                query = query.Where(t => t.Title.Contains(search) ||
                                        (t.Description != null && t.Description.Contains(search)));

            // ✨ TRIAGE
            query = sortBy.ToLower() switch
            {
                "title" => sortOrder == "asc" ? query.OrderBy(t => t.Title) : query.OrderByDescending(t => t.Title),
                "duedate" => sortOrder == "asc" ? query.OrderBy(t => t.DueDate) : query.OrderByDescending(t => t.DueDate),
                "priority" => sortOrder == "asc" ? query.OrderBy(t => t.Priority) : query.OrderByDescending(t => t.Priority),
                "status" => sortOrder == "asc" ? query.OrderBy(t => t.Status) : query.OrderByDescending(t => t.Status),
                "assignedto" => sortOrder == "asc" ? query.OrderBy(t => t.AssignedToUser!.Name) : query.OrderByDescending(t => t.AssignedToUser!.Name),
                "createddate" => sortOrder == "asc" ? query.OrderBy(t => t.CreatedDate) : query.OrderByDescending(t => t.CreatedDate),
                _ => sortOrder == "asc" ? query.OrderBy(t => t.Id) : query.OrderByDescending(t => t.Id)
            };

            var tasks = await query.ToListAsync();

            // ✨ DONNÉES POUR LA VUE
            var users = await _context.Users.Where(u => u.IsActive).ToListAsync();

            ViewBag.Users = new SelectList(users, "Id", "Name");
            ViewBag.SortBy = sortBy;
            ViewBag.SortOrder = sortOrder;
            ViewBag.FilterStatus = filterStatus;
            ViewBag.FilterPriority = filterPriority;
            ViewBag.FilterUser = filterUser;
            ViewBag.Search = search;

            // ✨ STATISTIQUES
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

        // =================== DRAG & DROP - MISE À JOUR DU STATUT ===================
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

        // =================== EXPORT PDF ===================
        [HttpGet]
        public async Task<IActionResult> ExportPdf(string filterStatus = "", string filterPriority = "", string filterUser = "")
        {
            var query = _context.Tasks
                .Include(t => t.AssignedToUser)
                .Include(t => t.CreatedByUser)
                .AsQueryable();

            // Appliquer les mêmes filtres
            if (!string.IsNullOrEmpty(filterStatus))
                query = query.Where(t => t.Status == filterStatus);
            if (!string.IsNullOrEmpty(filterPriority))
                query = query.Where(t => t.Priority == filterPriority);
            if (!string.IsNullOrEmpty(filterUser))
                query = query.Where(t => t.AssignedToUserId.ToString() == filterUser);

            var tasks = await query.OrderByDescending(t => t.Id).ToListAsync();

            // ✨ GÉNÉRATION HTML POUR PDF
            var html = GenerateTasksHtml(tasks);
            var pdfBytes = GeneratePdfFromHtml(html);

            return File(pdfBytes, "application/pdf", $"Taches_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
        }

        // =================== EXPORT EXCEL ===================
        [HttpGet]
        public async Task<IActionResult> ExportExcel(string filterStatus = "", string filterPriority = "", string filterUser = "")
        {
            var query = _context.Tasks
                .Include(t => t.AssignedToUser)
                .Include(t => t.CreatedByUser)
                .AsQueryable();

            // Appliquer les mêmes filtres
            if (!string.IsNullOrEmpty(filterStatus))
                query = query.Where(t => t.Status == filterStatus);
            if (!string.IsNullOrEmpty(filterPriority))
                query = query.Where(t => t.Priority == filterPriority);
            if (!string.IsNullOrEmpty(filterUser))
                query = query.Where(t => t.AssignedToUserId.ToString() == filterUser);

            var tasks = await query.OrderByDescending(t => t.Id).ToListAsync();

            // ✨ GÉNÉRATION CSV (COMPATIBLE EXCEL)
            var csv = GenerateTasksCsv(tasks);
            var bytes = Encoding.UTF8.GetBytes(csv);

            return File(bytes, "text/csv", $"Taches_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
        }

        // =================== ACTIONS CRUD EXISTANTES ===================
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await LoadUserSelectLists();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(TaskModel task)
        {
            if (ModelState.IsValid)
            {
                task.CreatedDate = DateTime.Now;

                if (!task.CreatedByUserId.HasValue)
                {
                    var firstUser = await _context.Users.FirstOrDefaultAsync(u => u.IsActive);
                    task.CreatedByUserId = firstUser?.Id;
                }

                _context.Tasks.Add(task);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Tâche créée avec succès !";
                return RedirectToAction("Index");
            }

            await LoadUserSelectLists();
            return View(task);
        }

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

            await LoadUserSelectLists();
            return View(task);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(TaskModel task)
        {
            if (ModelState.IsValid)
            {
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

            await LoadUserSelectLists();
            return View(task);
        }

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

        // =================== ACTIONS AJAX EXISTANTES ===================
        [HttpPost]
        public async Task<IActionResult> UpdateStatus(int id, string status)
        {
            var task = await _context.Tasks.FindAsync(id);
            if (task != null)
            {
                task.Status = status;
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            return Json(new { success = false });
        }

        [HttpPost]
        public async Task<IActionResult> UpdatePriority(int id, string priority)
        {
            var task = await _context.Tasks.FindAsync(id);
            if (task != null)
            {
                task.Priority = priority;
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            return Json(new { success = false });
        }

        [HttpPost]
        public async Task<IActionResult> AssignTask(int id, int? userId)
        {
            var task = await _context.Tasks.FindAsync(id);
            if (task != null)
            {
                task.AssignedToUserId = userId;
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
                .OrderBy(u => u.Name)
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
                .Select(t => new {
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