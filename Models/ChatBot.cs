using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TaskManager.Models
{
    // Modèle pour les messages du chat
    public class ChatMessage
    {
        public int Id { get; set; }
        public string Role { get; set; } = ""; // "user" ou "assistant"
        public string Content { get; set; } = "";
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string SessionId { get; set; } = "";
    }

    // Modèle pour les requêtes du chatbot
    public class ChatRequest
    {
        [Required]
        public string Message { get; set; } = "";
        public string SessionId { get; set; } = "";
    }

    // Modèle pour les réponses du chatbot
    public class ChatResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public string Error { get; set; } = "";
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }

    // Modèle pour l'API Grok
    public class GrokApiRequest
    {
        public List<GrokMessage> Messages { get; set; } = new List<GrokMessage>();
        public string Model { get; set; } = "grok-beta";
        public int MaxTokens { get; set; } = 1024;
        public double Temperature { get; set; } = 0.7;
        public bool Stream { get; set; } = false;
    }

    public class GrokMessage
    {
        public string Role { get; set; } = "";
        public string Content { get; set; } = "";
    }

    // Modèle pour la réponse de l'API Grok
    public class GrokApiResponse
    {
        public string Id { get; set; } = "";
        public string Object { get; set; } = "";
        public long Created { get; set; }
        public string Model { get; set; } = "";
        public List<GrokChoice> Choices { get; set; } = new List<GrokChoice>();
        public GrokUsage Usage { get; set; } = new GrokUsage();
    }

    public class GrokChoice
    {
        public int Index { get; set; }
        public GrokMessage Message { get; set; } = new GrokMessage();
        public string FinishReason { get; set; } = "";
    }

    public class GrokUsage
    {
        public int PromptTokens { get; set; }
        public int CompletionTokens { get; set; }
        public int TotalTokens { get; set; }
    }

    // Vue modèle pour la page du chatbot
    public class ChatBotViewModel
    {
        public string UserName { get; set; } = "Utilisateur";
        public string SessionId { get; set; } = "";
        public List<ChatMessage> RecentMessages { get; set; } = new List<ChatMessage>();
        public bool IsOnline { get; set; } = true;
    }
}