using CineMatchAPI.Domain.Entities;
using CineMatchAPI.Domain.Interfaces;
using CineMatchAPI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CineMatchAPI.Infrastructure.Repositories;

public class MessageRepository : IMessageRepository
{
    private readonly CineMatchDbContext _context;

    public MessageRepository(CineMatchDbContext context)
    {
        _context = context;
    }

    public async Task<Message?> GetByIdAsync(string id)
    {
        return await _context.Messages
            .Include(m => m.Sender)
            .FirstOrDefaultAsync(m => m.Id == id);
    }

    public async Task<IEnumerable<Message>> GetByMatchIdAsync(string matchId)
    {
        // Return messages in chronological order so chat displays correctly
        return await _context.Messages
            .Include(m => m.Sender)
            .Where(m => m.MatchId == matchId)
            .OrderBy(m => m.SentAt)
            .ToListAsync();
    }

    public async Task<Message> CreateAsync(Message message)
    {
        _context.Messages.Add(message);
        await _context.SaveChangesAsync();
        return message;
    }

    public async Task<Message> UpdateAsync(Message message)
    {
        _context.Entry(message).State = EntityState.Modified;
        await _context.SaveChangesAsync();
        return message;
    }
}