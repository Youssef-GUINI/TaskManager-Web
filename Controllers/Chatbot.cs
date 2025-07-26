// using Microsoft.AspNetCore.Mvc;
// using TaskManager.Models;
// using System.Text.Json;

// namespace TaskManager.Controllers
// {

//     public class ChatBotController : Controller
//     {
//         private readonly IHttpClientFactory _httpClientFactory;

//         // ✅ VOTRE CLÉ GROQ API
//         private const string GROQ_API_KEY = "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";

//         public ChatBotController(IHttpClientFactory httpClientFactory)
//         {
//             _httpClientFactory = httpClientFactory;
//         }

//         [HttpGet]
//         public IActionResult Index()
//         {
//             var model = new ChatBotViewModel
//             {
//                 UserName = "Youssef",
//                 SessionId = "session-" + DateTime.Now.Ticks,
//                 IsOnline = true
//             };

//             return View(model);
//         }

//         [HttpPost]
//         public async Task<IActionResult> SendMessage([FromBody] ChatRequest request)
//         {
//             try
//             {
//                 Console.WriteLine($"💬 Message reçu: {request.Message}");

//                 // Appel direct à Groq AI
//                 var groqResponse = await CallGroqAPI(request.Message);

//                 if (!string.IsNullOrEmpty(groqResponse))
//                 {
//                     Console.WriteLine("✅ Groq répond avec succès!");
//                     return Ok(new
//                     {
//                         success = true,
//                         message = groqResponse,
//                         timestamp = DateTime.Now
//                     });
//                 }
//                 else
//                 {
//                     Console.WriteLine("❌ Groq a échoué");
//                     return Ok(new
//                     {
//                         success = false,
//                         error = "Groq IA temporairement indisponible",
//                         timestamp = DateTime.Now
//                     });
//                 }
//             }
//             catch (Exception ex)
//             {
//                 Console.WriteLine($"❌ Erreur SendMessage: {ex.Message}");
//                 return Ok(new
//                 {
//                     success = false,
//                     error = "Erreur de connexion à Groq IA",
//                     timestamp = DateTime.Now
//                 });
//             }
//         }

//         [HttpPost]
//         [Route("ChatBot/TestMessage")]
//         public async Task<IActionResult> TestMessage([FromBody] ChatRequest request)
//         {
//             return await SendMessage(request);
//         }

//         [HttpGet]
//         public IActionResult GetQuickSuggestions()
//         {
//             var suggestions = new[]
//             {
//                 "Comment créer une nouvelle tâche efficace ?",
//                 "Explique-moi les boucles en C#",
//                 "Conseils pour améliorer ma productivité",
//                 "Qu'est-ce que ASP.NET Core ?",
//                 "Comment organiser mon workspace ?",
//                 "Techniques de debugging efficaces"
//             };

//             return Ok(new { suggestions });
//         }

//         [HttpPost]
//         public IActionResult ClearChat()
//         {
//             return Ok(new { success = true });
//         }

//         // =================== APPEL GROQ API SEULEMENT ===================

//         private async Task<string> CallGroqAPI(string userMessage)
//         {
//             try
//             {
//                 Console.WriteLine("🚀 === APPEL GROQ AI ===");
//                 Console.WriteLine($"🔑 Clé utilisée: {GROQ_API_KEY.Substring(0, 10)}...");

//                 var client = _httpClientFactory.CreateClient();
//                 client.Timeout = TimeSpan.FromSeconds(30);

//                 // Configuration headers
//                 client.DefaultRequestHeaders.Clear();
//                 client.DefaultRequestHeaders.Add("Authorization", $"Bearer {GROQ_API_KEY}");
//                 client.DefaultRequestHeaders.Add("User-Agent", "TaskManager-Assistant/1.0");

//                 // Modèles Groq dans l'ordre de préférence
//                 var modelsToTry = new[] {
//                     "llama-3.3-70b-versatile",  // Le plus puissant
//                     "llama-3.1-70b-versatile",  // Très bon aussi
//                     "llama-3.1-8b-instant",     // Ultra rapide
//                     "mixtral-8x7b-32768"        // Alternatif
//                 };

//                 foreach (var model in modelsToTry)
//                 {
//                     Console.WriteLine($"🤖 Tentative avec modèle: {model}");

//                     var requestBody = new
//                     {
//                         model = model,
//                         messages = new[]
//                         {
//                             new {
//                                 role = "system",
//                                 content = @"Tu es un assistant IA intelligent pour TaskManager, développé par Youssef GUINI.

// CONTEXTE UTILISATEUR:
// - Nom: Youssef GUINI
// - Localisation: Casablanca, Maroc
// - Projet: TaskManager (ASP.NET Core)
// - Niveau: Développeur intermédiaire
// - Objectif: Améliorer productivité et skills techniques

// INSTRUCTIONS:
// - Réponds TOUJOURS en français
// - Sois professionnel, encourageant et très utile
// - Adapte ton niveau selon la question (débutant à avancé)
// - Utilise des emojis appropriés pour rendre tes réponses vivantes
// - Donne des exemples concrets avec TaskManager quand c'est pertinent
// - Structure tes réponses avec des titres en gras
// - Utilise des listes à puces pour la clarté
// - Garde tes réponses entre 100-600 mots selon la complexité
// - Personnalise pour Youssef quand approprié
// - Encourage l'apprentissage et la curiosité
// - Si c'est du code, utilise la syntaxe C# par défaut
// - Termine par une question ou encouragement quand approprié"
//                             },
//                             new { role = "user", content = userMessage }
//                         },
//                         max_tokens = 600,
//                         temperature = 0.7
//                     };

//                     var jsonOptions = new JsonSerializerOptions
//                     {
//                         PropertyNamingPolicy = JsonNamingPolicy.CamelCase
//                     };

//                     var json = JsonSerializer.Serialize(requestBody, jsonOptions);
//                     var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

//                     Console.WriteLine($"📤 Envoi vers: https://api.groq.com/openai/v1/chat/completions");

//                     var response = await client.PostAsync("https://api.groq.com/openai/v1/chat/completions", content);

//                     Console.WriteLine($"📡 Status: {response.StatusCode}");

//                     if (response.IsSuccessStatusCode)
//                     {
//                         var responseContent = await response.Content.ReadAsStringAsync();
//                         Console.WriteLine($"📥 Réponse reçue: {responseContent.Length} caractères");

//                         var groqResponse = JsonSerializer.Deserialize<GroqApiResponse>(responseContent, jsonOptions);
//                         var result = groqResponse?.choices?.FirstOrDefault()?.message?.content;

//                         if (!string.IsNullOrEmpty(result))
//                         {
//                             Console.WriteLine($"✅ SUCCÈS avec {model}!");
//                             Console.WriteLine($"📝 Contenu: {result.Substring(0, Math.Min(150, result.Length))}...");
//                             return result;
//                         }
//                         else
//                         {
//                             Console.WriteLine($"⚠️ {model} retourne contenu vide");
//                         }
//                     }
//                     else
//                     {
//                         var errorContent = await response.Content.ReadAsStringAsync();
//                         Console.WriteLine($"❌ {model} échoue: {response.StatusCode}");
//                         Console.WriteLine($"❌ Erreur: {errorContent}");

//                         // Analyser l'erreur
//                         await AnalyzeError(response.StatusCode, errorContent);
//                     }
//                 }

//                 Console.WriteLine("❌ Tous les modèles Groq ont échoué");
//                 return null;
//             }
//             catch (Exception ex)
//             {
//                 Console.WriteLine($"💥 Exception Groq: {ex.Message}");
//                 return null;
//             }
//         }

//         private async Task AnalyzeError(System.Net.HttpStatusCode statusCode, string errorContent)
//         {
//             Console.WriteLine($"\n🔍 === ANALYSE ERREUR {statusCode} ===");

//             switch (statusCode)
//             {
//                 case System.Net.HttpStatusCode.Unauthorized:
//                     Console.WriteLine("🔒 ERREUR 401 - UNAUTHORIZED");
//                     Console.WriteLine("💡 Solutions:");
//                     Console.WriteLine("   - Vérifiez votre clé API sur console.groq.com");
//                     Console.WriteLine("   - Régénérez une nouvelle clé");
//                     Console.WriteLine("   - Vérifiez que la clé commence par 'gsk_'");
//                     break;

//                 case System.Net.HttpStatusCode.BadRequest:
//                     Console.WriteLine("📝 ERREUR 400 - BAD REQUEST");
//                     Console.WriteLine($"📋 Détail: {errorContent}");
//                     Console.WriteLine("💡 Cause probable:");
//                     Console.WriteLine("   - Modèle non supporté");
//                     Console.WriteLine("   - Format JSON incorrect");
//                     break;

//                 case System.Net.HttpStatusCode.TooManyRequests:
//                     Console.WriteLine("🚦 ERREUR 429 - TOO MANY REQUESTS");
//                     Console.WriteLine("💡 Solutions:");
//                     Console.WriteLine("   - Attendez 1-2 minutes");
//                     Console.WriteLine("   - Réduisez la fréquence des appels");
//                     break;

//                 case System.Net.HttpStatusCode.InternalServerError:
//                     Console.WriteLine("💥 ERREUR 500 - INTERNAL SERVER ERROR");
//                     Console.WriteLine("💡 Serveur Groq temporairement en panne");
//                     break;

//                 default:
//                     Console.WriteLine($"❓ ERREUR {(int)statusCode} - {statusCode}");
//                     Console.WriteLine($"📋 Détail: {errorContent}");
//                     break;
//             }
//         }
//     }

//     // =================== CLASSES DE DONNÉES ===================

//     public class ChatRequest
//     {
//         public string Message { get; set; } = "";
//         public string SessionId { get; set; } = "";
//     }

//     public class GroqApiResponse
//     {
//         public GroqChoice[] choices { get; set; } = Array.Empty<GroqChoice>();
//     }

//     public class GroqChoice
//     {
//         public GroqMessage message { get; set; } = new GroqMessage();
//     }

//     public class GroqMessage
//     {
//         public string content { get; set; } = "";
//     }
// }

using Microsoft.AspNetCore.Mvc;
using TaskManager.Models;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace TaskManager.Controllers
{
    public class ChatBotController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly UserManager<User> _userManager;

        // ✅ VOTRE CLÉ GROQ API
        private const string GROQ_API_KEY = "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";

        public ChatBotController(IHttpClientFactory httpClientFactory, UserManager<User> userManager)
        {
            _httpClientFactory = httpClientFactory;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            var firstName = user?.FirstName ?? "Utilisateur";
            
            var model = new ChatBotViewModel
            {
                UserName = firstName,
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
                var user = await _userManager.GetUserAsync(User);
                var firstName = user?.FirstName ?? "Utilisateur";
                
                Console.WriteLine($"💬 Message reçu de {firstName}: {request.Message}");

                // Appel direct à Groq AI
                var groqResponse = await CallGroqAPI(request.Message, firstName);

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

        private async Task<string> CallGroqAPI(string userMessage, string firstName = null)
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
                                content = $@"Tu es un assistant IA intelligent pour TaskManager.

CONTEXTE UTILISATEUR:
- Prénom: {firstName ?? "Utilisateur"}
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
- Personnalise pour {firstName ?? "l'utilisateur"} quand approprié
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