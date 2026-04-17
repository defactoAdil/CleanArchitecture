using CleanArchitecture.Domain.Entities;

namespace CleanArchitecture.Application.Employees.Queries.SearchEmployees;

public class EmployeeDto
{
    public int Id { get; set; }

    public string RegistrationNumber { get; set; } = string.Empty;

    public string IdentityNumber { get; set; } = string.Empty;

    public string Firstname { get; set; } = string.Empty;

    public string Lastname { get; set; } = string.Empty;

    public string? PersonalMobileNumber { get; set; }

    public string SourceTypeStr { get; set; } = string.Empty;

    public string ActivePassiveCode { get; set; } = string.Empty;

    public string ActivePassiveDefinition { get; set; } = string.Empty;

    public bool IsTerminated { get; set; }

    public string? CompanyName { get; set; }

    public int? BusinessUnitId { get; set; }

    public string? Description { get; set; }

    /// <summary>
    /// Indicates whether the current user is allowed to edit this employee.
    /// SAP and OzonTekstil employees can only be edited by HR Admin users.
    /// </summary>
    public bool CanEdit { get; set; }

    private class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<Employee, EmployeeDto>()
                .ForMember(d => d.ActivePassiveDefinition,
                    opt => opt.MapFrom(s => ResolveDefinition(s.ActivePassiveCode)))
                .ForMember(d => d.CanEdit, opt => opt.Ignore());
        }

        private static string ResolveDefinition(string code) =>
            code == "1" || code.Equals("active", StringComparison.OrdinalIgnoreCase)
                ? "Etkin"
                : "Passive";
    }
}
