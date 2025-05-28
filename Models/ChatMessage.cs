using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MentalHealthTracker.Models
{
    public class ChatMessage
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }

        [Required]
        public string Role { get; set; } // "user" sau "assistant"

        [Required]
        public string Content { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public int? ConversationId { get; set; }

        [ForeignKey("ConversationId")]
        public virtual ChatConversation? Conversation { get; set; }
    }
} 