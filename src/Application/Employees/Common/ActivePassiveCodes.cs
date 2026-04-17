namespace CleanArchitecture.Application.Employees.Common;

/// <summary>
/// Centralises ActivePassiveCode normalisation so Create and Update commands share identical logic.
/// "1" or "active" (case-insensitive) → "1" (Etkin/Active); everything else → "0" (Passive).
/// </summary>
public static class ActivePassiveCodes
{
    public const string Active = "1";
    public const string Passive = "0";

    public static string Normalize(string code) =>
        code == Active || code.Equals("active", StringComparison.OrdinalIgnoreCase)
            ? Active
            : Passive;
}
