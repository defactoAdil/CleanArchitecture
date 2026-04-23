using CleanArchitecture.Application.Common.Interfaces;
using CleanArchitecture.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CleanArchitecture.Infrastructure.Services;

public class EmployeeRegistrationNumberService : IEmployeeRegistrationNumberService
{
    private readonly ApplicationDbContext _context;

    public EmployeeRegistrationNumberService(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Returns the next registration number for the given source type.
    /// Call this method from within an open transaction (started via
    /// IApplicationDbContext.BeginTransactionAsync) so that the number
    /// computation and the subsequent INSERT are atomic.
    /// </summary>
    public Task<string> GenerateNextAsync(string sourceType, CancellationToken cancellationToken)
        => ComputeNextAsync(sourceType, cancellationToken);

    /// <summary>
    /// Returns a preview of the next registration number without reserving it.
    /// Not safe for concurrent use — use GenerateNextAsync (inside a transaction) during actual insert.
    /// </summary>
    public Task<string> PeekNextAsync(string sourceType, CancellationToken cancellationToken)
        => ComputeNextAsync(sourceType, cancellationToken);

    private async Task<string> ComputeNextAsync(string sourceType, CancellationToken cancellationToken)
    {
        var maxRaw = await _context.Employees
            .AsNoTracking()
            .Where(e => e.SourceTypeStr == sourceType)
            .Select(e => e.RegistrationNumber)
            .MaxAsync(rn => (string?)rn, cancellationToken);

        int next = 1;
        if (maxRaw != null && int.TryParse(maxRaw, out var current))
        {
            next = current + 1;
        }

        return next.ToString("D6");
    }
}
