using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MentalHealthTracker.Models
{
    public class UserProfile
    {
        [Key]
        public int Id { get; set; }

        public required string UserId { get; set; }

        [ForeignKey("UserId")]
        public ApplicationUser? User { get; set; }

        public required string Nume { get; set; }

        public required string Email { get; set; }

        public DateTime? DataNasterii { get; set; }

        public bool NotificariZilnice { get; set; } = true;

        public bool RapoarteSaptamanale { get; set; } = true;

        public TimeSpan? OraNotificare { get; set; }

        [StringLength(20)]
        public string Tema { get; set; } = "light";

        public DateTime DataCreare { get; set; } = DateTime.UtcNow;

        public DateTime? UltimaActualizare { get; set; }
    }
} 