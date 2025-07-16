using Microsoft.AspNetCore.Mvc;
using TaskManager.Models;

namespace TaskManager.Controllers
{
    // ✅ CHANGEMENT : Hérite de Controller (pas ControllerBase) et sans [ApiController]
    public class ChatBotController : Controller
    {
        // ✅ NOUVELLE ACTION : Affiche la page de chat complète
        [HttpGet]
        public IActionResult Index()
        {
            var model = new ChatBotViewModel
            {
                UserName = "Youssef",
                SessionId = "session-" + DateTime.Now.Ticks,
                IsOnline = true
            };

            return View(model); // ✅ Maintenant ça marche !
        }

        // ✅ NOUVELLE ACTION : Envoie un message (pour la page complète)
        [HttpPost]
        public async Task<IActionResult> SendMessage([FromBody] ChatRequest request)
        {
            try
            {
                await Task.Delay(500); // Simuler le délai
                var response = GenerateResponse(request.Message);

                return Ok(new
                {
                    success = true,
                    message = response,
                    timestamp = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                return Ok(new
                {
                    success = false,
                    error = ex.Message,
                    timestamp = DateTime.Now
                });
            }
        }

        // ✅ NOUVELLE ACTION : Suggestions rapides
        [HttpGet]
        public IActionResult GetQuickSuggestions()
        {
            var suggestions = new[]
            {
                "Comment créer une nouvelle tâche ?",
                "Montre-moi mes tâches urgentes",
                "Conseils pour améliorer ma productivité",
                "Comment organiser mon workspace ?",
                "Aide sur les priorités des tâches"
            };

            return Ok(new { suggestions });
        }

        // ✅ NOUVELLE ACTION : Effacer la conversation
        [HttpPost]
        public IActionResult ClearChat()
        {
            // Ici vous pouvez ajouter la logique pour effacer l'historique
            return Ok(new { success = true });
        }

        // ✅ ACTION EXISTANTE : Pour le widget (avec route explicite)
        [HttpPost]
        [Route("ChatBot/TestMessage")]
        public async Task<IActionResult> TestMessage([FromBody] ChatRequest request)
        {
            try
            {
                await Task.Delay(500);
                var response = GenerateResponse(request.Message);

                return Ok(new
                {
                    success = true,
                    message = response,
                    timestamp = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                return Ok(new
                {
                    success = false,
                    error = $"Erreur: {ex.Message}",
                    timestamp = DateTime.Now
                });
            }
        }

        // ✅ MÉTHODE COMMUNE : Génère les réponses
        private string GenerateResponse(string message)
        {
            if (string.IsNullOrEmpty(message))
                return "Je n'ai pas reçu de message. Pouvez-vous répéter ?";

            var lowerMessage = message.ToLower();

            if (lowerMessage.Contains("bonjour") || lowerMessage.Contains("salut") || lowerMessage.Contains("hello"))
                return "Bonjour Youssef ! Je suis votre assistant TaskManager. Comment puis-je vous aider aujourd'hui ?";

            if (lowerMessage.Contains("tâche") || lowerMessage.Contains("task"))
                return "**Gestion des tâches :**\n\n• Créer une nouvelle tâche\n• Modifier une tâche existante\n• Supprimer une tâche\n• Consulter vos tâches par priorité\n\nQue souhaitez-vous faire ?";

            if (lowerMessage.Contains("créer") && lowerMessage.Contains("tâche"))
                return "Pour créer une nouvelle tâche :\n\n1. Cliquez sur **'New Task'** dans le dashboard\n2. Remplissez le titre et la description\n3. Définissez la priorité (High, Medium, Low)\n4. Choisissez une date d'échéance\n5. Cliquez sur **'Sauvegarder'**\n\nVoulez-vous que je vous guide étape par étape ?";

            if (lowerMessage.Contains("urgent") || lowerMessage.Contains("priorité"))
                return "**Vos tâches par priorité :**\n\n🔴 **High Priority** : 3 tâches\n🔵 **Medium Priority** : 5 tâches\n🟢 **Low Priority** : 3 tâches\n\nPour voir le détail, allez dans *'View All Tasks'*. Souhaitez-vous des conseils pour gérer vos priorités ?";

            if (lowerMessage.Contains("productivité") || lowerMessage.Contains("conseil"))
                return "**💡 Conseils productivité :**\n\n• **Priorisez** vos tâches importantes\n• **Planifiez** votre journée le matin\n• **Divisez** les gros projets en sous-tâches\n• **Prenez** des pauses régulières\n• **Éliminez** les distractions\n\nVoulez-vous des conseils spécifiques pour votre situation ?";

            if (lowerMessage.Contains("aide") || lowerMessage.Contains("help"))
                return "**🆘 Aide TaskManager :**\n\n**Fonctionnalités disponibles :**\n• Gestion des tâches\n• Suivi des projets\n• Analyses de productivité\n• Gestion des échéances\n\n**Navigation :**\n• Dashboard principal\n• Liste des tâches\n• Profil utilisateur\n\nSur quoi avez-vous besoin d'aide ?";

            if (lowerMessage.Contains("workspace") || lowerMessage.Contains("organiser"))
                return "**📊 Organisation du Workspace :**\n\n• **Dashboard** : Vue d'ensemble de vos tâches\n• **Quick Actions** : Accès rapide aux fonctions\n• **Graphiques** : Suivi de votre activité\n• **Statistiques** : Analyse de vos performances\n\nVoulez-vous des conseils pour personnaliser votre espace de travail ?";

            if (lowerMessage.Contains("merci"))
                return "De rien Youssef ! 😊 Je suis là pour vous aider à optimiser votre productivité avec TaskManager. N'hésitez pas si vous avez d'autres questions !";

            // Réponse par défaut
            return $"**Vous avez dit :** *\"{message}\"*\n\nJe comprends votre demande ! En tant qu'assistant TaskManager, je peux vous aider avec :\n\n• **Tâches** : Création, modification, suivi\n• **Projets** : Organisation et planification\n• **Productivité** : Conseils et optimisation\n• **Navigation** : Aide sur l'application\n\nPouvez-vous être plus précis sur ce que vous souhaitez faire ?";
        }
    }

    // ✅ CLASSE EXISTANTE : Pour les requêtes
    public class ChatRequest
    {
        public string Message { get; set; }
        public string SessionId { get; set; }
    }
}