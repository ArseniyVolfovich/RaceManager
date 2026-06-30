using RaceManager.Domain.Entities;

namespace RaceManager.Application.Interfaces;

public interface ISupportTicketRepository
{
    Task<IReadOnlyList<SupportTicket>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<SupportTicket?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task AddAsync(SupportTicket ticket, CancellationToken cancellationToken = default);
    Task UpdateAsync(SupportTicket ticket, CancellationToken cancellationToken = default);
}
