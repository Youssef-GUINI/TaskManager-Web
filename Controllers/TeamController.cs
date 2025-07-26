using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskManager.data;
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
        // public async Task<IActionResult> Index()
        // {
        //     var users = await _context.Users
        //         .Where(u => u.IsActive)
        //         .Include(u => u.AssignedTasks)
        //         .OrderBy(u => u.LastName)
        //         .ToListAsync();
        //     Console.WriteLine($"Nombre d'utilisateurs : {users.Count}");
        //     // Statistiques pour chaque utilisateur
        //     foreach (var user in users)
        //     {
        //         if (user.AssignedTasks != null)
        //         {
        //             ViewData[$"TaskCount_{user.Idclass}"] = user.AssignedTasks.Count;
        //             ViewData[$"CompletedTasks_{user.Idclass}"] = user.AssignedTasks.Count(t => t.Status == "Completed");
        //             ViewData[$"PendingTasks_{user.Idclass}"] = user.AssignedTasks.Count(t => t.Status != "Completed");
        //         }
        //         else
        //         {
        //             ViewData[$"TaskCount_{user.Idclass}"] = 0;
        //             ViewData[$"CompletedTasks_{user.Idclass}"] = 0;
        //             ViewData[$"PendingTasks_{user.Idclass}"] = 0;
        //         }
        //     }

        //     var currentUser = users.FirstOrDefault();
        //     ViewBag.CurrentUser = currentUser?.LastName ?? "Utilisateur";
        //     ViewBag.CurrentUserId = currentUser?.Idclass ?? 1;
        //     ViewBag.UsersCount = users.Count;
        //     return View(users);
        // }
public async Task<IActionResult> Index()
{
    // Récupérer l'utilisateur connecté
    var currentUserEmail = User.Identity?.Name;
    var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == currentUserEmail);

    if (currentUser == null)
    {
        return RedirectToAction("Login", "Account");
    }

    IQueryable<User> usersQuery = _context.Users
        .Where(u => u.IsActive);

    // Filtrer selon le rôle
    if (User.IsInRole("Admin"))
    {
        usersQuery = usersQuery.Where(u => u.ManagerId == currentUser.Id);
    }
    else if (!User.IsInRole("SuperAdmin")) // Pour les membres normaux
    {
        usersQuery = usersQuery.Where(u => u.Id == currentUser.Id);
    }

    var users = await usersQuery
        .Include(u => u.AssignedTasks)
        .OrderBy(u => u.LastName)
        .ToListAsync();

    // Calculer les statistiques
    foreach (var user in users)
    {
        ViewData[$"TaskCount_{user.Idclass}"] = user.AssignedTasks?.Count ?? 0;
        ViewData[$"CompletedTasks_{user.Idclass}"] = user.AssignedTasks?.Count(t => t.Status == "Completed") ?? 0;
        ViewData[$"PendingTasks_{user.Idclass}"] = user.AssignedTasks?.Count(t => t.Status != "Completed") ?? 0;
    }

    ViewBag.CurrentUser = currentUser.LastName ?? "Utilisateur";
    ViewBag.CurrentUserId = currentUser.Idclass;
    ViewBag.UsersCount = users.Count;

    return View(users);
}
        // ✨ NOUVELLE ACTION POUR LE CHAT
        // public async Task<IActionResult> Chat()
        // {
        //     try
        //     {
        //         // Récupérer tous les utilisateurs actifs pour le chat
        //         var users = await _context.Users
        //             .Where(u => u.IsActive)
        //             .OrderBy(u => u.LastName)
        //             .ToListAsync();

        //         // Utilisateur actuel (premier utilisateur pour l'instant)
        //         var currentUser = users.FirstOrDefault();
        //         ViewBag.CurrentUser = currentUser?.LastName ?? "Utilisateur Anonyme";
        //         ViewBag.CurrentUserId = $"user-{currentUser?.Idclass ?? 1}";

        //         // ✨ CONVERTIR LES UTILISATEURS POUR LE JAVASCRIPT (AVEC GESTION D'ERREUR)
        //         var chatUsers = users.Select(u => new
        //         {
        //             id = $"user-{u.Idclass}",
        //             name = u.LastName ?? "Utilisateur",
        //             avatar = !string.IsNullOrEmpty(u.AvatarUrl) ? u.AvatarUrl :
        //                      $"https://ui-avatars.com/api/?name={Uri.EscapeDataString(u.LastName ?? "User")}&size=100&background=667eea&color=fff",
        //             status = "offline", // Par défaut offline, sera mis à jour par SignalR
        //             position = u.Position ?? "Membre de l'équipe",
        //             department = u.Department ?? "Général",
        //             email = u.Email ?? "",
        //             connectionId = (string?)null
        //         }).ToList();

        //         // ✨ SÉRIALISATION JSON SÉCURISÉE
        //         var jsonOptions = new JsonSerializerOptions
        //         {
        //             PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        //             WriteIndented = false,
        //             Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        //         };

        //         var jsonString = JsonSerializer.Serialize(chatUsers, jsonOptions);

        //         // Debug
        //         Console.WriteLine($"JSON généré: {jsonString}");
        //         Console.WriteLine($"Nombre d'utilisateurs: {users.Count}");

        //         // Passer les données à la vue via ViewBag
        //         ViewBag.TeamMembersJson = jsonString;
        //         ViewBag.TeamMembers = chatUsers;
        //         ViewBag.UsersCount = users.Count;

        //         return View();
        //     }
        //     catch (Exception ex)
        //     {
        //         Console.WriteLine($"Erreur dans Chat(): {ex.Message}");

        //         // En cas d'erreur, passer des données par défaut
        //         ViewBag.CurrentUser = "Utilisateur Test";
        //         ViewBag.CurrentUserId = "user-1";
        //         ViewBag.TeamMembersJson = "[]";
        //         ViewBag.UsersCount = 0;

        //         return View();
        //     }
        // }
        public async Task<IActionResult> Chat()
        {
            try
            {
                var users = await _context.Users
                    .Where(u => u.IsActive)
                    .OrderBy(u => u.LastName)
                    .ToListAsync();

                // Debug 1: Vérifiez ce qui est réellement chargé depuis la DB
                Console.WriteLine("=== UTILISATEURS DE LA BASE DE DONNEES ===");
                foreach (var user in users)
                {
                    Console.WriteLine($"ID: {user.Idclass}, Nom: {user.LastName}, Email: {user.Email}");
                }

                var currentUser = users.FirstOrDefault();
                ViewBag.CurrentUser = currentUser?.LastName ?? "Utilisateur";
                ViewBag.CurrentUserId = currentUser?.Idclass.ToString() ?? "1";

                var teamMembers = users.Select(u => new
                {
                    idclass = u.Idclass,
                    LastName = u.LastName ?? "Utilisateur",
                    position = u.Position ?? "Membre",
                    avatar = !string.IsNullOrEmpty(u.AvatarUrl) ? u.AvatarUrl :
                           $"https://ui-avatars.com/api/?name={Uri.EscapeDataString(u.LastName ?? "User")}&size=100&background=667eea&color=fff",
                    status = "offline"
                }).ToList();

                // Debug 2: Vérifiez la structure des données avant sérialisation
                Console.WriteLine("=== DONNEES A SERIALISER ===");
                Console.WriteLine(JsonSerializer.Serialize(teamMembers.Take(1))); // Affiche juste le premier pour vérifier la structure

                ViewBag.TeamMembersJson = JsonSerializer.Serialize(teamMembers);
                ViewBag.UsersCount = users.Count;

                return View();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERREUR CRITIQUE: {ex.ToString()}");
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
                .FirstOrDefaultAsync(u => u.Idclass == id);

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

                TempData["Success"] = $"L'utilisateur {user.LastName} a été ajouté avec succès !";
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
            if (id != user.Idclass)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(user);
                    await _context.SaveChangesAsync();

                    TempData["Success"] = $"L'utilisateur {user.LastName} a été modifié avec succès !";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!UserExists(user.Idclass))
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

                TempData["Info"] = $"L'utilisateur {user.LastName} a été désactivé.";
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
                    id = u.Idclass,
                    name = u.LastName,
                    email = u.Email,
                    avatar = !string.IsNullOrEmpty(u.AvatarUrl) ? u.AvatarUrl :
                             $"https://ui-avatars.com/api/?name={Uri.EscapeDataString(u.LastName)}&size=100&background=667eea&color=fff",
                    position = u.Position,
                    department = u.Department
                })
                .ToListAsync();

            return Json(users);
        }

        private bool UserExists(int id)
        {
            return _context.Users.Any(e => e.Idclass == id);
        }
    }
}