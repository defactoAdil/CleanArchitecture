namespace CleanArchitecture.Domain.Entities;

public class Employee : BaseAuditableEntity
{
    public string RegistrationNumber { get; set; } = string.Empty;

    public string IdentityNumber { get; set; } = string.Empty;

    public string Firstname { get; set; } = string.Empty;

    public string Lastname { get; set; } = string.Empty;

    public string? PersonalMobileNumber { get; set; }

    public string SourceTypeStr { get; set; } = string.Empty;

    /// <summary>
    /// "1" represents Active/Etkin; any other value represents Passive.
    /// </summary>
    public string ActivePassiveCode { get; set; } = "1";

    public bool IsTerminated { get; set; }

    public string? CompanyName { get; set; }

    public int? BusinessUnitId { get; set; }

    public string? Description { get; set; }
}
