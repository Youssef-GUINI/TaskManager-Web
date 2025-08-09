using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using System.Collections.Concurrent;
using TaskManager.data;
using TaskManager.Models;
using Microsoft.EntityFrameworkCore;

namespace TaskManager.Helpers
{
    [Authorize] // Seuls les utilisateurs authentifiés peuvent se connecter
    public class ChatHub : Hub
    {
        private readonly ApplicationDbContext _context;

        // Dictionnaire thread-safe pour stocker les connexions
        private static readonly ConcurrentDictionary<string, UserConnection> Connections = new();

        // Dictionnaire pour mapper les utilisateurs à leurs connexions
        private static readonly ConcurrentDictionary<string, HashSet<string>> UserConnections = new();

        public ChatHub(ApplicationDbContext context)
        {
            _context = context;
        }

        public class UserConnection
        {
            public string ConnectionId { get; set; } = "";
            public string UserName { get; set; } = "";
            public string UserId { get; set; } = "";
            public string Email { get; set; } = "";
            public DateTime ConnectedAt { get; set; } = DateTime.Now;
            public DateTime LastActivity { get; set; } = DateTime.Now;
        }

        /// <summary>
        /// Utilisateur rejoint le chat
        /// </summary>
        public async Task JoinChat(string userName, string userId = "", string email = "")
        {
            if (string.IsNullOrWhiteSpace(userName)) return;

            // Ajouter la connexion
            Connections[Context.ConnectionId] = new UserConnection
            {
                ConnectionId = Context.ConnectionId,
                UserName = userName,
                UserId = userId,
                Email = email,
                ConnectedAt = DateTime.Now,
                LastActivity = DateTime.Now
            };

            // Gérer les connexions multiples pour un même utilisateur
            var userKey = !string.IsNullOrWhiteSpace(email) ? email : userName;
            if (!UserConnections.ContainsKey(userKey))
            {
                UserConnections[userKey] = new HashSet<string>();
            }
            UserConnections[userKey].Add(Context.ConnectionId);

            Console.WriteLine($"✅ {userName} ({userKey}) rejoint le chat (Connexion: {Context.ConnectionId}, Total: {Connections.Count})");

            // Notifier les autres utilisateurs
            await Clients.Others.SendAsync("UserJoined", userName, userId);

            // Envoyer la liste des utilisateurs connectés au nouveau client
            var connectedUsers = GetUniqueConnectedUsers();
            await Clients.Caller.SendAsync("ConnectedUsersList", connectedUsers);

            // Notifier tous les clients de la mise à jour
            await Clients.All.SendAsync("UpdateConnectedUsers", connectedUsers);
        }

        /// <summary>
        /// Envoyer un message privé à un utilisateur spécifique avec sauvegarde en BDD
        /// </summary>
        public async Task SendPrivateMessage(string recipientUserId, string recipientName, string message)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(message) || string.IsNullOrWhiteSpace(recipientUserId))
                    return;

                var senderConnection = Connections[Context.ConnectionId];
                if (senderConnection == null)
                {
                    await Clients.Caller.SendAsync("MessageError", "Session expirée");
                    return;
                }

                // ✨ SAUVEGARDER LE MESSAGE EN BASE DE DONNÉES
                var chatMessage = new ChatMessage
                {
                    SenderId = senderConnection.UserId,
                    ReceiverId = recipientUserId,
                    Message = message.Trim(),
                    SentAt = DateTime.Now,
                    MessageType = MessageType.Private,
                    IsRead = false
                };

                _context.ChatMessages.Add(chatMessage);
                await _context.SaveChangesAsync();

                var timestamp = DateTime.Now.ToString("HH:mm");

                var messageData = new
                {
                    messageId = chatMessage.Id,
                    senderName = senderConnection.UserName,
                    senderId = senderConnection.UserId,
                    senderEmail = senderConnection.Email,
                    recipientName = recipientName,
                    recipientId = recipientUserId,
                    message = message,
                    timestamp = timestamp,
                    messageType = "private",
                    sentAt = chatMessage.SentAt,
                    isRead = false
                };

                // Récupérer le destinataire pour obtenir son email
                var recipient = await _context.Users.FindAsync(recipientUserId);
                var recipientEmail = recipient?.Email ?? "";

                // Envoyer à tous les clients connectés du destinataire
                if (!string.IsNullOrEmpty(recipientEmail) && UserConnections.ContainsKey(recipientEmail))
                {
                    var recipientConnections = UserConnections[recipientEmail].ToList();
                    foreach (var connectionId in recipientConnections)
                    {
                        if (Connections.ContainsKey(connectionId))
                        {
                            await Clients.Client(connectionId).SendAsync("ReceivePrivateMessage", messageData);
                        }
                    }
                }
                else
                {
                    // Fallback: essayer par nom d'utilisateur
                    if (UserConnections.ContainsKey(recipientName))
                    {
                        var recipientConnections = UserConnections[recipientName].ToList();
                        foreach (var connectionId in recipientConnections)
                        {
                            if (Connections.ContainsKey(connectionId))
                            {
                                await Clients.Client(connectionId).SendAsync("ReceivePrivateMessage", messageData);
                            }
                        }
                    }
                }

                // Confirmer l'envoi à l'expéditeur
                await Clients.Caller.SendAsync("MessageSent", messageData);

                Console.WriteLine($"💬 Message privé sauvegardé (ID: {chatMessage.Id}) de {senderConnection.UserName} vers {recipientName}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erreur SendPrivateMessage: {ex.Message}");
                await Clients.Caller.SendAsync("MessageError", "Erreur lors de l'envoi du message");
            }
        }

        /// <summary>
        /// Marquer les messages comme lus
        /// </summary>
        public async Task MarkMessagesAsRead(string senderId)
        {
            try
            {
                var senderConnection = Connections[Context.ConnectionId];
                if (senderConnection == null) return;

                var unreadMessages = await _context.ChatMessages
                    .Where(m => m.SenderId == senderId &&
                               m.ReceiverId == senderConnection.UserId &&
                               !m.IsRead)
                    .ToListAsync();

                foreach (var msg in unreadMessages)
                {
                    msg.IsRead = true;
                }

                if (unreadMessages.Any())
                {
                    await _context.SaveChangesAsync();

                    // Notifier l'expéditeur que ses messages ont été lus
                    var senderEmail = await _context.Users
                        .Where(u => u.Id == senderId)
                        .Select(u => u.Email)
                        .FirstOrDefaultAsync();

                    if (!string.IsNullOrEmpty(senderEmail) && UserConnections.ContainsKey(senderEmail))
                    {
                        var senderConnections = UserConnections[senderEmail].ToList();
                        foreach (var connectionId in senderConnections)
                        {
                            await Clients.Client(connectionId).SendAsync("MessagesMarkedAsRead",
                                senderConnection.UserId, unreadMessages.Count);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erreur MarkMessagesAsRead: {ex.Message}");
            }
        }

        /// <summary>
        /// Diffuser un message à toute l'équipe (optionnel)
        /// </summary>
        public async Task SendTeamMessage(string message)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(message)) return;

                var senderConnection = Connections[Context.ConnectionId];
                if (senderConnection == null) return;

                // ✨ SAUVEGARDER LE MESSAGE D'ÉQUIPE
                var teamMessage = new ChatMessage
                {
                    SenderId = senderConnection.UserId,
                    ReceiverId = "TEAM", // Identifiant spécial pour les messages d'équipe
                    Message = message.Trim(),
                    SentAt = DateTime.Now,
                    MessageType = MessageType.Team,
                    IsRead = false
                };

                _context.ChatMessages.Add(teamMessage);
                await _context.SaveChangesAsync();

                var timestamp = DateTime.Now.ToString("HH:mm");

                var messageData = new
                {
                    messageId = teamMessage.Id,
                    senderName = senderConnection.UserName,
                    senderId = senderConnection.UserId,
                    message = message,
                    timestamp = timestamp,
                    messageType = "team",
                    sentAt = teamMessage.SentAt
                };

                await Clients.All.SendAsync("ReceiveTeamMessage", messageData);
                Console.WriteLine($"📢 Message d'équipe sauvegardé (ID: {teamMessage.Id}) de {senderConnection.UserName}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erreur SendTeamMessage: {ex.Message}");
            }
        }

        /// <summary>
        /// Notifier qu'un utilisateur est en train de taper
        /// </summary>
        public async Task NotifyTyping(string recipientUserId, bool isTyping)
        {
            try
            {
                var senderConnection = Connections[Context.ConnectionId];
                if (senderConnection == null) return;

                // Récupérer l'email du destinataire
                var recipientEmail = await _context.Users
                    .Where(u => u.Id == recipientUserId)
                    .Select(u => u.Email)
                    .FirstOrDefaultAsync();

                if (!string.IsNullOrEmpty(recipientEmail) && UserConnections.ContainsKey(recipientEmail))
                {
                    var recipientConnections = UserConnections[recipientEmail].ToList();
                    foreach (var connectionId in recipientConnections)
                    {
                        if (Connections.ContainsKey(connectionId))
                        {
                            await Clients.Client(connectionId).SendAsync("UserTyping",
                                senderConnection.UserName,
                                senderConnection.UserId,
                                isTyping);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erreur NotifyTyping: {ex.Message}");
            }
        }

        /// <summary>
        /// Mettre à jour le statut d'activité
        /// </summary>
        public async Task UpdateActivity()
        {
            if (Connections.ContainsKey(Context.ConnectionId))
            {
                Connections[Context.ConnectionId].LastActivity = DateTime.Now;
            }
        }

        /// <summary>
        /// Obtenir la liste des utilisateurs connectés (sans doublons)
        /// </summary>
        public async Task GetConnectedUsers()
        {
            var connectedUsers = GetUniqueConnectedUsers();
            await Clients.Caller.SendAsync("ConnectedUsersList", connectedUsers);
        }

        /// <summary>
        /// Utilisateur se déconnecte
        /// </summary>
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            try
            {
                if (Connections.TryRemove(Context.ConnectionId, out UserConnection? userConnection))
                {
                    var userKey = !string.IsNullOrWhiteSpace(userConnection.Email) ? userConnection.Email : userConnection.UserName;

                    // Supprimer cette connexion de la liste des connexions utilisateur
                    if (UserConnections.ContainsKey(userKey))
                    {
                        UserConnections[userKey].Remove(Context.ConnectionId);

                        // Si l'utilisateur n'a plus de connexions actives, le supprimer complètement
                        if (!UserConnections[userKey].Any())
                        {
                            UserConnections.TryRemove(userKey, out _);
                            await Clients.Others.SendAsync("UserLeft", userConnection.UserName, userConnection.UserId);
                            Console.WriteLine($"❌ {userConnection.UserName} quitte complètement le chat");
                        }
                        else
                        {
                            Console.WriteLine($"📱 Une connexion de {userConnection.UserName} fermée (reste {UserConnections[userKey].Count} connexions)");
                        }
                    }

                    // Mettre à jour la liste pour tous les clients
                    var connectedUsers = GetUniqueConnectedUsers();
                    await Clients.All.SendAsync("UpdateConnectedUsers", connectedUsers);

                    Console.WriteLine($"🔌 Connexion fermée: {Context.ConnectionId} (Restants: {Connections.Count})");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erreur OnDisconnectedAsync: {ex.Message}");
            }

            await base.OnDisconnectedAsync(exception);
        }

        /// <summary>
        /// Obtenir la liste unique des utilisateurs connectés
        /// </summary>
        private List<object> GetUniqueConnectedUsers()
        {
            var uniqueUsers = new Dictionary<string, object>();

            foreach (var connection in Connections.Values)
            {
                var userKey = !string.IsNullOrWhiteSpace(connection.Email) ? connection.Email : connection.UserName;

                if (!uniqueUsers.ContainsKey(userKey))
                {
                    uniqueUsers[userKey] = new
                    {
                        userName = connection.UserName,
                        userId = connection.UserId,
                        email = connection.Email,
                        connectedAt = connection.ConnectedAt,
                        lastActivity = connection.LastActivity,
                        connectionCount = UserConnections.ContainsKey(userKey) ? UserConnections[userKey].Count : 0
                    };
                }
            }

            return uniqueUsers.Values.ToList();
        }

        /// <summary>
        /// Méthode de diagnostic pour les administrateurs
        /// </summary>
        public async Task GetDiagnostics()
        {
            if (Connections.ContainsKey(Context.ConnectionId))
            {
                var diagnostics = new
                {
                    totalConnections = Connections.Count,
                    uniqueUsers = UserConnections.Count,
                    currentUser = Connections[Context.ConnectionId].UserName,
                    connectionId = Context.ConnectionId,
                    connectedUsers = GetUniqueConnectedUsers(),
                    totalMessages = await _context.ChatMessages.CountAsync(),
                    unreadMessages = await _context.ChatMessages.CountAsync(m => !m.IsRead)
                };

                await Clients.Caller.SendAsync("DiagnosticsResult", diagnostics);
                Console.WriteLine($"📊 Diagnostics demandés par {Connections[Context.ConnectionId].UserName}");
            }
        }

        /// <summary>
        /// Supprimer un message (soft delete)
        /// </summary>
        public async Task DeleteMessage(int messageId)
        {
            try
            {
                var senderConnection = Connections[Context.ConnectionId];
                if (senderConnection == null) return;

                var message = await _context.ChatMessages
                    .FirstOrDefaultAsync(m => m.Id == messageId && m.SenderId == senderConnection.UserId);

                if (message != null)
                {
                    message.IsDeleted = true;
                    await _context.SaveChangesAsync();

                    // Notifier tous les participants de la conversation
                    var recipientEmail = await _context.Users
                        .Where(u => u.Id == message.ReceiverId)
                        .Select(u => u.Email)
                        .FirstOrDefaultAsync();

                    if (!string.IsNullOrEmpty(recipientEmail) && UserConnections.ContainsKey(recipientEmail))
                    {
                        var recipientConnections = UserConnections[recipientEmail].ToList();
                        foreach (var connectionId in recipientConnections)
                        {
                            await Clients.Client(connectionId).SendAsync("MessageDeleted", messageId);
                        }
                    }

                    // Notifier l'expéditeur
                    await Clients.Caller.SendAsync("MessageDeleted", messageId);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erreur DeleteMessage: {ex.Message}");
            }
        }
    }
}
  