using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SubExplore.DataAccess;
using SubExplore.Models.Domain;
using SubExplore.Models.Enums;
using SubExplore.Repositories.Interfaces;

namespace SubExplore.Repositories.Implementations
{
    public class UserRepository : GenericRepository<User>, IUserRepository
    {
        public UserRepository(SubExploreDbContext context) : base(context)
        {
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _context.Users
                .Include(u => u.Preferences)
                .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());
        }

        public async Task<User?> GetByUsernameAsync(string username)
        {
            return await _context.Users
                .Include(u => u.Preferences)
                .FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower());
        }

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            return await _context.Users
                .Include(u => u.Preferences)
                .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());
        }

        public async Task<User?> GetUserByUsernameAsync(string username)
        {
            return await _context.Users
                .Include(u => u.Preferences)
                .FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower());
        }

        public async Task<bool> IsEmailTakenAsync(string email)
        {
            return await _context.Users
                .AnyAsync(u => u.Email.ToLower() == email.ToLower());
        }

        public async Task<bool> IsUsernameTakenAsync(string username)
        {
            return await _context.Users
                .AnyAsync(u => u.Username.ToLower() == username.ToLower());
        }

        public async Task<IEnumerable<User>> GetByAccountTypeAsync(AccountType accountType)
        {
            return await _context.Users
                .Where(u => u.AccountType == accountType)
                .ToListAsync();
        }

        public async Task<IEnumerable<User>> GetTopContributorsAsync(int count = 10)
        {
            return await _context.Users
                .OrderByDescending(u => u.CreatedSpots.Count)
                .Take(count)
                .ToListAsync();
        }

        public async Task<UserPreferences> GetUserPreferencesAsync(int userId)
        {
            return await _context.UserPreferences
                .FirstOrDefaultAsync(p => p.UserId == userId);
        }

        public async Task UpdateUserPreferencesAsync(UserPreferences preferences)
        {
            var existingPreferences = await _context.UserPreferences
                .FirstOrDefaultAsync(p => p.UserId == preferences.UserId);

            if (existingPreferences != null)
            {
                // Mettre à jour les préférences existantes
                _context.Entry(existingPreferences).CurrentValues.SetValues(preferences);
            }
            else
            {
                // Créer de nouvelles préférences
                await _context.UserPreferences.AddAsync(preferences);
            }

            // La sauvegarde sera effectuée par la méthode appelante
            // via SaveChangesAsync()
        }

        public override async Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _context.Users
                .Include(u => u.Preferences)
                .Include(u => u.CreatedSpots)
                .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        }
    }
}