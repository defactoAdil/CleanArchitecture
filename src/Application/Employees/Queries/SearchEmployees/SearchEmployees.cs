using CleanArchitecture.Application.Common.Interfaces;
using CleanArchitecture.Application.Common.Security;
using CleanArchitecture.Domain.Constants;
using CleanArchitecture.Domain.Entities;
using CleanArchitecture.Domain.Enums;

namespace CleanArchitecture.Application.Employees.Queries.SearchEmployees;

[Authorize]
public record SearchEmployeesQuery : IRequest<List<EmployeeDto>>
{
    public EmployeeSearchRequest SearchRequest { get; init; } = new();
}

public class SearchEmployeesQueryValidator : AbstractValidator<SearchEmployeesQuery>
{
    public SearchEmployeesQueryValidator()
    {
        RuleFor(q => q.SearchRequest)
            .Must(HasAtLeastOneCriterion)
            .WithMessage("At least one search criterion must be provided.");
    }

    private static bool HasAtLeastOneCriterion(EmployeeSearchRequest r)
    {
        if (!string.IsNullOrWhiteSpace(r.RegistrationNumber)) return true;
        if (!string.IsNullOrWhiteSpace(r.IdentityNumber)) return true;
        if (!string.IsNullOrWhiteSpace(r.FirstName)) return true;
        if (!string.IsNullOrWhiteSpace(r.LastName)) return true;
        if (r.SourceTypeList is { Count: > 0 }) return true;
        if (r.IsTerminated.HasValue && !string.IsNullOrWhiteSpace(r.ActivePassiveCode)) return true;
        return false;
    }
}

public class SearchEmployeesQueryHandler : IRequestHandler<SearchEmployeesQuery, List<EmployeeDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IUser _user;

    private static readonly string[] AdminOnlySourceTypes =
    [
        SourceType.SAP.ToString(),
        SourceType.OzonTekstil.ToString()
    ];

    public SearchEmployeesQueryHandler(
        IApplicationDbContext context,
        IMapper mapper,
        IUser user)
    {
        _context = context;
        _mapper = mapper;
        _user = user;
    }

    public async Task<List<EmployeeDto>> Handle(SearchEmployeesQuery request, CancellationToken cancellationToken)
    {
        var isAdmin = _user.Roles?.Contains(Roles.HumanResourcesAdminSourceTypes) ?? false;
        var r = request.SearchRequest;

        var query = _context.Employees.AsNoTracking().AsQueryable();

        // Non-admin users cannot see SAP / OzonTekstil employees
        if (!isAdmin)
        {
            query = query.Where(e => !AdminOnlySourceTypes.Contains(e.SourceTypeStr));
        }

        if (!string.IsNullOrWhiteSpace(r.RegistrationNumber))
            query = query.Where(e => e.RegistrationNumber == r.RegistrationNumber);

        if (!string.IsNullOrWhiteSpace(r.IdentityNumber))
            query = query.Where(e => e.IdentityNumber == r.IdentityNumber);

        if (!string.IsNullOrWhiteSpace(r.FirstName))
            query = query.Where(e => e.Firstname.Contains(r.FirstName));

        if (!string.IsNullOrWhiteSpace(r.LastName))
            query = query.Where(e => e.Lastname.Contains(r.LastName));

        if (r.SourceTypeList is { Count: > 0 })
        {
            // Restrict non-admin users to non-admin source types in the filter
            var allowedSourceTypes = isAdmin
                ? r.SourceTypeList
                : r.SourceTypeList.Where(st => !AdminOnlySourceTypes.Contains(st)).ToList();

            if (allowedSourceTypes.Count > 0)
                query = query.Where(e => allowedSourceTypes.Contains(e.SourceTypeStr));
        }

        if (!string.IsNullOrWhiteSpace(r.ActivePassiveCode))
            query = query.Where(e => e.ActivePassiveCode == r.ActivePassiveCode);

        if (r.IsTerminated.HasValue)
            query = query.Where(e => e.IsTerminated == r.IsTerminated.Value);

        var employees = await query
            .OrderBy(e => e.RegistrationNumber)
            .ToListAsync(cancellationToken);

        var dtos = _mapper.Map<List<EmployeeDto>>(employees);

        foreach (var dto in dtos)
        {
            dto.CanEdit = isAdmin || !AdminOnlySourceTypes.Contains(dto.SourceTypeStr);
        }

        return dtos;
    }
}
