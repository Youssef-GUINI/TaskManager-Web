using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using TaskManager.data;
using TaskManager.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using ClosedXML.Excel;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using System.IO;

namespace TaskManager.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class TasksController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly ILogger<TasksController> _logger;

        public TasksController(
            ApplicationDbContext context,
            UserManager<User> userManager,
            ILogger<TasksController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        // GET: Admin/Tasks
        public async Task<IActionResult> Index(
            string sortBy = "duedate",
            string sortOrder = "asc",
            string filterStatus = "",
            string filterPriority = "",
            string filterUser = "",
            string search = "")
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                _logger.LogWarning("Utilisateur non authentifié");
                return RedirectToAction("Login", "Account", new { area = "" });
            }

            var query = _context.Tasks
                .Include(t => t.AssignedToUser)
                .Include(t => t.CreatedByUser)
                .Where(t => t.AssignedToUser.ManagerId == currentUser.Id ||
                            t.CreatedByUserId == currentUser.Id);

            if (!string.IsNullOrEmpty(filterStatus))
                query = query.Where(t => t.Status == filterStatus);

            if (!string.IsNullOrEmpty(filterPriority))
                query = query.Where(t => t.Priority == filterPriority);

            if (!string.IsNullOrEmpty(filterUser))
                query = query.Where(t => t.AssignedToUserId == filterUser);

            if (!string.IsNullOrEmpty(search))
                query = query.Where(t => t.Title.Contains(search) || t.Description.Contains(search));

            query = sortBy.ToLower() switch
            {
                "title" => sortOrder == "asc" ? query.OrderBy(t => t.Title) : query.OrderByDescending(t => t.Title),
                "priority" => sortOrder == "asc"
                    ? query.OrderBy(t => t.Priority == "High" ? 1 : t.Priority == "Medium" ? 2 : 3)
                    : query.OrderByDescending(t => t.Priority == "High" ? 1 : t.Priority == "Medium" ? 2 : 3),
                _ => sortOrder == "asc" ? query.OrderBy(t => t.DueDate) : query.OrderByDescending(t => t.DueDate)
            };

            var managedUsers = await _userManager.Users
                .Where(u => u.ManagerId == currentUser.Id)
                .ToListAsync();

            ViewBag.Users = new SelectList(managedUsers, "Id", "UserName");
            ViewBag.Stats = new
            {
                Total = await query.CountAsync(),
                Todo = await query.CountAsync(t => t.Status == "To Do"),
                InProgress = await query.CountAsync(t => t.Status == "In Progress"),
                Completed = await query.CountAsync(t => t.Status == "Completed")
            };

            ViewBag.FilterStatus = filterStatus;
            ViewBag.FilterPriority = filterPriority;
            ViewBag.FilterUser = filterUser;
            ViewBag.Search = search;
            ViewBag.SortBy = sortBy;
            ViewBag.SortOrder = sortOrder;

            return View("~/Areas/Admin/Views/Tasks/Index.cshtml", await query.ToListAsync());
        }

        // GET: Admin/Tasks/Create
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                _logger.LogWarning("Utilisateur non authentifié");
                return RedirectToAction("Login", "Account", new { area = "" });
            }

            await LoadAssignees(currentUser.Id);

            var model = new TaskModel
            {
                DueDate = DateTime.Now.AddDays(7),
                Priority = "Medium",
                Status = "To Do"
            };

            return View("~/Areas/Admin/Views/Tasks/Create.cshtml", model);
        }

        // POST: Admin/Tasks/Create
        [HttpPost]
        public async Task<IActionResult> Create(TaskModel taskModel)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                _logger.LogWarning("Utilisateur non authentifié");
                return RedirectToAction("Login", "Account", new { area = "" });
            }

            taskModel.CreatedByUserId = currentUser.Id;
            taskModel.CreatedDate = DateTime.Now;

            if (string.IsNullOrWhiteSpace(taskModel.Title))
            {
                ModelState.AddModelError("Title", "Le titre est obligatoire.");
            }

            if (!ModelState.IsValid)
            {
                await LoadAssignees(currentUser.Id);
                return View("~/Areas/Admin/Views/Tasks/Create.cshtml", taskModel);
            }

            _context.Add(taskModel);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Tâche créée avec succès !";
            return RedirectToAction(nameof(Index));
        }

        private async Task LoadAssignees(string managerId)
        {
            var managedUsers = await _userManager.Users
                .Where(u => u.ManagerId == managerId)
                .ToListAsync();

            ViewBag.AssignedToUsers = new SelectList(managedUsers, "Id", "UserName");
        }

        // GET: Admin/Tasks/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var taskModel = await _context.Tasks.FindAsync(id);
            if (taskModel == null) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return RedirectToAction("Login", "Account", new { area = "" });

            var isAuthorized = taskModel.CreatedByUserId == currentUser.Id ||
                               (taskModel.AssignedToUser != null && taskModel.AssignedToUser.ManagerId == currentUser.Id);

            if (!isAuthorized) return Forbid();

            await LoadAssignees(currentUser.Id);

            return View("~/Areas/Admin/Views/Tasks/Edit.cshtml", taskModel);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, TaskModel taskModel)
        {
            if (id != taskModel.Id)
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account", new { area = "" });
            }

            var originalTask = await _context.Tasks
                .Include(t => t.AssignedToUser)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (originalTask == null)
            {
                return NotFound();
            }

            var isAuthorized = originalTask.CreatedByUserId == currentUser.Id ||
                              (originalTask.AssignedToUser != null && originalTask.AssignedToUser.ManagerId == currentUser.Id);

            if (!isAuthorized)
            {
                return Forbid();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    originalTask.Title = taskModel.Title;
                    originalTask.Description = taskModel.Description;
                    originalTask.DueDate = taskModel.DueDate;
                    originalTask.Priority = taskModel.Priority;
                    originalTask.Status = taskModel.Status;
                    originalTask.AssignedToUserId = taskModel.AssignedToUserId;
                    originalTask.LastUpdated = DateTime.Now;

                    if (taskModel.Status == "Completed" && originalTask.CompletedDate == null)
                    {
                        originalTask.CompletedDate = DateTime.Now;
                    }
                    else if (taskModel.Status != "Completed")
                    {
                        originalTask.CompletedDate = null;
                    }

                    _context.Update(originalTask);
                    await _context.SaveChangesAsync();

                    TempData["Success"] = "Tâche mise à jour avec succès!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Tasks.Any(e => e.Id == taskModel.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }

                return RedirectToAction(nameof(Index));
            }

            await LoadAssignees(currentUser.Id);
            return View("~/Areas/Admin/Views/Tasks/Edit.cshtml", taskModel);
        }

        // GET: Admin/Tasks/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var taskModel = await _context.Tasks
                .Include(t => t.AssignedToUser)
                .Include(t => t.CreatedByUser)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (taskModel == null) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return RedirectToAction("Login", "Account", new { area = "" });

            var isAuthorized = taskModel.CreatedByUserId == currentUser.Id ||
                               (taskModel.AssignedToUser != null && taskModel.AssignedToUser.ManagerId == currentUser.Id);

            if (!isAuthorized) return Forbid();

            return View("~/Areas/Admin/Views/Tasks/Delete.cshtml", taskModel);
        }

        // POST: Admin/Tasks/Delete/5
        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var taskModel = await _context.Tasks.FindAsync(id);
            if (taskModel == null) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return RedirectToAction("Login", "Account", new { area = "" });

            var isAuthorized = taskModel.CreatedByUserId == currentUser.Id ||
                               (taskModel.AssignedToUser != null && taskModel.AssignedToUser.ManagerId == currentUser.Id);

            if (!isAuthorized) return Forbid();

            _context.Tasks.Remove(taskModel);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Tâche supprimée avec succès!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> UpdateStatus([FromBody] UpdateStatusDto model)
        {
            try
            {
                var task = await _context.Tasks
                    .Include(t => t.AssignedToUser)
                    .FirstOrDefaultAsync(t => t.Id == model.Id);

                if (task == null)
                    return NotFound();

                var currentUser = await _userManager.GetUserAsync(User);
                var isAuthorized = task.CreatedByUserId == currentUser.Id ||
                                  (task.AssignedToUser != null && task.AssignedToUser.ManagerId == currentUser.Id);

                if (!isAuthorized)
                    return Forbid();

                task.Status = model.Status;
                task.LastUpdated = DateTime.Now;

                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Statut mis à jour avec succès!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la mise à jour du statut de la tâche.");
                return StatusCode(500, new { success = false, message = "Erreur interne" });
            }
        }

        // EXPORT PDF avec iText7
        public async Task<IActionResult> ExportPdf(
            string filterStatus = "",
            string filterPriority = "",
            string filterUser = "",
            string search = "")
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    return RedirectToAction("Login", "Account", new { area = "" });
                }

                // Récupérer les tâches avec les mêmes filtres que l'Index
                var query = _context.Tasks
                    .Include(t => t.AssignedToUser)
                    .Include(t => t.CreatedByUser)
                    .Where(t => t.AssignedToUser.ManagerId == currentUser.Id ||
                                t.CreatedByUserId == currentUser.Id);

                // Appliquer les filtres
                if (!string.IsNullOrEmpty(filterStatus))
                    query = query.Where(t => t.Status == filterStatus);

                if (!string.IsNullOrEmpty(filterPriority))
                    query = query.Where(t => t.Priority == filterPriority);

                if (!string.IsNullOrEmpty(filterUser))
                    query = query.Where(t => t.AssignedToUserId == filterUser);

                if (!string.IsNullOrEmpty(search))
                    query = query.Where(t => t.Title.Contains(search) || t.Description.Contains(search));

                var tasks = await query.OrderBy(t => t.DueDate).ToListAsync();

                using (var stream = new MemoryStream())
                {
                    var writer = new PdfWriter(stream);
                    var pdf = new PdfDocument(writer);
                    var document = new Document(pdf);

                    // Titre du document
                    document.Add(new Paragraph("📋 Rapport des Tâches")
                        .SetFontSize(20)
                        .SetBold()
                        .SetTextAlignment(TextAlignment.CENTER));

                    document.Add(new Paragraph($"Généré le: {DateTime.Now:dd/MM/yyyy à HH:mm}")
                        .SetFontSize(10)
                        .SetTextAlignment(TextAlignment.RIGHT));

                    document.Add(new Paragraph("\n"));

                    // Statistiques
                    document.Add(new Paragraph("📊 Statistiques:")
                        .SetFontSize(14)
                        .SetBold());

                    document.Add(new Paragraph($"• Total des tâches: {tasks.Count}")
                        .SetFontSize(12));
                    document.Add(new Paragraph($"• À faire: {tasks.Count(t => t.Status == "To Do")}")
                        .SetFontSize(12));
                    document.Add(new Paragraph($"• En cours: {tasks.Count(t => t.Status == "In Progress")}")
                        .SetFontSize(12));
                    document.Add(new Paragraph($"• Terminées: {tasks.Count(t => t.Status == "Completed")}")
                        .SetFontSize(12));

                    document.Add(new Paragraph("\n"));

                    // Créer le tableau
                    var table = new Table(6);
                    table.SetWidth(UnitValue.CreatePercentValue(100));

                    // En-têtes
                    table.AddHeaderCell(new Cell().Add(new Paragraph("ID").SetBold()));
                    table.AddHeaderCell(new Cell().Add(new Paragraph("Titre").SetBold()));
                    table.AddHeaderCell(new Cell().Add(new Paragraph("Priorité").SetBold()));
                    table.AddHeaderCell(new Cell().Add(new Paragraph("Statut").SetBold()));
                    table.AddHeaderCell(new Cell().Add(new Paragraph("Assigné à").SetBold()));
                    table.AddHeaderCell(new Cell().Add(new Paragraph("Échéance").SetBold()));

                    // Données
                    foreach (var task in tasks)
                    {
                        table.AddCell(new Cell().Add(new Paragraph(task.Id.ToString())));
                        table.AddCell(new Cell().Add(new Paragraph(task.Title ?? "")));
                        table.AddCell(new Cell().Add(new Paragraph(task.Priority ?? "")));
                        table.AddCell(new Cell().Add(new Paragraph(task.Status ?? "")));
                        table.AddCell(new Cell().Add(new Paragraph(task.AssignedToUser?.UserName ?? "Non assigné")));
                        table.AddCell(new Cell().Add(new Paragraph(task.DueDate.ToString("dd/MM/yyyy"))));
                    }

                    document.Add(table);
                    document.Close();

                    var fileName = $"Taches_Export_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
                    return File(stream.ToArray(), "application/pdf", fileName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'export PDF");
                TempData["Error"] = "Erreur lors de la génération du PDF: " + ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        // EXPORT EXCEL avec ClosedXML
        public async Task<IActionResult> ExportExcel(
            string filterStatus = "",
            string filterPriority = "",
            string filterUser = "",
            string search = "")
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    return RedirectToAction("Login", "Account", new { area = "" });
                }

                // Récupérer les tâches avec les mêmes filtres que l'Index
                var query = _context.Tasks
                    .Include(t => t.AssignedToUser)
                    .Include(t => t.CreatedByUser)
                    .Where(t => t.AssignedToUser.ManagerId == currentUser.Id ||
                                t.CreatedByUserId == currentUser.Id);

                // Appliquer les filtres
                if (!string.IsNullOrEmpty(filterStatus))
                    query = query.Where(t => t.Status == filterStatus);

                if (!string.IsNullOrEmpty(filterPriority))
                    query = query.Where(t => t.Priority == filterPriority);

                if (!string.IsNullOrEmpty(filterUser))
                    query = query.Where(t => t.AssignedToUserId == filterUser);

                if (!string.IsNullOrEmpty(search))
                    query = query.Where(t => t.Title.Contains(search) || t.Description.Contains(search));

                var tasks = await query.OrderBy(t => t.DueDate).ToListAsync();

                using (var workbook = new XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add("Tâches");

                    // Titre principal
                    worksheet.Cell(1, 1).Value = "📋 Rapport des Tâches";
                    worksheet.Cell(1, 1).Style.Font.FontSize = 16;
                    worksheet.Cell(1, 1).Style.Font.Bold = true;
                    worksheet.Range(1, 1, 1, 8).Merge();
                    worksheet.Cell(1, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    worksheet.Cell(2, 1).Value = $"Généré le: {DateTime.Now:dd/MM/yyyy à HH:mm}";
                    worksheet.Range(2, 1, 2, 8).Merge();
                    worksheet.Cell(2, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

                    // Statistiques
                    int statsRow = 4;
                    worksheet.Cell(statsRow, 1).Value = "📊 Statistiques:";
                    worksheet.Cell(statsRow, 1).Style.Font.Bold = true;

                    worksheet.Cell(statsRow + 1, 1).Value = $"Total des tâches: {tasks.Count}";
                    worksheet.Cell(statsRow + 2, 1).Value = $"À faire: {tasks.Count(t => t.Status == "To Do")}";
                    worksheet.Cell(statsRow + 3, 1).Value = $"En cours: {tasks.Count(t => t.Status == "In Progress")}";
                    worksheet.Cell(statsRow + 4, 1).Value = $"Terminées: {tasks.Count(t => t.Status == "Completed")}";

                    // En-têtes du tableau
                    int headerRow = statsRow + 6;
                    worksheet.Cell(headerRow, 1).Value = "ID";
                    worksheet.Cell(headerRow, 2).Value = "Titre";
                    worksheet.Cell(headerRow, 3).Value = "Description";
                    worksheet.Cell(headerRow, 4).Value = "Priorité";
                    worksheet.Cell(headerRow, 5).Value = "Statut";
                    worksheet.Cell(headerRow, 6).Value = "Assigné à";
                    worksheet.Cell(headerRow, 7).Value = "Échéance";
                    worksheet.Cell(headerRow, 8).Value = "Créé le";

                    // Style des en-têtes
                    var headerRange = worksheet.Range(headerRow, 1, headerRow, 8);
                    headerRange.Style.Font.Bold = true;
                    headerRange.Style.Fill.BackgroundColor = XLColor.LightBlue;
                    headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thick;
                    headerRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

                    // Données
                    int currentRow = headerRow + 1;
                    foreach (var task in tasks)
                    {
                        worksheet.Cell(currentRow, 1).Value = task.Id;
                        worksheet.Cell(currentRow, 2).Value = task.Title ?? "";
                        worksheet.Cell(currentRow, 3).Value = task.Description ?? "";
                        worksheet.Cell(currentRow, 4).Value = task.Priority ?? "";
                        worksheet.Cell(currentRow, 5).Value = task.Status ?? "";
                        worksheet.Cell(currentRow, 6).Value = task.AssignedToUser?.UserName ?? "Non assigné";
                        worksheet.Cell(currentRow, 7).Value = task.DueDate.ToString("dd/MM/yyyy");
                        worksheet.Cell(currentRow, 8).Value = task.CreatedDate.ToString("dd/MM/yyyy");
                        // Coloration selon la priorité
                        if (task.Priority == "High")
                        {
                            worksheet.Cell(currentRow, 4).Style.Fill.BackgroundColor = XLColor.LightCoral;
                        }
                        else if (task.Priority == "Medium")
                        {
                            worksheet.Cell(currentRow, 4).Style.Fill.BackgroundColor = XLColor.LightYellow;
                        }
                        else if (task.Priority == "Low")
                        {
                            worksheet.Cell(currentRow, 4).Style.Fill.BackgroundColor = XLColor.LightGreen;
                        }

                        // Coloration selon le statut
                        if (task.Status == "Completed")
                        {
                            worksheet.Cell(currentRow, 5).Style.Fill.BackgroundColor = XLColor.LightGreen;
                        }
                        else if (task.Status == "In Progress")
                        {
                            worksheet.Cell(currentRow, 5).Style.Fill.BackgroundColor = XLColor.LightYellow;
                        }
                        else if (task.Status == "To Do")
                        {
                            worksheet.Cell(currentRow, 5).Style.Fill.BackgroundColor = XLColor.LightGray;
                        }

                        // Marquer en rouge si en retard
                        if (task.DueDate < DateTime.Today && task.Status != "Completed")
                        {
                            worksheet.Cell(currentRow, 7).Style.Fill.BackgroundColor = XLColor.LightCoral;
                        }

                        currentRow++;
                    }

                    // Bordures pour toutes les données
                    if (tasks.Any())
                    {
                        var dataRange = worksheet.Range(headerRow, 1, currentRow - 1, 8);
                        dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thick;
                        dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                    }

                    // Ajuster la largeur des colonnes
                    worksheet.Columns().AdjustToContents();

                    using (var stream = new MemoryStream())
                    {
                        workbook.SaveAs(stream);
                        var fileName = $"Taches_Export_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                        return File(stream.ToArray(),
                            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                            fileName);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'export Excel");
                TempData["Error"] = "Erreur lors de la génération du fichier Excel: " + ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        public class UpdateStatusDto
        {
            public int Id { get; set; }
            public string Status { get; set; }
        }
    }
}