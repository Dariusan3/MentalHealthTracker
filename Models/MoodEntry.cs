using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MentalHealthTracker.Models
{
    public class MoodEntry
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Data este obligatorie")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        public DateTime Date { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "Nivelul stării de spirit este obligatoriu")]
        [Range(1, 10, ErrorMessage = "Nivelul stării de spirit trebuie să fie între 1 și 10")]
        public int MoodLevel { get; set; } = 5;

        [MaxLength(500, ErrorMessage = "Descrierea nu poate depăși 500 de caractere")]
        public string? Description { get; set; }

        public string? Activities { get; set; }

        public string? Triggers { get; set; }

        [Range(0, 24, ErrorMessage = "Orele de somn trebuie să fie între 0 și 24")]
        public int? SleepHours { get; set; }
        
        // Scorul de sentiment analizat automat (între -1 și 1)
        [Range(-1, 1, ErrorMessage = "Scorul de sentiment trebuie să fie între -1 și 1")]
        public double? SentimentScore { get; set; }

        // Relația cu utilizatorul
        [Required(ErrorMessage = "ID-ul utilizatorului este obligatoriu")]
        public string? UserId { get; set; }
        
        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; set; }
    }
} 