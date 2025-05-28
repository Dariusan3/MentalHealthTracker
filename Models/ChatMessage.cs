using System;
using System.ComponentModel.DataAnnotations;

public class ChatMessage
{
    [Key]
    public int Id { get; set; }

    [Required]
    public required string UserId { get; set; }

    [Required]
    public required string Role { get; set; } // "user" sau "assistant"

    [Required]
    public required string Content { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
} 