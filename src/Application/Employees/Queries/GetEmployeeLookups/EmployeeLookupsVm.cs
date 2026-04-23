namespace CleanArchitecture.Application.Employees.Queries.GetEmployeeLookups;

public class EmployeeLookupsVm
{
    public List<SourceTypeLookupDto> SourceTypes { get; set; } = [];

    public List<ActivePassiveLookupDto> ActivePassiveCodes { get; set; } = [];
}

public class SourceTypeLookupDto
{
    public string Value { get; set; } = string.Empty;

    public string Label { get; set; } = string.Empty;
}

public class ActivePassiveLookupDto
{
    public string Code { get; set; } = string.Empty;

    public string Definition { get; set; } = string.Empty;
}
