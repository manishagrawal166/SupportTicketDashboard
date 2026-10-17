using SupportDashboard.Core.Models;

namespace SupportDashboard.Core.Interfaces;

public interface IIncidentRepository
{
    Task<IReadOnlyList<Incident>> GetAllAsync(CancellationToken ct = default);
    Task AddAsync(Incident incident, CancellationToken ct = default);
    Task<bool> UpdateCommentAsync(string number, string? comment, CancellationToken ct = default);
}

public interface IRunRepository<TRun> where TRun : class
{
    Task<IReadOnlyList<TRun>> GetAllAsync(CancellationToken ct = default);
}
