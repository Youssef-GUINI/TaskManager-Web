using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskManager.data;
using TaskManager.Models;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

namespace TaskManager.Controllers
{
    [Authorize]
    public class TeamController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        public TeamController(ApplicationDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // =================== CHAT - PAGE DE CHAT CORRIGÉE ===================
        public async Task<IActionResult> Chat()
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    return RedirectToAction("Login", "Account");
                }

                // Récupération simplifiée des utilisateurs
                var users = await _context.Users
                    .Where(u => u.IsActive && u.Id != currentUser.Id)
                    .ToListAsync();

                Console.WriteLine($"=== UTILISATEURS POUR LE CHAT ===");
                Console.WriteLine($"Utilisateur connecté: {currentUser.FirstName} {currentUser.LastName} ({currentUser.Email})");
                Console.WriteLine($"Nombre d'utilisateurs trouvés: {users.Count}");

                ViewBag.CurrentUser = $"{currentUser.FirstName?.Trim()} {currentUser.LastName?.Trim()}".Trim();
                if (string.IsNullOrWhiteSpace(ViewBag.CurrentUser))
                    ViewBag.CurrentUser = currentUser.UserName ?? "Utilisateur";

                ViewBag.CurrentUserId = currentUser.Id;
                ViewBag.CurrentUserEmail = currentUser.Email ?? "";

                // Structure de données pour le JavaScript - traitement en mémoire
                var teamMembers = users.Select(u => new TeamMemberDto
                {
                    Id = u.Id,
                    Name = !string.IsNullOrWhiteSpace(u.FirstName) && !string.IsNullOrWhiteSpace(u.LastName)
                        ? $"{u.FirstName.Trim()} {u.LastName.Trim()}"
                        : u.UserName ?? "Utilisateur",
                    Email = u.Email ?? "",
                    Position = u.Position ?? "Membre",
                    Department = u.Department ?? "Général",
                    Avatar = !string.IsNullOrWhiteSpace(u.AvatarUrl) ? u.AvatarUrl :
                           $"https://ui-avatars.com/api/?name={Uri.EscapeDataString((u.FirstName ?? "U").Substring(0, 1))}&size=100&background=667eea&color=fff",
                    IsOnline = false // Sera mis à jour par SignalR si implémenté
                }).ToList();

                // ⚠️ CORRECTION PRINCIPALE : Ne pas sérialiser en JSON dans le ViewBag
                // Passer directement la liste d'objets
                ViewBag.TeamMembers = teamMembers;
                ViewBag.UsersCount = users.Count;

                Console.WriteLine($"Données passées à la vue: {teamMembers.Count} utilisateurs");

                return View("~/Views/ChatBot/Index.cshtml");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERREUR CRITIQUE dans Chat(): {ex}");
                ViewBag.CurrentUser = "Utilisateur";
                ViewBag.CurrentUserId = "unknown";
                ViewBag.TeamMembers = new List<TeamMemberDto>(); // Liste vide au lieu de JSON
                ViewBag.UsersCount = 0;
                return View("~/Views/ChatBot/Index.cshtml");
            }
        }

        // =================== ACTION AJAX POUR RÉCUPÉRER LES UTILISATEURS ACTIFS ===================
        [HttpGet]
        public async Task<IActionResult> GetActiveUsers()
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    return Json(new { success = false, message = "Utilisateur non authentifié" });
                }

                var users = await _context.Users
                    .Where(u => u.IsActive && u.Id != currentUser.Id)
                    .ToListAsync();

                // Traitement en mémoire avec des objets simples
                var usersList = users.Select(u => new
                {
                    id = u.Id,
                    name = !string.IsNullOrWhiteSpace(u.FirstName) && !string.IsNullOrWhiteSpace(u.LastName)
                        ? $"{u.FirstName.Trim()} {u.LastName.Trim()}"
                        : u.UserName ?? "Utilisateur",
                    email = u.Email ?? "",
                    avatar = !string.IsNullOrWhiteSpace(u.AvatarUrl)
                        ? u.AvatarUrl
                        : $"https://ui-avatars.com/api/?name={Uri.EscapeDataString((u.FirstName ?? "U").Substring(0, 1))}&size=100&background=667eea&color=fff",
                    position = u.Position ?? "Membre de l'équipe",
                    department = u.Department ?? "TaskManager",
                    status = "offline", // Statut par défaut
                    isCurrentUser = false,
                    unreadCount = 0
                })
                .OrderBy(u => u.name)
                .ToList();

                Console.WriteLine($"[AJAX] GetActiveUsers: {usersList.Count} utilisateurs trouvés");

                // ⚠️ CORRECTION : Retourner un JSON propre avec options spécifiques
                return Json(new
                {
                    success = true,
                    users = usersList,
                    count = usersList.Count
                }, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = false
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AJAX ERROR] GetActiveUsers: {ex.Message}");
                return Json(new
                {
                    success = false,
                    message = ex.Message,
                    users = new object[0],
                    count = 0
                });
            }
        }

        // =================== NOUVELLE MÉTHODE POUR LES CONVERSATIONS - CORRIGÉE ===================
        [HttpGet]
        public async Task<IActionResult> GetConversation(string userId, int page = 1, int pageSize = 50)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null || string.IsNullOrEmpty(userId))
                {
                    return Json(new { success = false, message = "Paramètres invalides" });
                }

                // ⚠️ CORRECTION : Récupérer d'abord les messages avec les données de base
                var rawMessages = await _context.ChatMessages
                    .Where(m =>
                        (m.SenderId == currentUser.Id && m.ReceiverId == userId) ||
                        (m.SenderId == userId && m.ReceiverId == currentUser.Id))
                    .Where(m => !m.IsDeleted)
                    .Include(m => m.Sender)
                    .Include(m => m.Receiver)
                    .OrderByDescending(m => m.SentAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync(); // ⚠️ Exécuter la requête d'abord

                // ⚠️ CORRECTION : Traitement en mémoire pour éviter les opérateurs null dans LINQ
                var messages = rawMessages.Select(m => new
                {
                    id = m.Id,
                    senderId = m.SenderId,
                    senderName = GetUserDisplayName(m.Sender),
                    content = m.Message,
                    sentAt = m.SentAt,
                    timestamp = m.SentAt,
                    isOwn = m.SenderId == currentUser.Id,
                    isRead = m.IsRead,
                    avatar = GetUserAvatar(m.Sender)
                }).ToList();

                // Marquer les messages comme lus
                var unreadMessages = await _context.ChatMessages
                    .Where(m => m.SenderId == userId && m.ReceiverId == currentUser.Id && !m.IsRead)
                    .ToListAsync();

                foreach (var msg in unreadMessages)
                {
                    msg.IsRead = true;
                }

                if (unreadMessages.Any())
                {
                    await _context.SaveChangesAsync();
                }

                return Json(new
                {
                    success = true,
                    messages = messages.OrderBy(m => m.sentAt).ToList(),
                    hasMore = messages.Count == pageSize
                }, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = false
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur GetConversation: {ex.Message}");
                return Json(new { success = false, message = "Erreur serveur: " + ex.Message });
            }
        }

        // ⚠️ CORRECTION : Méthodes utilitaires pour éviter les opérateurs null dans LINQ
        private static string GetUserDisplayName(User? user)
        {
            if (user == null) return "Utilisateur";

            if (!string.IsNullOrWhiteSpace(user.FirstName) && !string.IsNullOrWhiteSpace(user.LastName))
            {
                return $"{user.FirstName.Trim()} {user.LastName.Trim()}";
            }

            return user.UserName ?? "Utilisateur";
        }

        private static string GetUserAvatar(User? user)
        {
            if (user != null && !string.IsNullOrWhiteSpace(user.AvatarUrl))
            {
                return user.AvatarUrl;
            }

            var firstLetter = user?.FirstName?.Substring(0, 1) ?? "U";
            return $"https://ui-avatars.com/api/?name={Uri.EscapeDataString(firstLetter)}&size=100&background=667eea&color=fff";
        }

        // =================== NOUVELLE MÉTHODE POUR ENVOYER UN MESSAGE ===================
        [HttpPost]
        public async Task<IActionResult> SendMessage([FromBody] SendMessageRequest request)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null || string.IsNullOrEmpty(request.ReceiverId) || string.IsNullOrEmpty(request.Content))
                {
                    return Json(new { success = false, message = "Paramètres invalides" });
                }

                var message = new ChatMessage
                {
                    SenderId = currentUser.Id,
                    ReceiverId = request.ReceiverId,
                    Message = request.Content.Trim(),
                    SentAt = DateTime.Now,
                    MessageType = MessageType.Private,
                    IsRead = false
                };

                _context.ChatMessages.Add(message);
                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    messageId = message.Id,
                    timestamp = message.SentAt
                }, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur SendMessage: {ex.Message}");
                return Json(new { success = false, message = "Erreur serveur: " + ex.Message });
            }
        }

        // =================== MARQUER LES MESSAGES COMME LUS ===================
        [HttpPost]
        public async Task<IActionResult> MarkAsRead(string userId)
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null || string.IsNullOrEmpty(userId))
                {
                    return Json(new { success = false, message = "Paramètres invalides" });
                }

                var unreadMessages = await _context.ChatMessages
                    .Where(m => m.SenderId == userId && m.ReceiverId == currentUser.Id && !m.IsRead)
                    .ToListAsync();

                foreach (var message in unreadMessages)
                {
                    message.IsRead = true;
                }

                if (unreadMessages.Any())
                {
                    await _context.SaveChangesAsync();
                }

                return Json(new { success = true, markedCount = unreadMessages.Count });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur MarkAsRead: {ex.Message}");
                return Json(new { success = false, message = "Erreur serveur: " + ex.Message });
            }
        }

        // =================== RÉCUPÉRER LES COMPTEURS DE MESSAGES NON LUS ===================
        [HttpGet]
        public async Task<IActionResult> GetUnreadCounts()
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    return Json(new { success = false, message = "Utilisateur non trouvé" });
                }

                var unreadCounts = await _context.ChatMessages
                    .Where(m => m.ReceiverId == currentUser.Id && !m.IsRead && !m.IsDeleted)
                    .GroupBy(m => m.SenderId)
                    .Select(g => new
                    {
                        userId = g.Key,
                        count = g.Count()
                    })
                    .ToDictionaryAsync(x => x.userId, x => x.count);

                return Json(new
                {
                    success = true,
                    counts = unreadCounts
                }, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur GetUnreadCounts: {ex.Message}");
                return Json(new { success = false, message = "Erreur serveur: " + ex.Message });
            }
        }

        // Autres méthodes existantes (Index, Profile, etc.)
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    return RedirectToAction("Login", "Account");
                }

                var users = await _context.Users
                    .Where(u => u.Id != currentUser.Id && u.IsActive == true)
                    .ToListAsync();

                var teamMembers = users.Select(u => new TeamMemberDto
                {
                    Id = u.Id,
                    Name = !string.IsNullOrWhiteSpace(u.FirstName) && !string.IsNullOrWhiteSpace(u.LastName)
                        ? $"{u.FirstName.Trim()} {u.LastName.Trim()}"
                        : u.UserName ?? "Utilisateur",
                    Email = u.Email ?? "",
                    Avatar = !string.IsNullOrWhiteSpace(u.AvatarUrl)
                        ? u.AvatarUrl
                        : $"https://ui-avatars.com/api/?name={Uri.EscapeDataString((u.FirstName ?? "U").Substring(0, 1))}&size=100&background=667eea&color=fff",
                    Position = u.Position ?? "Membre de l'équipe",
                    Department = u.Department ?? "TaskManager",
                    IsOnline = false
                })
                .OrderBy(u => u.Name)
                .ToList();

                ViewBag.TeamMembers = teamMembers;
                ViewBag.UsersCount = teamMembers.Count;
                ViewBag.CurrentUser = currentUser.UserName ?? "Utilisateur";
                ViewBag.CurrentUserId = currentUser.Id;
                ViewBag.CurrentUserEmail = currentUser.Email ?? "";

                return View();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TEAM ERROR] Erreur dans Index: {ex.Message}");
                ViewBag.TeamMembers = new List<TeamMemberDto>();
                ViewBag.UsersCount = 0;
                ViewBag.CurrentUser = "Utilisateur";
                ViewBag.CurrentUserId = "";
                ViewBag.CurrentUserEmail = "";
                return View();
            }
        }

        public async Task<IActionResult> Profile(string id)
        {
            var user = await _context.Users
                .Include(u => u.AssignedTasks)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                return NotFound();
            }

            return View("~/Areas/Admin/Views/Workspace/Profile.cshtml", user);
        }

        private bool UserExists(string id)
        {
            return _context.Users.Any(e => e.Id == id);
        }
    }

    // =================== MODÈLES DE DONNÉES ===================
    public class TeamMemberDto
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Email { get; set; } = "";
        public string Avatar { get; set; } = "";
        public string Position { get; set; } = "";
        public string Department { get; set; } = "";
        public bool IsOnline { get; set; }
    }

    public class SendMessageRequest
    {
        public string ReceiverId { get; set; } = "";
        public string Content { get; set; } = "";
    }
}