using System.ComponentModel.DataAnnotations;

namespace TaskManager.Models
{
    public class ChatMessage
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(450)]
        public string SenderId { get; set; } = "";

        [Required]
        [MaxLength(450)]
        public string ReceiverId { get; set; } = "";

        [Required]
        [MaxLength(1000)]
        public string Message { get; set; } = "";

        [Required]
        public DateTime SentAt { get; set; } = DateTime.Now;

        public bool IsRead { get; set; } = false;

        public bool IsDeleted { get; set; } = false;

        public MessageType MessageType { get; set; } = MessageType.Private;

        // Relations
        public virtual User? Sender { get; set; }
        public virtual User? Receiver { get; set; }
    }

    public enum MessageType
    {
        Private = 1,
        Team = 2,
        System = 3
    }
}