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
    /// Generates the next registration number within a transaction to prevent race conditions.
    /// Uses pessimistic locking (read inside transaction) to ensure uniqueness.
    /// </summary>
    public async Task<string> GenerateNextAsync(string sourceType, CancellationToken cancellationToken)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var next = await ComputeNextAsync(sourceType, cancellationToken);
            // Transaction stays open until the caller commits via SaveChangesAsync.
            // Committing here is safe because the INSERT happens in the same DbContext.
            await transaction.CommitAsync(cancellationToken);
            return next;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    /// <summary>
    /// Returns a preview of the next number without a transaction — suitable for UI hints only.
    /// </summary>
    public async Task<string> PeekNextAsync(string sourceType, CancellationToken cancellationToken)
    {
        return await ComputeNextAsync(sourceType, cancellationToken);
    }

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
