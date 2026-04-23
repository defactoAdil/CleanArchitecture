namespace CleanArchitecture.Application.Common.Interfaces;

/// <summary>
/// Generates unique employee registration numbers per source type.
/// </summary>
public interface IEmployeeRegistrationNumberService
{
    /// <summary>
    /// Returns the next registration number for the given source type.
    /// Must be called from within an open database transaction
    /// (started via IApplicationDbContext.BeginTransactionAsync) so that
    /// the number read and the subsequent INSERT are atomic.
    /// </summary>
    Task<string> GenerateNextAsync(string sourceType, CancellationToken cancellationToken);

    /// <summary>
    /// Returns a preview of the next registration number without reserving it.
    /// Suitable for UI hints only — not safe under concurrent load.
    /// </summary>
    Task<string> PeekNextAsync(string sourceType, CancellationToken cancellationToken);
}
