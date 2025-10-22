using CineMatchAPI.Domain.Entities;
using CineMatchAPI.Domain.Interfaces;
using CineMatchAPI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CineMatchAPI.Infrastructure.Repositories;

/// <summary>
/// Implementation of user data access operations using Entity Framework Core.
/// This class translates repository method calls into database queries.
/// </summary>
public class UserRepository : IUserRepository
{
    private readonly CineMatchDbContext _context;

    // The context is injected via constructor dependency injection
    // This follows the Dependency Inversion Principle from SOLID
    public UserRepository(CineMatchDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetByIdAsync(string id)
    {
        // FindAsync is optimized for primary key lookups
        // It returns null if the entity is not found
        return await _context.Users.FindAsync(id);
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        // FirstOrDefaultAsync returns the first matching entity or null
        // We use this instead of SingleOrDefaultAsync because it's faster
        // and we know email is unique from our database constraint
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<IEnumerable<User>> GetAllAsync()
    {
        // ToListAsync executes the query and returns all users as a list
        // For production with many users, you would add pagination here
        return await _context.Users.ToListAsync();
    }

    public async Task<User> CreateAsync(User user)
    {
        // Add the entity to the context's change tracker
        _context.Users.Add(user);
        
        // SaveChangesAsync persists all tracked changes to the database
        // This is where the actual INSERT statement executes
        await _context.SaveChangesAsync();
        
        // Return the user, which now has any database-generated values
        return user;
    }

    public async Task<User> UpdateAsync(User user)
    {
        // Mark the entity as modified
        _context.Entry(user).State = EntityState.Modified;
        
        // Update the timestamp
        user.UpdatedAt = DateTime.UtcNow;
        
        // Persist changes to database
        await _context.SaveChangesAsync();
        
        return user;
    }

    public async Task DeleteAsync(string id)
    {
        // Find the user first
        var user = await GetByIdAsync(id);
        
        if (user != null)
        {
            // Remove from context and save
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> ExistsAsync(string id)
    {
        // AnyAsync is more efficient than counting or fetching the entity
        // It stops as soon as it finds one matching record
        return await _context.Users.AnyAsync(u => u.Id == id);
    }

    public async Task<bool> EmailExistsAsync(string email)
    {
        return await _context.Users.AnyAsync(u => u.Email == email);
    }
}