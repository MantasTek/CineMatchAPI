using CineMatchAPI.Domain.Entities;

namespace CineMatchAPI.Domain.Interfaces;

public interface IMessageRepository
{
    Task<Message?> GetByIdAsync(string id);
    Task<IEnumerable<Message>> GetByMatchIdAsync(string matchId);
    Task<Message> CreateAsync(Message message);
    Task<Message> UpdateAsync(Message message);
}