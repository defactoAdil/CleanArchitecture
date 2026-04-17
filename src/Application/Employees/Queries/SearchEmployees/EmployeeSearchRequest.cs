namespace CleanArchitecture.Application.Employees.Queries.SearchEmployees;

public class EmployeeSearchRequest
{
    public string? RegistrationNumber { get; set; }

    public string? IdentityNumber { get; set; }

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public string? ActivePassiveCode { get; set; }

    public bool? IsTerminated { get; set; }

    public List<string>? SourceTypeList { get; set; }
}
