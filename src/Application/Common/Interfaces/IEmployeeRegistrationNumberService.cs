namespace CleanArchitecture.Application.Common.Interfaces;

/// <summary>
/// Generates unique, race-condition-safe employee registration numbers per source type.
/// </summary>
public interface IEmployeeRegistrationNumberService
{
    /// <summary>
    /// Returns the next registration number for the given source type by atomically
    /// reading the current maximum and incrementing it inside a database transaction.
    /// </summary>
    Task<string> GenerateNextAsync(string sourceType, CancellationToken cancellationToken);

    /// <summary>
    /// Returns a preview of the next registration number without reserving it.
    /// Not safe for concurrent use — use GenerateNextAsync during actual insert.
    /// </summary>
    Task<string> PeekNextAsync(string sourceType, CancellationToken cancellationToken);
}
