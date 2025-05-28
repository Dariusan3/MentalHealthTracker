using Microsoft.AspNetCore.Identity;
using System;

namespace MentalHealthTracker.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public DateTime RegisteredDate { get; set; } = DateTime.Now;
        public bool IsSubscribed { get; set; } = false;
        public int MessagesLeftToday { get; set; } = 5;
        public string? StripeCustomerId { get; set; }
        public DateTime? LastMessageResetDate { get; set; } = DateTime.Today;
    }
} 