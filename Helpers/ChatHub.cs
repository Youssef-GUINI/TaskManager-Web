using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace TaskManager.Helpers
{
    public class ChatHub : Hub
    {
        private static readonly ConcurrentDictionary<string, UserConnection> Connections = new();

        public class UserConnection
        {
            public string ConnectionId { get; set; } = "";
            public string UserName { get; set; } = "";
            public DateTime ConnectedAt { get; set; } = DateTime.Now;
        }

        /// <summary>
        /// Utilisateur rejoint le chat
        /// </summary>
        public async Task JoinChat(string userName)
        {
            Connections[Context.ConnectionId] = new UserConnection
            {
                ConnectionId = Context.ConnectionId,
                UserName = userName,
                ConnectedAt = DateTime.Now
            };

            Console.WriteLine($"✅ {userName} rejoint le chat (Total: {Connections.Count})");

            await Clients.Others.SendAsync("UserJoined", userName);

            var connectedUsers = Connections.Values.Select(c => c.UserName).ToList();
            await Clients.Caller.SendAsync("UpdateConnectedUsers", connectedUsers);
        }

        /// <summary>
        /// Diffuser un message à tous (NOUVELLE VERSION SANS DOUBLON)
        /// </summary>
        public async Task SendTeamMessage(string message, string senderName, string targetUserName)
        {
            var timestamp = DateTime.Now.ToString("HH:mm");

            await Clients.All.SendAsync("ReceiveTeamMessage", new
            {
                senderName = senderName,
                targetUserName = targetUserName,
                message = message,
                timestamp = timestamp
            });

            Console.WriteLine($"💬 Message diffusé de {senderName} pour {targetUserName}: {message}");
        }

        /// <summary>
        /// Notifier qu'un utilisateur tape
        /// </summary>
        public async Task NotifyTyping(string targetConnectionId, string senderName, bool isTyping)
        {
            await Clients.Client(targetConnectionId).SendAsync("UserTyping", senderName, isTyping);
        }

        /// <summary>
        /// Utilisateur se déconnecte
        /// </summary>
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            if (Connections.TryRemove(Context.ConnectionId, out UserConnection? userConnection))
            {
                await Clients.Others.SendAsync("UserLeft", userConnection.UserName);
                Console.WriteLine($"❌ {userConnection.UserName} quitte le chat (Restants: {Connections.Count})");
            }

            await base.OnDisconnectedAsync(exception);
        }
    }
}