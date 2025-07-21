//using Microsoft.AspNetCore.Mvc;
//using TaskManager.Models;

//namespace TaskManager.Controllers
//{
//    public class ChatBotController : Controller
//    {
//        private readonly IHttpClientFactory _httpClientFactory;

//        public ChatBotController(IHttpClientFactory httpClientFactory)
//        {
//            _httpClientFactory = httpClientFactory;
//        }

//        // =================== ACTIONS PRINCIPALES ===================

//        /// <summary>
//        /// Affiche la page de chat complète
//        /// </summary>
//        [HttpGet]
//        public IActionResult Index()
//        {
//            var model = new ChatBotViewModel
//            {
//                UserName = "Youssef",
//                SessionId = "session-" + DateTime.Now.Ticks,
//                IsOnline = true
//            };

//            return View(model);
//        }

//        /// <summary>
//        /// Envoie un message depuis la page complète
//        /// </summary>
//        [HttpPost]
//        public async Task<IActionResult> SendMessage([FromBody] ChatRequest request)
//        {
//            try
//            {
//                var response = await GenerateResponseAsync(request.Message);

//                return Ok(new
//                {
//                    success = true,
//                    message = response,
//                    timestamp = DateTime.Now
//                });
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"Erreur SendMessage: {ex.Message}");
//                return Ok(new
//                {
//                    success = false,
//                    error = ex.Message,
//                    timestamp = DateTime.Now
//                });
//            }
//        }

//        /// <summary>
//        /// Envoie un message depuis le widget flottant
//        /// </summary>
//        [HttpPost]
//        [Route("ChatBot/TestMessage")]
//        public async Task<IActionResult> TestMessage([FromBody] ChatRequest request)
//        {
//            try
//            {
//                var response = await GenerateResponseAsync(request.Message);

//                return Ok(new
//                {
//                    success = true,
//                    message = response,
//                    timestamp = DateTime.Now
//                });
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"Erreur TestMessage: {ex.Message}");
//                return Ok(new
//                {
//                    success = false,
//                    error = $"Erreur: {ex.Message}",
//                    timestamp = DateTime.Now
//                });
//            }
//        }

//        /// <summary>
//        /// Retourne les suggestions rapides pour l'interface
//        /// </summary>
//        [HttpGet]
//        public IActionResult GetQuickSuggestions()
//        {
//            var suggestions = new[]
//            {
//                "Comment créer une nouvelle tâche ?",
//                "Explique-moi les boucles en C#",
//                "Comment apprendre la programmation ?",
//                "Conseils pour améliorer ma productivité",
//                "Qu'est-ce que ASP.NET Core ?",
//                "Comment organiser mon workspace ?",
//                "Explique-moi les variables en C#",
//                "Conseils pour gérer mon temps",
//                "Comment structurer un projet .NET ?",
//                "Techniques de debugging efficaces"
//            };

//            return Ok(new { suggestions });
//        }

//        /// <summary>
//        /// Efface l'historique de conversation
//        /// </summary>
//        [HttpPost]
//        public IActionResult ClearChat()
//        {
//            return Ok(new { success = true, message = "Conversation effacée" });
//        }

//        // =================== GÉNÉRATION DE RÉPONSES ===================

//        /// <summary>
//        /// Méthode principale de génération de réponses
//        /// Essaie Grok IA d'abord, puis fallback vers assistant local
//        /// </summary>
//        private async Task<string> GenerateResponseAsync(string message)
//        {
//            try
//            {
//                Console.WriteLine($"🤔 Traitement de: {message}");

//                // Essayer Grok IA d'abord
//                var aiResponse = await CallGrokAsync(message);

//                if (!string.IsNullOrEmpty(aiResponse))
//                {
//                    Console.WriteLine("✅ Utilisation réponse Grok");
//                    return "🤖 **Grok IA :** " + aiResponse;
//                }

//                // Si Grok échoue, utiliser l'assistant local intelligent
//                Console.WriteLine("⚠️ Grok indisponible, utilisation assistant local intelligent");
//                return GenerateSmartLocalResponse(message);
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"💥 Erreur GenerateResponseAsync: {ex.Message}");
//                return GenerateSmartLocalResponse(message);
//            }
//        }

//        /// <summary>
//        /// Appel à l'API Grok avec la nouvelle clé
//        /// </summary>
//        private async Task<string> CallGrokAsync(string userMessage)
//        {
//            try
//            {
//                Console.WriteLine("🚀 Envoi vers Grok avec nouvelle clé...");

//                var client = _httpClientFactory.CreateClient();
//                client.Timeout = TimeSpan.FromSeconds(30);

//                // ✅ NOUVELLE CLÉ GROK
//                var apiKey = "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";

//                if (string.IsNullOrEmpty(apiKey))
//                {
//                    Console.WriteLine("❌ Clé API manquante");
//                    return null;
//                }

//                client.DefaultRequestHeaders.Clear();
//                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
//                client.DefaultRequestHeaders.Add("User-Agent", "TaskManager-Assistant/1.0");

//                var requestBody = new
//                {
//                    model = "grok-beta",
//                    messages = new[]
//                    {
//                        new {
//                            role = "system",
//                            content = @"Tu es un assistant IA intelligent intégré dans TaskManager, une application de gestion de tâches développée en ASP.NET Core.

//CONTEXTE UTILISATEUR :
//- Nom : Youssef GUINI
//- Étudiant/Stagiaire en développement .NET
//- Utilise TaskManager pour gérer ses tâches
//- Localisation : Casablanca, Maroc
//- Passionné par la programmation C# et le développement web

//TU PEUX RÉPONDRE À :
//1. Questions sur TaskManager (création tâches, productivité, organisation)
//2. Questions techniques (C#, ASP.NET Core, programmation, développement web)
//3. Questions générales (apprentissage, conseils, technologie)
//4. Questions personnelles (études, stage, conseils carrière)

//INSTRUCTIONS :
//- Réponds TOUJOURS en français
//- Sois utile, professionnel et encourageant
//- Adapte ton niveau selon la question (débutant à avancé)
//- Utilise des emojis pour rendre tes réponses vivantes
//- Donne des exemples concrets quand c'est pertinent
//- Si c'est du code, utilise la syntaxe C# par défaut
//- Garde tes réponses entre 100-400 mots maximum
//- Encourage l'apprentissage et la curiosité
//- Personnalise tes réponses pour Youssef quand approprié

//STYLE :
//- Commence par un emoji approprié
//- Structure tes réponses avec des titres en gras
//- Utilise des puces pour les listes
//- Termine par une question ou encouragement quand approprié"
//                        },
//                        new { role = "user", content = userMessage }
//                    },
//                    max_tokens = 400,
//                    temperature = 0.7,
//                    stream = false
//                };

//                var jsonOptions = new System.Text.Json.JsonSerializerOptions
//                {
//                    PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
//                };

//                var json = System.Text.Json.JsonSerializer.Serialize(requestBody, jsonOptions);
//                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

//                Console.WriteLine($"📝 Envoi vers: https://api.x.ai/v1/chat/completions");

//                var response = await client.PostAsync("https://api.x.ai/v1/chat/completions", content);

//                Console.WriteLine($"📡 Status Code: {response.StatusCode}");

//                if (!response.IsSuccessStatusCode)
//                {
//                    var errorContent = await response.Content.ReadAsStringAsync();
//                    Console.WriteLine($"❌ Erreur Grok API: {response.StatusCode} - {errorContent}");
//                    return null;
//                }

//                var responseContent = await response.Content.ReadAsStringAsync();
//                Console.WriteLine($"📥 Réponse reçue (taille: {responseContent.Length} chars)");

//                var grokResponse = System.Text.Json.JsonSerializer.Deserialize<GrokApiResponse>(responseContent, jsonOptions);
//                var result = grokResponse?.choices?.FirstOrDefault()?.message?.content;

//                if (!string.IsNullOrEmpty(result))
//                {
//                    Console.WriteLine($"✅ Succès Grok! Contenu: {result.Substring(0, Math.Min(50, result.Length))}...");
//                    return result;
//                }

//                Console.WriteLine("⚠️ Réponse Grok vide");
//                return null;
//            }
//            catch (HttpRequestException httpEx)
//            {
//                Console.WriteLine($"❌ Erreur HTTP Grok: {httpEx.Message}");
//                return null;
//            }
//            catch (TaskCanceledException timeoutEx)
//            {
//                Console.WriteLine($"⏰ Timeout Grok: {timeoutEx.Message}");
//                return null;
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"💥 Erreur générale Grok: {ex.Message}");
//                return null;
//            }
//        }

//        /// <summary>
//        /// Assistant local intelligent (fallback)
//        /// Fournit des réponses détaillées sans API externe
//        /// </summary>
//        private string GenerateSmartLocalResponse(string message)
//        {
//            var lowerMessage = message.ToLower();

//            // =================== SALUTATIONS ===================
//            if (ContainsAny(lowerMessage, new[] { "bonjour", "salut", "hello", "bonsoir", "hey" }))
//                return "👋 **Bonjour Youssef !**\n\nJe suis votre assistant IA TaskManager. Grok n'est pas disponible pour le moment, mais je peux quand même vous aider efficacement !\n\n**💡 Essayez des questions comme :**\n• \"Explique-moi les boucles en C#\"\n• \"Comment créer une tâche efficace ?\"\n• \"Conseils pour apprendre la programmation\"\n• \"Comment améliorer ma productivité ?\"\n\nQuelle est votre question ?";

//            // =================== BOUCLES C# ===================
//            if (ContainsAny(lowerMessage, new[] { "boucle", "loop" }) && ContainsAny(lowerMessage, new[] { "c#", "csharp" }))
//                return "🔄 **Les boucles en C# - Guide TaskManager**\n\n**1. Boucle FOR :**\n```csharp\n// Afficher toutes les tâches\nfor (int i = 0; i < tasks.Count; i++)\n{\n    Console.WriteLine($\"Tâche {i+1}: {tasks[i].Title}\");\n}\n\n// Exemple avec priorités\nfor (int priority = 1; priority <= 3; priority++)\n{\n    var tasksAtPriority = tasks.Where(t => t.PriorityLevel == priority);\n    Console.WriteLine($\"Priorité {priority}: {tasksAtPriority.Count()} tâches\");\n}\n```\n\n**2. Boucle FOREACH :**\n```csharp\n// Traiter chaque tâche\nforeach (var task in allTasks)\n{\n    if (task.Priority == \"High\")\n    {\n        Console.WriteLine($\"URGENT: {task.Title}\");\n    }\n    \n    // Marquer comme urgent si proche deadline\n    if (task.DueDate <= DateTime.Now.AddDays(1))\n    {\n        task.IsUrgent = true;\n    }\n}\n\n// Avec LINQ\nforeach (var highTask in tasks.Where(t => t.Priority == \"High\"))\n{\n    SendUrgentNotification(highTask);\n}\n```\n\n**3. Boucle WHILE :**\n```csharp\n// Traiter jusqu'à condition\nint index = 0;\nwhile (index < incompleteTasks.Count)\n{\n    if (incompleteTasks[index].IsCompleted)\n        incompleteTasks.RemoveAt(index);\n    else\n        index++;\n}\n```\n\n**💡 Dans TaskManager, utilisez :**\n• **FOR** : Pagination, indexation\n• **FOREACH** : Parcours collections, traitement\n• **WHILE** : Conditions dynamiques\n\nQuelle boucle voulez-vous approfondir ?";

//            // =================== VARIABLES C# ===================
//            if (ContainsAny(lowerMessage, new[] { "variable", "type" }) && ContainsAny(lowerMessage, new[] { "c#", "csharp" }))
//                return "📦 **Variables C# - Exemples TaskManager**\n\n**Types de base avec TaskManager :**\n```csharp\n// Identifiants et compteurs\nint taskId = 1;\nlong totalTasksEver = 1234567890L;\nshort priorityLevel = 3;\n\n// Texte et caractères\nstring title = \"Développer chat bot IA\";\nstring description = \"Intégrer Grok dans TaskManager\";\nchar priority = 'H'; // H, M, L\n\n// Dates (très important pour TaskManager !)\nDateTime createdDate = DateTime.Now;\nDateTime dueDate = DateTime.Now.AddDays(7);\nTimeSpan timeRemaining = dueDate - DateTime.Now;\n\n// État et flags\nbool isCompleted = false;\nbool isUrgent = timeRemaining.Days <= 1;\nbool hasAttachments = true;\n\n// Nombres décimaux\ndouble progressPercentage = 67.5;\nfloat estimatedHours = 8.5f;\ndecimal budgetAllocated = 1500.00m;\n```\n\n**Collections pour TaskManager :**\n```csharp\n// Listes dynamiques\nList<string> tags = new List<string> { \"urgent\", \"backend\", \"api\" };\nList<TaskModel> userTasks = new List<TaskModel>();\n\n// Tableaux fixes\nstring[] assignedUsers = { \"Youssef\", \"Alice\", \"Bob\" };\nint[] priorityCounters = { 3, 5, 2 }; // High, Medium, Low\n\n// Dictionnaires (clé-valeur)\nDictionary<string, int> priorityCounts = new Dictionary<string, int>\n{\n    { \"High\", 3 },\n    { \"Medium\", 5 },\n    { \"Low\", 2 }\n};\n\nDictionary<int, TaskModel> taskLookup = new Dictionary<int, TaskModel>();\n```\n\n**Déclaration avancée :**\n```csharp\n// Inférence de type (var)\nvar task = new TaskModel { Title = \"Nouvelle tâche\", Priority = \"High\" };\nvar completedTasks = tasks.Where(t => t.IsCompleted).ToList();\nvar tasksByPriority = tasks.GroupBy(t => t.Priority);\n\n// Nullable (peut être null)\nint? parentTaskId = null; // Pas de tâche parent\nDateTime? actualCompletionDate = null; // Pas encore complétée\nstring? assignedUser = null; // Pas encore assignée\n\n// Constantes\nconst int MAX_TASKS_PER_USER = 50;\nconst string DEFAULT_PRIORITY = \"Medium\";\nconst double COMPLETION_THRESHOLD = 80.0;\n\n// Readonly (défini une seule fois)\nreadonly string APPLICATION_NAME = \"TaskManager\";\nreadonly DateTime APP_START_TIME = DateTime.Now;\n```\n\n**💡 Bonnes pratiques TaskManager :**\n• **var** quand le type est évident\n• **DateTime** pour toutes les dates\n• **List<T>** pour collections dynamiques\n• **Dictionary** pour lookup rapide\n• **bool** pour les états/flags\n• **Nullable** pour valeurs optionnelles\n\nQuel type de variable vous pose question ?";

//            // =================== ASP.NET CORE ===================
//            if (ContainsAny(lowerMessage, new[] { "asp.net", "aspnet", "core", "mvc" }))
//                return "🌐 **ASP.NET Core MVC - Architecture TaskManager**\n\n**🏗️ Structure complète de TaskManager :**\n```\nTaskManager/\n├── Controllers/              ← Logique métier\n│   ├── HomeController.cs     ← Page d'accueil\n│   ├── TasksController.cs    ← CRUD des tâches\n│   ├── WorkspaceController.cs ← Dashboard\n│   └── ChatBotController.cs  ← Assistant IA (nous !)\n├── Models/                   ← Structures de données\n│   ├── TaskModel.cs          ← Modèle des tâches\n│   ├── UserProfile.cs        ← Profil utilisateur\n│   ├── WorkspaceVM.cs        ← Vue du dashboard\n│   └── ChatBotViewModel.cs   ← Chat interface\n├── Views/                    ← Interface utilisateur\n│   ├── Tasks/                ← Pages des tâches\n│   │   ├── Index.cshtml      ← Liste des tâches\n│   │   └── Create.cshtml     ← Formulaire création\n│   ├── Workspace/            ← Dashboard\n│   │   ├── Index.cshtml      ← Page principale\n│   │   └── Profile.cshtml    ← Profil utilisateur\n│   ├── ChatBot/              ← Assistant IA\n│   │   └── Index.cshtml      ← Chat complet\n│   └── Shared/               ← Layouts communs\n│       ├── _Layout.cshtml    ← Template principal\n│       └── _ChatWidget.cshtml ← Widget flottant\n├── Data/                     ← Base de données\n│   └── ApplicationDbContext.cs ← Context EF\n├── wwwroot/                  ← Fichiers statiques\n│   ├── css/                  ← Styles\n│   ├── js/                   ← JavaScript\n│   └── images/               ← Images\n└── Program.cs                ← Configuration app\n```\n\n**🔄 Flux MVC typique dans TaskManager :**\n```csharp\n// 1. Contrôleur reçoit requête\n[HttpGet]\npublic async Task<IActionResult> Index()\n{\n    // 2. Récupère données via Entity Framework\n    var tasks = await _context.Tasks\n        .Where(t => t.UserId == currentUserId)\n        .OrderByDescending(t => t.CreatedDate)\n        .ToListAsync();\n    \n    // 3. Prépare ViewModel pour la vue\n    var viewModel = new TaskListViewModel\n    {\n        Tasks = tasks,\n        TotalCount = tasks.Count,\n        CompletedCount = tasks.Count(t => t.Status == \"Completed\"),\n        HighPriorityCount = tasks.Count(t => t.Priority == \"High\")\n    };\n    \n    // 4. Retourne vue avec données\n    return View(viewModel);\n}\n```\n\n**💾 Entity Framework dans TaskManager :**\n```csharp\n// ApplicationDbContext.cs\npublic class ApplicationDbContext : DbContext\n{\n    public DbSet<TaskModel> Tasks { get; set; }\n    public DbSet<UserProfile> Users { get; set; }\n    \n    protected override void OnModelCreating(ModelBuilder modelBuilder)\n    {\n        // Configuration TaskModel\n        modelBuilder.Entity<TaskModel>()\n            .HasKey(t => t.Id);\n            \n        modelBuilder.Entity<TaskModel>()\n            .Property(t => t.Title)\n            .IsRequired()\n            .HasMaxLength(200);\n            \n        modelBuilder.Entity<TaskModel>()\n            .Property(t => t.Priority)\n            .HasDefaultValue(\"Medium\");\n    }\n}\n```\n\n**💡 Points clés ASP.NET Core :**\n• **Separation of Concerns** : MVC sépare logique/vue/données\n• **Dependency Injection** : Services injectés automatiquement\n• **Middleware Pipeline** : Traitement des requêtes en chaîne\n• **Entity Framework** : ORM pour base de données\n• **Razor Views** : Templates dynamiques\n\nQuelle partie d'ASP.NET Core vous intéresse le plus ?";

//            // =================== TASKMANAGER SPÉCIFIQUE ===================
//            if (ContainsAny(lowerMessage, new[] { "tâche", "task", "créer", "gestion" }))
//                return "📋 **Maîtriser TaskManager - Guide expert pour Youssef**\n\n**🚀 Créer une tâche optimale :**\n\n**1. Titre efficace :**\n❌ \"Travail sur le site\"\n✅ \"Implémenter authentification JWT TaskManager\"\n✅ \"Corriger bug affichage mobile dashboard\"\n✅ \"Ajouter tests unitaires ChatBotController\"\n✅ \"Intégrer notifications temps réel SignalR\"\n\n**2. Description structurée :**\n```\n## 🎯 Objectif\nIntégrer système de notifications push temps réel\n\n## 📋 Sous-étapes\n- [ ] Installer package SignalR\n- [ ] Créer NotificationHub.cs\n- [ ] Ajouter endpoints API notifications\n- [ ] Intégrer frontend JavaScript\n- [ ] Tester notifications cross-browser\n\n## ✅ Critères de réussite\n- Notifications instantanées (<2s)\n- Compatible tous navigateurs modernes\n- Tests unitaires passent (>90%)\n- Performance maintenue\n\n## 📚 Ressources\n- Doc SignalR: https://docs.microsoft.com/signalr\n- Exemple GitHub: [lien projet similaire]\n- Tutoriel vidéo: [lien YouTube]\n```\n\n**3. Priorités intelligentes :**\n• **High (🔴)** : Bugs bloquants, deadlines < 2 jours, dépendances critiques\n• **Medium (🔵)** : Features importantes, deadlines < 1 semaine, améliorations UX\n• **Low (🟢)** : Nice-to-have, refactoring, documentation, learning\n\n**⚡ Workflow quotidien optimisé :**\n\n**Morning Routine (10 min) :**\n1. **Check stats** dans TaskManager dashboard\n2. **Identifiez 3 priorités** max pour aujourd'hui\n3. **Estimez temps** pour chaque (1h, 2h, 4h, 8h)\n4. **Time-box** sur calendrier\n5. **Préparez environnement** (IDE, docs, café ☕)\n\n**During Work :**\n• **Pomodoro** : 25min focus + 5min pause\n• **Update status** après chaque session\n• **Notes rapides** des problèmes rencontrés\n• **Commits fréquents** avec messages clairs\n\n**Evening Review (5 min) :**\n1. **Update TaskManager** : statuts, temps réel\n2. **Plan demain** : 3 nouvelles priorités\n3. **Log learnings** : nouvelles compétences\n4. **Celebrate wins** : même les petites ! 🎉\n\n**🎯 Objectif immédiat :**\n**18% → 50% de tâches complétées cette semaine !**\n\n**Plan d'action concret :**\n1. **Créez 10 tâches** bien définies (mix High/Medium/Low)\n2. **Estimez temps** réaliste pour chaque\n3. **Commencez par High** priority (momentum)\n4. **Track progress** 2x/jour minimum\n5. **Ajustez stratégie** si nécessaire\n\nQuelle partie de la gestion vous bloque le plus ?";

//            // =================== PRODUCTIVITÉ ===================
//            if (ContainsAny(lowerMessage, new[] { "productivité", "efficace", "temps", "organisation" }))
//                return "⚡ **Productivité développeur - Méthodes Youssef**\n\n**📊 Analyse de vos 18% :**\nC'est un excellent point de départ ! Objectif réaliste : **40-50% cette semaine**.\n\n**🎯 Techniques spéciales développeur :**\n\n**1. Deep Work Sessions**\n• **2-4h blocs** sans interruption\n• Mode avion + notifications OFF\n• Une seule tâche complexe\n• **Idéal pour** : Architecture, algorithms, learning\n\n**2. Pomodoro Adapté Code**\n• **25 min coding** intensif (pas de Stack Overflow !)\n• **5 min pause** : yeux, étirements, réflexion\n• **4 cycles** puis 30 min pause longue\n• **Parfait pour** : Debug, refactoring, features\n\n**3. Time-boxing par Type**\n```\n9h-11h   : Développement complexe (nouvelles features)\n11h-12h  : Code review, tests, refactoring\n14h-15h  : Learning (docs, tutos, veille tech)\n15h-17h  : TaskManager improvements\n17h-18h  : Side projects, contributions open source\n```\n\n**💻 Stack technique productivité :**\n\n**IDE & Extensions :**\n• **Visual Studio** : IntelliCode, Live Share\n• **Extensions** : ReSharper, CodeMaid, GitLens\n• **Shortcuts** : Ctrl+. (quick fixes), F12 (go to def)\n\n**Workflow Git optimisé :**\n```bash\n# Commits fréquents (30-60 min)\ngit add .\ngit commit -m \"Add user authentication validation\"\n\n# Branches features\ngit checkout -b feature/chat-notifications\n# Travaillez sans casser main\n\n# Push régulier\ngit push origin feature/chat-notifications\n```\n\n**🧠 Techniques mentales :**\n\n**Rubber Duck Debugging :**\n• Expliquez votre code à voix haute\n• Forcez compréhension ligne par ligne\n• Identifiez bugs plus rapidement\n\n**Feynman Technique (Learning) :**\n1. **Étudiez** concept (ex: Entity Framework)\n2. **Enseignez** à quelqu'un (ou vous-même)\n3. **Identifiez** lacunes de compréhension\n4. **Retournez** aux sources pour combler\n\n**📱 Apps & Outils recommandés :**\n• **TaskManager** : Votre hub central ! 🎯\n• **Notion** : Documentation, notes de cours\n• **RescueTime** : Tracking automatique temps\n• **Forest** : Focus timer avec gamification\n• **GitHub Desktop** : Git visuel simplifié\n\n**🚫 Productivity Killers à éviter :**\n• **Multitasking** : -40% efficacité prouvé\n• **Context switching** : Regroupez tâches similaires\n• **Perfectionnisme** : \"Done is better than perfect\"\n• **Réunions sans agenda** : Perte de temps massive\n• **Notifications constantes** : Coupe le flow\n\n**🏆 Challenge cette semaine :**\n\n**Jour 1-2** : Setup workflow (TaskManager + time-boxing)\n**Jour 3-4** : Application techniques (Pomodoro + Deep Work)\n**Jour 5-6** : Optimisation (mesure temps réel vs estimé)\n**Jour 7** : Review et ajustements pour semaine suivante\n\n**Métrique de succès :** 18% → 45%+ tâches complétées\n\nQuel aspect productivité voulez-vous améliorer en premier ?";

//            // =================== APPRENTISSAGE ===================
//            if (ContainsAny(lowerMessage, new[] { "apprendre", "formation", "étudier", "cours" }))
//                return "📚 **Plan d'apprentissage personnalisé - Youssef GUINI**\n\n**🎯 Votre niveau actuel :**\nVous développez TaskManager en C#/ASP.NET Core → **Niveau intermédiaire confirmé** ! 💪\n\n**📈 Roadmap de progression (3 mois) :**\n\n**🚀 Phase 1 : Consolidation (3-4 semaines)**\n**Objectif :** Maîtriser les bases solidement\n\n*Semaine 1-2 : C# Avancé*\n• **LINQ** : Queries complexes, méthodes extension\n• **Async/Await** : Programmation asynchrone\n• **Generics** : Types génériques, contraintes\n• **Events & Delegates** : Programmation événementielle\n\n*Semaine 3-4 : ASP.NET Core Approfondissement*\n• **Entity Framework** : Relations, migrations, performance\n• **Dependency Injection** : Scoped, Singleton, Transient\n• **Middleware** : Custom middleware, pipeline\n• **Configuration** : appsettings, environment variables\n\n**🔥 Phase 2 : Spécialisation (4-5 semaines)**\n**Objectif :** Devenir expert web développeur\n\n*Semaine 5-6 : APIs & Frontend*\n• **Web APIs** : RESTful design, OpenAPI/Swagger\n• **Authentication** : JWT, Identity, OAuth\n• **JavaScript** : ES6+, async/await, fetch API\n• **SignalR** : Real-time communication\n\n*Semaine 7-8 : Testing & Quality*\n• **Unit Testing** : xUnit, Moq, Test-Driven Development\n• **Integration Testing** : WebApplicationFactory\n• **Code Quality** : SonarQube, code coverage\n• **Debugging** : Advanced techniques, performance profiling\n\n*Semaine 9 : DevOps & Deployment*\n• **Docker** : Containerization basics\n• **Azure** : App Service, SQL Database\n• **CI/CD** : GitHub Actions, automated deployment\n• **Monitoring** : Application Insights, logging\n\n**⚡ Phase 3 : Expertise (4 semaines)**\n**Objectif :** Architecte logiciel junior\n\n*Semaine 10-11 : Architecture*\n• **Clean Architecture** : Domain, Application, Infrastructure\n• **Design Patterns** : Repository, Factory, Observer\n• **SOLID Principles** : Single Responsibility, etc.\n• **Microservices** : Concepts, communication patterns\n\n*Semaine 12-13 : Advanced Topics*\n• **Performance** : Caching (Redis), optimization\n• **Security** : OWASP Top 10, secure coding\n• **Scalability** : Load balancing, database optimization\n• **Event Sourcing** : CQRS, event-driven architecture\n\n**📚 Ressources par niveau :**\n\n**Débutant → Intermédiaire :**\n• **Microsoft Learn** (gratuit) : ASP.NET Core path\n• **YouTube - IAmTimCorey** : C# fundamentals\n• **Pluralsight** : .NET Core development path\n\n**Intermédiaire → Avancé :**\n• **Clean Code** (livre) : Robert C. Martin\n• **YouTube - Nick Chapsas** : Advanced .NET tips\n• **Architecture Patterns** : Martin Fowler\n\n**Avancé → Expert :**\n• **Domain-Driven Design** : Eric Evans\n• **Microservices Patterns** : Chris Richardson\n• **Conference talks** : NDC, Build, Connect()\n\n**🎯 Routine d'apprentissage optimale :**\n\n**Quotidien (2h minimum) :**\n• **7h-8h** : Théorie (lecture, vidéos) ☕\n• **12h-13h** : Practice (exercices, mini-projets) 🍽️\n• **17h-18h** : Application (TaskManager improvements) 💻\n\n**Weekend (4-6h) :**\n• **Samedi** : Projet side (nouveau concept)\n• **Dimanche** : Review semaine + planification suivante\n\n**🏆 Projets recommandés (après TaskManager) :**\n1. **API TaskManager** : Créer REST API pour mobile\n2. **Blog Personnel** : Portfolio + articles techniques\n3. **E-commerce Mini** : Catalogue + panier + paiement\n4. **Chat Real-time** : SignalR + authentification\n5. **Microservice** : Decomposer TaskManager\n\nSur quel aspect voulez-vous vous concentrer cette semaine ?";

//            // =================== AIDE GÉNÉRALE ===================
//            if (ContainsAny(lowerMessage, new[] { "aide", "help" }))
//                return "🆘 **Guide d'aide complet - Assistant TaskManager**\n\n**🤖 Mode actuel :** Assistant Local Intelligent\n(Grok IA temporairement indisponible)\n\n**💪 Mes capacités étendues :**\n\n**🔧 Expertise technique :**\n• **C# & .NET** : Syntaxe, POO, LINQ, async/await\n• **ASP.NET Core** : MVC, Entity Framework, APIs\n• **Développement web** : HTML, CSS, JavaScript\n• **Base de données** : SQL Server, migrations EF\n• **Architecture** : Design patterns, clean code\n• **Testing** : Unit tests, integration tests\n• **DevOps** : Git, Docker, CI/CD, déploiement\n\n**📋 TaskManager Expert :**\n• **Gestion optimale** : Création, priorisation, suivi\n• **Techniques productivité** : Pomodoro, GTD, time-boxing\n• **Analyse performances** : Statistiques, amélioration\n• **Workflow développeur** : Méthodologies agiles adaptées\n\n**📚 Mentoring apprentissage :**\n• **Plans d'étude** : Roadmaps personnalisées\n• **Ressources qualité** : Livres, cours, tutoriels\n• **Méthodes efficaces** : Techniques d'apprentissage rapide\n• **Progression carrière** : Conseils stage, emploi\n\n**⚡ Productivité & Organisation :**\n• **Gestion temps** : Techniques pour développeurs\n• **Organisation workspace** : Environnement optimal\n• **Élimination distractions** : Focus et concentration\n• **Équilibre vie/code** : Prévention burnout\n\n**💬 Comment obtenir la meilleure aide :**\n\n**✅ Questions efficaces :**\n• \"Explique-moi [concept] avec exemples TaskManager\"\n• \"Comment implémenter [feature] en ASP.NET Core ?\"\n• \"Plan pour apprendre [technologie] en [temps] ?\"\n• \"Stratégie pour passer de [niveau A] à [niveau B] ?\"\n• \"Debug [problème spécifique] dans mon code\"\n\n**❌ Questions trop vagues :**\n• \"Aide-moi\"\n• \"Comment coder ?\"\n• \"C'est quoi .NET ?\"\n\n**🎯 Contexte que je connais sur vous :**\n• **Nom** : Youssef GUINI\n• **Projet** : TaskManager (ASP.NET Core + C#)\n• **Niveau** : Développeur intermédiaire\n• **Objectif** : Améliorer skills + productivité\n• **Localisation** : Casablanca, Maroc\n• **Status** : Étudiant/Stagiaire développement\n• **Progression** : 18% → 50% tâches complétées\n\n**🚀 Suggestions de questions populaires :**\n\n**Technique immédiat :**\n• \"Explique-moi les boucles foreach avec LINQ\"\n• \"Comment structurer mes models ASP.NET Core ?\"\n• \"Différence entre async et sync, quand utiliser ?\"\n• \"Comment optimiser mes requêtes Entity Framework ?\"\n\n**TaskManager amélioration :**\n• \"Comment passer de 18% à 50% de tâches complétées ?\"\n• \"Technique pour gérer 15+ tâches simultanées efficacement ?\"\n• \"Workflow pour développeur : prioriser code vs learning ?\"\n\n**Apprentissage stratégique :**\n• \"Plan 30 jours pour maîtriser Entity Framework ?\"\n• \"Roadmap développeur web complet en 6 mois ?\"\n• \"Comment structurer mes sessions d'apprentissage quotidiennes ?\"\n\n**Productivité développeur :**\n• \"Technique pour rester concentré 4h sur du code complexe ?\"\n• \"Comment organiser ma journée : code, learning, projets ?\"\n• \"Méthodes anti-procrastination spéciales développeurs ?\"\n\n**💡 Fonctionnalités spéciales :**\n• **Code examples** : Exemples pratiques TaskManager\n• **Step-by-step** : Tutoriels détaillés\n• **Roadmaps** : Plans d'apprentissage personnalisés\n• **Best practices** : Conseils d'expert\n• **Real-world** : Applications concrètes\n\nQuelle est votre question précise aujourd'hui ?";

//            // =================== MERCI ===================
//            if (ContainsAny(lowerMessage, new[] { "merci", "thanks" }))
//                return "😊 **De rien Youssef !**\n\nC'était un plaisir de vous aider ! 🚀\n\n**🎯 Pour continuer sur votre lancée :**\n• **Appliquez** les conseils qu'on a vus ensemble\n• **Pratiquez** quotidiennement (consistency is key!)\n• **Mesurez** vos progrès dans TaskManager\n• **Ajustez** votre approche selon les résultats\n\n**💡 Prochaines étapes suggérées :**\nMaintenant que TaskManager fonctionne bien avec l'assistant IA, pourquoi ne pas ajouter :\n• **Notifications temps réel** (SignalR)\n• **API REST** pour app mobile\n• **Authentification** utilisateurs multiples\n• **Analytics avancés** des tâches\n• **Export/Import** données\n\n**🏆 Rappel objectif :**\n**18% → 50% de tâches complétées cette semaine !**\n\n**📈 Progression long terme :**\n• **Mois 1** : TaskManager v2.0 avec nouvelles features\n• **Mois 2** : Portfolio GitHub + blog technique\n• **Mois 3** : Niveau expert ASP.NET Core\n\n**💪 Keep coding, keep learning, stay productive!**\n\nÀ très bientôt pour d'autres questions techniques ! 😄\n\n*P.S. : N'oubliez pas de célébrer chaque petite victoire en chemin ! 🎉*";

//            // =================== RÉPONSE PAR DÉFAUT INTELLIGENTE ===================
//            return $"💬 **Message reçu :** *\"{message}\"*\n\n**🤖 Assistant Local Intelligent activé !**\n(Grok IA temporairement indisponible)\n\n**💡 Je suis spécialement optimisé pour vous aider, Youssef, avec :**\n\n**🔧 Développement technique :**\n• **C# avancé** : LINQ, async/await, generics, delegates\n• **ASP.NET Core** : MVC, Entity Framework, APIs, middleware\n• **Web development** : Frontend/backend, SignalR, authentication\n• **Architecture** : Design patterns, clean code, testing\n• **DevOps** : Git, Docker, deployment, monitoring\n\n**📋 TaskManager mastery :**\n• **Création tâches** : Titres efficaces, descriptions structurées\n• **Gestion priorités** : High/Medium/Low optimization\n• **Workflow productif** : De 18% à 50%+ completion\n• **Analytics** : Exploitation statistiques dashboard\n• **Time management** : Estimation, tracking, amélioration\n\n**📚 Apprentissage accéléré :**\n• **Roadmaps personnalisés** : Plans 30/60/90 jours\n• **Ressources curated** : Livres, cours, tutoriels quality\n• **Techniques learning** : Feynman, spaced repetition\n• **Career guidance** : Stage, emploi, portfolio\n\n**⚡ Productivité développeur :**\n• **Deep work** : Sessions 2-4h sans distraction\n• **Pomodoro adapté** : 25min code + 5min pause\n• **Time-boxing** : Organisation journée type\n• **Anti-procrastination** : Techniques éprouvées\n\n**💬 Pour une réponse ultra-précise, reformulez avec :**\n\n**Format optimal :** \"[Action] + [Technologie/Concept] + [Contexte]\"\n\n**Exemples excellents :**\n• \"Explique-moi les delegates C# avec exemples TaskManager\"\n• \"Comment implémenter authentification JWT dans ASP.NET Core ?\"\n• \"Plan 3 semaines pour maîtriser Entity Framework relations\"\n• \"Workflow pour gérer 20 tâches dev sans stress\"\n• \"Debugging technique pour async/await problems\"\n• \"Architecture recommandée pour scaling TaskManager\"\n\n**🎯 Spécialisations par domaine :**\n• **Backend** : APIs, databases, services, architecture\n• **Frontend** : JavaScript, Razor, responsive design\n• **DevOps** : CI/CD, containers, cloud deployment\n• **Quality** : Testing, code review, performance\n• **Soft skills** : Productivity, learning, communication\n\nQuelle est votre question technique précise ?";
//        }

//        /// <summary>
//        /// Méthode utilitaire pour vérifier si un texte contient certains mots-clés
//        /// </summary>
//        private bool ContainsAny(string text, string[] keywords)
//        {
//            return keywords.Any(keyword => text.Contains(keyword, StringComparison.OrdinalIgnoreCase));
//        }
//    }

//    // =================== CLASSES DE DONNÉES ===================

//    /// <summary>
//    /// Modèle pour les requêtes de chat
//    /// </summary>
//    public class ChatRequest
//    {
//        public string Message { get; set; } = "";
//        public string SessionId { get; set; } = "";
//    }

//    /// <summary>
//    /// Modèles pour la réponse de l'API Grok
//    /// </summary>
//    public class GrokApiResponse
//    {
//        public GrokChoice[] choices { get; set; } = Array.Empty<GrokChoice>();
//        public GrokUsage usage { get; set; } = new GrokUsage();
//    }

//    public class GrokChoice
//    {
//        public GrokMessage message { get; set; } = new GrokMessage();
//        public string finish_reason { get; set; } = "";
//    }

//    public class GrokMessage
//    {
//        public string role { get; set; } = "";
//        public string content { get; set; } = "";
//    }

//    public class GrokUsage
//    {
//        public int prompt_tokens { get; set; }
//        public int completion_tokens { get; set; }
//        public int total_tokens { get; set; }
//    }
//}

using Microsoft.AspNetCore.Mvc;
using TaskManager.Models;
using System.Text.Json;

namespace TaskManager.Controllers
{
    public class ChatBotController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        // ✅ VOTRE CLÉ GROQ API
        private const string GROQ_API_KEY = "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";

        public ChatBotController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet]
        public IActionResult Index()
        {
            var model = new ChatBotViewModel
            {
                UserName = "Youssef",
                SessionId = "session-" + DateTime.Now.Ticks,
                IsOnline = true
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> SendMessage([FromBody] ChatRequest request)
        {
            try
            {
                Console.WriteLine($"💬 Message reçu: {request.Message}");

                // Appel direct à Groq AI
                var groqResponse = await CallGroqAPI(request.Message);

                if (!string.IsNullOrEmpty(groqResponse))
                {
                    Console.WriteLine("✅ Groq répond avec succès!");
                    return Ok(new
                    {
                        success = true,
                        message = groqResponse,
                        timestamp = DateTime.Now
                    });
                }
                else
                {
                    Console.WriteLine("❌ Groq a échoué");
                    return Ok(new
                    {
                        success = false,
                        error = "Groq IA temporairement indisponible",
                        timestamp = DateTime.Now
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erreur SendMessage: {ex.Message}");
                return Ok(new
                {
                    success = false,
                    error = "Erreur de connexion à Groq IA",
                    timestamp = DateTime.Now
                });
            }
        }

        [HttpPost]
        [Route("ChatBot/TestMessage")]
        public async Task<IActionResult> TestMessage([FromBody] ChatRequest request)
        {
            return await SendMessage(request);
        }

        [HttpGet]
        public IActionResult GetQuickSuggestions()
        {
            var suggestions = new[]
            {
                "Comment créer une nouvelle tâche efficace ?",
                "Explique-moi les boucles en C#",
                "Conseils pour améliorer ma productivité",
                "Qu'est-ce que ASP.NET Core ?",
                "Comment organiser mon workspace ?",
                "Techniques de debugging efficaces"
            };

            return Ok(new { suggestions });
        }

        [HttpPost]
        public IActionResult ClearChat()
        {
            return Ok(new { success = true });
        }

        // =================== APPEL GROQ API SEULEMENT ===================

        private async Task<string> CallGroqAPI(string userMessage)
        {
            try
            {
                Console.WriteLine("🚀 === APPEL GROQ AI ===");
                Console.WriteLine($"🔑 Clé utilisée: {GROQ_API_KEY.Substring(0, 10)}...");

                var client = _httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(30);

                // Configuration headers
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {GROQ_API_KEY}");
                client.DefaultRequestHeaders.Add("User-Agent", "TaskManager-Assistant/1.0");

                // Modèles Groq dans l'ordre de préférence
                var modelsToTry = new[] {
                    "llama-3.3-70b-versatile",  // Le plus puissant
                    "llama-3.1-70b-versatile",  // Très bon aussi
                    "llama-3.1-8b-instant",     // Ultra rapide
                    "mixtral-8x7b-32768"        // Alternatif
                };

                foreach (var model in modelsToTry)
                {
                    Console.WriteLine($"🤖 Tentative avec modèle: {model}");

                    var requestBody = new
                    {
                        model = model,
                        messages = new[]
                        {
                            new {
                                role = "system",
                                content = @"Tu es un assistant IA intelligent pour TaskManager, développé par Youssef GUINI.

CONTEXTE UTILISATEUR:
- Nom: Youssef GUINI
- Localisation: Casablanca, Maroc
- Projet: TaskManager (ASP.NET Core)
- Niveau: Développeur intermédiaire
- Objectif: Améliorer productivité et skills techniques

INSTRUCTIONS:
- Réponds TOUJOURS en français
- Sois professionnel, encourageant et très utile
- Adapte ton niveau selon la question (débutant à avancé)
- Utilise des emojis appropriés pour rendre tes réponses vivantes
- Donne des exemples concrets avec TaskManager quand c'est pertinent
- Structure tes réponses avec des titres en gras
- Utilise des listes à puces pour la clarté
- Garde tes réponses entre 100-600 mots selon la complexité
- Personnalise pour Youssef quand approprié
- Encourage l'apprentissage et la curiosité
- Si c'est du code, utilise la syntaxe C# par défaut
- Termine par une question ou encouragement quand approprié"
                            },
                            new { role = "user", content = userMessage }
                        },
                        max_tokens = 600,
                        temperature = 0.7
                    };

                    var jsonOptions = new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    };

                    var json = JsonSerializer.Serialize(requestBody, jsonOptions);
                    var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                    Console.WriteLine($"📤 Envoi vers: https://api.groq.com/openai/v1/chat/completions");

                    var response = await client.PostAsync("https://api.groq.com/openai/v1/chat/completions", content);

                    Console.WriteLine($"📡 Status: {response.StatusCode}");

                    if (response.IsSuccessStatusCode)
                    {
                        var responseContent = await response.Content.ReadAsStringAsync();
                        Console.WriteLine($"📥 Réponse reçue: {responseContent.Length} caractères");

                        var groqResponse = JsonSerializer.Deserialize<GroqApiResponse>(responseContent, jsonOptions);
                        var result = groqResponse?.choices?.FirstOrDefault()?.message?.content;

                        if (!string.IsNullOrEmpty(result))
                        {
                            Console.WriteLine($"✅ SUCCÈS avec {model}!");
                            Console.WriteLine($"📝 Contenu: {result.Substring(0, Math.Min(150, result.Length))}...");
                            return result;
                        }
                        else
                        {
                            Console.WriteLine($"⚠️ {model} retourne contenu vide");
                        }
                    }
                    else
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        Console.WriteLine($"❌ {model} échoue: {response.StatusCode}");
                        Console.WriteLine($"❌ Erreur: {errorContent}");

                        // Analyser l'erreur
                        await AnalyzeError(response.StatusCode, errorContent);
                    }
                }

                Console.WriteLine("❌ Tous les modèles Groq ont échoué");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"💥 Exception Groq: {ex.Message}");
                return null;
            }
        }

        private async Task AnalyzeError(System.Net.HttpStatusCode statusCode, string errorContent)
        {
            Console.WriteLine($"\n🔍 === ANALYSE ERREUR {statusCode} ===");

            switch (statusCode)
            {
                case System.Net.HttpStatusCode.Unauthorized:
                    Console.WriteLine("🔒 ERREUR 401 - UNAUTHORIZED");
                    Console.WriteLine("💡 Solutions:");
                    Console.WriteLine("   - Vérifiez votre clé API sur console.groq.com");
                    Console.WriteLine("   - Régénérez une nouvelle clé");
                    Console.WriteLine("   - Vérifiez que la clé commence par 'gsk_'");
                    break;

                case System.Net.HttpStatusCode.BadRequest:
                    Console.WriteLine("📝 ERREUR 400 - BAD REQUEST");
                    Console.WriteLine($"📋 Détail: {errorContent}");
                    Console.WriteLine("💡 Cause probable:");
                    Console.WriteLine("   - Modèle non supporté");
                    Console.WriteLine("   - Format JSON incorrect");
                    break;

                case System.Net.HttpStatusCode.TooManyRequests:
                    Console.WriteLine("🚦 ERREUR 429 - TOO MANY REQUESTS");
                    Console.WriteLine("💡 Solutions:");
                    Console.WriteLine("   - Attendez 1-2 minutes");
                    Console.WriteLine("   - Réduisez la fréquence des appels");
                    break;

                case System.Net.HttpStatusCode.InternalServerError:
                    Console.WriteLine("💥 ERREUR 500 - INTERNAL SERVER ERROR");
                    Console.WriteLine("💡 Serveur Groq temporairement en panne");
                    break;

                default:
                    Console.WriteLine($"❓ ERREUR {(int)statusCode} - {statusCode}");
                    Console.WriteLine($"📋 Détail: {errorContent}");
                    break;
            }
        }
    }

    // =================== CLASSES DE DONNÉES ===================

    public class ChatRequest
    {
        public string Message { get; set; } = "";
        public string SessionId { get; set; } = "";
    }

    public class GroqApiResponse
    {
        public GroqChoice[] choices { get; set; } = Array.Empty<GroqChoice>();
    }

    public class GroqChoice
    {
        public GroqMessage message { get; set; } = new GroqMessage();
    }

    public class GroqMessage
    {
        public string content { get; set; } = "";
    }
}