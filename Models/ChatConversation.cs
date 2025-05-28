using System.ComponentModel.DataAnnotations;

namespace MentalHealthTracker.Models
{
    public class ChatConversation
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public string UserId { get; set; }
        
        [Required]
        public string Title { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? LastModifiedAt { get; set; }
        
        public virtual ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
    }
} 