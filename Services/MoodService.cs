using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MentalHealthTracker.Data;
using MentalHealthTracker.Models;

namespace MentalHealthTracker.Services
{
    public class MoodService
    {
        private readonly ApplicationDbContext _context;

        public MoodService(ApplicationDbContext context)
        {
            _context = context;

            // Adăugăm date de test doar dacă nu există înregistrări
            if (!_context.MoodEntries.Any())
            {
                SeedTestData();
            }
        }

        private void SeedTestData()
        {
            var entries = new List<MoodEntry>
            {
                new MoodEntry
                {
                    Date = DateTime.Today.AddDays(-1),
                    MoodLevel = 7,
                    Description = "Am avut o zi productivă la muncă.",
                    Activities = "Sport, Citit",
                    Triggers = "Niciun factor declanșator",
                    SleepHours = 7
                },
                new MoodEntry
                {
                    Date = DateTime.Today.AddDays(-2),
                    MoodLevel = 5,
                    Description = "O zi normală, fără evenimente deosebite.",
                    Activities = "Plimbare",
                    Triggers = "Stres la muncă",
                    SleepHours = 6
                },
                new MoodEntry
                {
                    Date = DateTime.Today.AddDays(-3),
                    MoodLevel = 3,
                    Description = "Am avut o zi dificilă.",
                    Activities = "Nimic special",
                    Triggers = "Conflict cu un coleg",
                    SleepHours = 5
                }
            };

            _context.MoodEntries.AddRange(entries);
            _context.SaveChanges();
        }

        public async Task<List<MoodEntry>> GetMoodEntriesAsync(string? userId = null)
        {
            var query = _context.MoodEntries.AsQueryable();
            
            if (!string.IsNullOrEmpty(userId))
            {
                query = query.Where(e => e.UserId == userId);
            }
            
            return await query.OrderByDescending(e => e.Date).ToListAsync();
        }

        public async Task<MoodEntry?> GetMoodEntryByIdAsync(int id, string? userId = null)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return await _context.MoodEntries.FindAsync(id);
            }
            
            return await _context.MoodEntries
                .FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);
        }

        public async Task<MoodEntry> AddMoodEntryAsync(MoodEntry entry, string? userId = null)
        {
            if (entry.Date == default)
            {
                entry.Date = DateTime.Today;
            }
            
            if (!string.IsNullOrEmpty(userId))
            {
                entry.UserId = userId;
            }
            
            _context.MoodEntries.Add(entry);
            await _context.SaveChangesAsync();
            return entry;
        }

        public async Task<bool> UpdateMoodEntryAsync(MoodEntry entry, string? userId = null)
        {
            // Verificăm dacă utilizatorul are acces la această înregistrare
            if (!string.IsNullOrEmpty(userId))
            {
                var existingEntry = await _context.MoodEntries
                    .FirstOrDefaultAsync(e => e.Id == entry.Id && e.UserId == userId);
                
                if (existingEntry == null)
                {
                    return false;
                }
            }
            
            _context.Entry(entry).State = EntityState.Modified;
            
            try
            {
                await _context.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.MoodEntries.AnyAsync(e => e.Id == entry.Id))
                {
                    return false;
                }
                throw;
            }
        }

        public async Task<bool> DeleteMoodEntryAsync(int id, string? userId = null)
        {
            MoodEntry? entry;
            
            if (!string.IsNullOrEmpty(userId))
            {
                entry = await _context.MoodEntries
                    .FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);
            }
            else
            {
                entry = await _context.MoodEntries.FindAsync(id);
            }
            
            if (entry == null)
            {
                return false;
            }

            _context.MoodEntries.Remove(entry);
            await _context.SaveChangesAsync();
            return true;
        }
    }
} 