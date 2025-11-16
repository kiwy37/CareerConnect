using CareerConnect.Server.Data;
using CareerConnect.Server.Models;
using Microsoft.EntityFrameworkCore;

namespace CareerConnect.Server.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly AppDbContext _context;

        public UserRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<User>> GetAllAsync()
        {
            return await _context.Users
                .Include(u => u.Rol)
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync();
        }

        public async Task<User?> GetByIdAsync(int id)
        {
            return await _context.Users
                .Include(u => u.Rol)
                .FirstOrDefaultAsync(u => u.Id == id);
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _context.Users
                .Include(u => u.Rol)
                .FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<User?> GetByGoogleIdAsync(string googleId)
        {
            return await _context.Users
                .Include(u => u.Rol)
                .FirstOrDefaultAsync(u => u.GoogleId == googleId);
        }

        public async Task<User?> GetByFacebookIdAsync(string facebookId)
        {
            return await _context.Users
                .Include(u => u.Rol)
                .FirstOrDefaultAsync(u => u.FacebookId == facebookId);
        }

        public async Task<User?> GetByTwitterIdAsync(string twitterId)
        {
            return await _context.Users
                .Include(u => u.Rol)
                .FirstOrDefaultAsync(u => u.TwitterId == twitterId);
        }

        public async Task<User?> GetByLinkedInIdAsync(string linkedInId)
        {
            return await _context.Users
                .Include(u => u.Rol)
                .FirstOrDefaultAsync(u => u.LinkedInId == linkedInId);
        }

        public async Task<User> CreateAsync(User user)
        {
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return user;
        }

        public async Task<User> UpdateAsync(User user)
        {
            user.UpdatedAt = DateTime.UtcNow;
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
            return user;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return false;

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> EmailExistsAsync(string email)
        {
            return await _context.Users.AnyAsync(u => u.Email == email);
        }

        public async Task<bool> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync() > 0;
        }
    }
}
