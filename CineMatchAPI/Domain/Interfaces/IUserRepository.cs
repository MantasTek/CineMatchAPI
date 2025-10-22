using CineMatchAPI.Domain.Entities;

namespace CineMatchAPI.Domain.Interfaces;

/// <summary>
/// Defines data access operations for User entities.
/// This interface allows us to test business logic without a real database.
/// </summary>
public interface IUserRepository
{
    // Basic CRUD operations
    Task<User?> GetByIdAsync(string id);
    Task<User?> GetByEmailAsync(string email);
    Task<IEnumerable<User>> GetAllAsync();
    Task<User> CreateAsync(User user);
    Task<User> UpdateAsync(User user);
    Task DeleteAsync(string id);
    
    // User exists check for validation
    Task<bool> ExistsAsync(string id);
    Task<bool> EmailExistsAsync(string email);
}