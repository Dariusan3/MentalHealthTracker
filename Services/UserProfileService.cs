using Microsoft.EntityFrameworkCore;
using MentalHealthTracker.Data;
using MentalHealthTracker.Models;
using System.Threading.Tasks;
using System.Linq;

namespace MentalHealthTracker.Services
{
    public class UserProfileService
    {
        private readonly ApplicationDbContext _context;

        public UserProfileService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<UserProfile?> GetUserProfileAsync(string userId)
        {
            return await _context.UserProfiles
                .FirstOrDefaultAsync(p => p.UserId == userId);
        }

        public async Task<UserProfile> CreateOrUpdateProfileAsync(UserProfile profile)
        {
            var existingProfile = await _context.UserProfiles
                .FirstOrDefaultAsync(p => p.UserId == profile.UserId);

            if (existingProfile == null)
            {
                _context.UserProfiles.Add(profile);
            }
            else
            {
                existingProfile.Nume = profile.Nume;
                existingProfile.Email = profile.Email;
                existingProfile.DataNasterii = profile.DataNasterii;
                existingProfile.NotificariZilnice = profile.NotificariZilnice;
                existingProfile.RapoarteSaptamanale = profile.RapoarteSaptamanale;
                existingProfile.OraNotificare = profile.OraNotificare;
                existingProfile.Tema = profile.Tema;
                existingProfile.UltimaActualizare = DateTime.UtcNow;

                _context.UserProfiles.Update(existingProfile);
            }

            await _context.SaveChangesAsync();
            return profile; // Return the saved profile (either new or updated)
        }
    }
} 