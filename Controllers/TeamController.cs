using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskManager.Data;
using TaskManager.Models;
using System.Text.Json;

namespace TaskManager.Controllers
{
    public class TeamController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TeamController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Page de liste des utilisateurs
        public async Task<IActionResult> Index()
        {
            var users = await _context.Users
                .Where(u => u.IsActive)
                .Include(u => u.AssignedTasks)
                .OrderBy(u => u.Name)
                .ToListAsync();

            // Statistiques pour chaque utilisateur
            foreach (var user in users)
            {
                if (user.AssignedTasks != null)
                {
                    ViewData[$"TaskCount_{user.Id}"] = user.AssignedTasks.Count;
                    ViewData[$"CompletedTasks_{user.Id}"] = user.AssignedTasks.Count(t => t.Status == "Completed");
                    ViewData[$"PendingTasks_{user.Id}"] = user.AssignedTasks.Count(t => t.Status != "Completed");
                }
                else
                {
                    ViewData[$"TaskCount_{user.Id}"] = 0;
                    ViewData[$"CompletedTasks_{user.Id}"] = 0;
                    ViewData[$"PendingTasks_{user.Id}"] = 0;
                }
            }

            var currentUser = users.FirstOrDefault();
            ViewBag.CurrentUser = currentUser?.Name ?? "Utilisateur";
            ViewBag.CurrentUserId = currentUser?.Id ?? 1;

            return View(users);
        }

        // ✨ NOUVELLE ACTION POUR LE CHAT
        public async Task<IActionResult> Chat()
        {
            try
            {
                // Récupérer tous les utilisateurs actifs pour le chat
                var users = await _context.Users
                    .Where(u => u.IsActive)
                    .OrderBy(u => u.Name)
                    .ToListAsync();

                // Utilisateur actuel (premier utilisateur pour l'instant)
                var currentUser = users.FirstOrDefault();
                ViewBag.CurrentUser = currentUser?.Name ?? "Utilisateur Anonyme";
                ViewBag.CurrentUserId = $"user-{currentUser?.Id ?? 1}";

                // ✨ CONVERTIR LES UTILISATEURS POUR LE JAVASCRIPT (AVEC GESTION D'ERREUR)
                var chatUsers = users.Select(u => new
                {
                    id = $"user-{u.Id}",
                    name = u.Name ?? "Utilisateur",
                    avatar = !string.IsNullOrEmpty(u.AvatarUrl) ? u.AvatarUrl :
                             $"https://ui-avatars.com/api/?name={Uri.EscapeDataString(u.Name ?? "User")}&size=100&background=667eea&color=fff",
                    status = "offline", // Par défaut offline, sera mis à jour par SignalR
                    position = u.Position ?? "Membre de l'équipe",
                    department = u.Department ?? "Général",
                    email = u.Email ?? "",
                    connectionId = (string?)null
                }).ToList();

                // ✨ SÉRIALISATION JSON SÉCURISÉE
                var jsonOptions = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = false,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };

                var jsonString = JsonSerializer.Serialize(chatUsers, jsonOptions);

                // Debug
                Console.WriteLine($"JSON généré: {jsonString}");
                Console.WriteLine($"Nombre d'utilisateurs: {users.Count}");

                // Passer les données à la vue via ViewBag
                ViewBag.TeamMembersJson = jsonString;
                ViewBag.TeamMembers = chatUsers;
                ViewBag.UsersCount = users.Count;

                return View();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur dans Chat(): {ex.Message}");

                // En cas d'erreur, passer des données par défaut
                ViewBag.CurrentUser = "Utilisateur Test";
                ViewBag.CurrentUserId = "user-1";
                ViewBag.TeamMembersJson = "[]";
                ViewBag.UsersCount = 0;

                return View();
            }
        }

        // Actions existantes...
        public async Task<IActionResult> Profile(int id)
        {
            var user = await _context.Users
                .Include(u => u.AssignedTasks)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(User user)
        {
            if (ModelState.IsValid)
            {
                user.CreatedDate = DateTime.Now;
                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                TempData["Success"] = $"L'utilisateur {user.Name} a été ajouté avec succès !";
                return RedirectToAction(nameof(Index));
            }

            return View(user);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, User user)
        {
            if (id != user.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(user);
                    await _context.SaveChangesAsync();

                    TempData["Success"] = $"L'utilisateur {user.Name} a été modifié avec succès !";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!UserExists(user.Id))
                    {
                        return NotFound();
                    }
                    throw;
                }
            }

            return View(user);
        }

        public async Task<IActionResult> Deactivate(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                user.IsActive = false;
                await _context.SaveChangesAsync();

                TempData["Info"] = $"L'utilisateur {user.Name} a été désactivé.";
            }

            return RedirectToAction(nameof(Index));
        }

        // ✨ API POUR RÉCUPÉRER LES UTILISATEURS (pour AJAX)
        [HttpGet]
        public async Task<IActionResult> GetActiveUsers()
        {
            var users = await _context.Users
                .Where(u => u.IsActive)
                .Select(u => new
                {
                    id = u.Id,
                    name = u.Name,
                    email = u.Email,
                    avatar = !string.IsNullOrEmpty(u.AvatarUrl) ? u.AvatarUrl :
                             $"https://ui-avatars.com/api/?name={Uri.EscapeDataString(u.Name)}&size=100&background=667eea&color=fff",
                    position = u.Position,
                    department = u.Department
                })
                .ToListAsync();

            return Json(users);
        }

        private bool UserExists(int id)
        {
            return _context.Users.Any(e => e.Id == id);
        }
    }
}