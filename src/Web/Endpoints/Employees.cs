using CleanArchitecture.Application.Employees.Commands.CreateEmployee;
using CleanArchitecture.Application.Employees.Commands.UpdateEmployee;
using CleanArchitecture.Application.Employees.Queries.GetEmployeeLookups;
using CleanArchitecture.Application.Employees.Queries.GetNextRegistrationNumber;
using CleanArchitecture.Application.Employees.Queries.SearchEmployees;
using Microsoft.AspNetCore.Http.HttpResults;

namespace CleanArchitecture.Web.Endpoints;

public class Employees : IEndpointGroup
{
    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.RequireAuthorization();

        groupBuilder.MapPost(SearchEmployees);
        groupBuilder.MapGet(GetEmployee, "{registrationNumber}");
        groupBuilder.MapGet(GetNextRegistrationNumber, "next-registration-number/{sourceType}");
        groupBuilder.MapPost(CreateEmployee, string.Empty);
        groupBuilder.MapPut(UpdateEmployee, "{registrationNumber}");
        groupBuilder.MapGet(GetSourceTypeLookups, "lookup/source-types");
        groupBuilder.MapGet(GetActivePassiveCodeLookups, "lookup/active-passive-codes");
    }

    [EndpointSummary("Search employees")]
    [EndpointDescription("Returns a filtered list of employees matching the search criteria. At least one criterion must be provided.")]
    public static async Task<Ok<List<EmployeeDto>>> SearchEmployees(
        ISender sender, EmployeeSearchRequest request)
    {
        var result = await sender.Send(new SearchEmployeesQuery { SearchRequest = request });
        return TypedResults.Ok(result);
    }

    [EndpointSummary("Get employee by registration number")]
    [EndpointDescription("Returns a single employee identified by their registration number.")]
    public static async Task<Results<Ok<EmployeeDto>, NotFound>> GetEmployee(
        ISender sender, string registrationNumber)
    {
        var result = await sender.Send(new SearchEmployeesQuery
        {
            SearchRequest = new EmployeeSearchRequest { RegistrationNumber = registrationNumber }
        });

        var employee = result.FirstOrDefault();
        return employee is not null
            ? TypedResults.Ok(employee)
            : TypedResults.NotFound();
    }

    [EndpointSummary("Get next registration number")]
    [EndpointDescription("Returns a preview of the next auto-generated registration number for the given source type.")]
    public static async Task<Ok<string>> GetNextRegistrationNumber(
        ISender sender, string sourceType)
    {
        var result = await sender.Send(new GetNextRegistrationNumberQuery(sourceType));
        return TypedResults.Ok(result);
    }

    [EndpointSummary("Create employee")]
    [EndpointDescription("Creates a new employee. RegistrationNumber is auto-generated and cannot be supplied by the caller.")]
    public static async Task<Created<string>> CreateEmployee(
        ISender sender, CreateEmployeeCommand command)
    {
        var registrationNumber = await sender.Send(command);
        return TypedResults.Created($"/api/employees/{registrationNumber}", registrationNumber);
    }

    [EndpointSummary("Update employee")]
    [EndpointDescription("Updates the employee identified by the registration number in the URL. The registration number in the body must match the URL.")]
    public static async Task<Results<NoContent, BadRequest>> UpdateEmployee(
        ISender sender, string registrationNumber, UpdateEmployeeCommand command)
    {
        if (!registrationNumber.Equals(command.RegistrationNumber, StringComparison.OrdinalIgnoreCase))
            return TypedResults.BadRequest();

        await sender.Send(command);
        return TypedResults.NoContent();
    }

    [EndpointSummary("Get source type lookups")]
    [EndpointDescription("Returns the list of available source types filtered by the current user's role.")]
    public static async Task<Ok<List<SourceTypeLookupDto>>> GetSourceTypeLookups(ISender sender)
    {
        var result = await sender.Send(new GetEmployeeLookupsQuery());
        return TypedResults.Ok(result.SourceTypes);
    }

    [EndpointSummary("Get active/passive code lookups")]
    [EndpointDescription("Returns the list of available active/passive codes.")]
    public static async Task<Ok<List<ActivePassiveLookupDto>>> GetActivePassiveCodeLookups(ISender sender)
    {
        var result = await sender.Send(new GetEmployeeLookupsQuery());
        return TypedResults.Ok(result.ActivePassiveCodes);
    }
}
