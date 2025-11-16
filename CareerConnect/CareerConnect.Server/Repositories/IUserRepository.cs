using CareerConnect.Server.Models;

namespace CareerConnect.Server.Repositories
{
    public interface IUserRepository
    {
        Task<IEnumerable<User>> GetAllAsync();
        Task<User?> GetByIdAsync(int id);
        Task<User?> GetByEmailAsync(string email);
        Task<User?> GetByGoogleIdAsync(string googleId);
        Task<User?> GetByFacebookIdAsync(string facebookId);
        Task<User?> GetByTwitterIdAsync(string twitterId);
        Task<User?> GetByLinkedInIdAsync(string linkedInId);
        Task<User> CreateAsync(User user);
        Task<User> UpdateAsync(User user);
        Task<bool> DeleteAsync(int id);
        Task<bool> EmailExistsAsync(string email);
        Task<bool> SaveChangesAsync();
    }
}
