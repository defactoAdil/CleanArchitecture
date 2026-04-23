using CleanArchitecture.Application.Common.Interfaces;
using CleanArchitecture.Application.Common.Security;
using CleanArchitecture.Domain.Constants;
using CleanArchitecture.Domain.Enums;

namespace CleanArchitecture.Application.Employees.Queries.GetEmployeeLookups;

[Authorize]
public record GetEmployeeLookupsQuery : IRequest<EmployeeLookupsVm>;

public class GetEmployeeLookupsQueryHandler : IRequestHandler<GetEmployeeLookupsQuery, EmployeeLookupsVm>
{
    private readonly IUser _user;

    public GetEmployeeLookupsQueryHandler(IUser user)
    {
        _user = user;
    }

    public Task<EmployeeLookupsVm> Handle(GetEmployeeLookupsQuery request, CancellationToken cancellationToken)
    {
        var isAdmin = _user.Roles?.Contains(Roles.HumanResourcesAdminSourceTypes) ?? false;

        var sourceTypes = isAdmin
            ? new[] { SourceType.SAP, SourceType.OzonTekstil, SourceType.Efruz, SourceType.Ecrou, SourceType.Other, SourceType.All }
            : new[] { SourceType.Other };

        var vm = new EmployeeLookupsVm
        {
            SourceTypes = sourceTypes
                .Select(st => new SourceTypeLookupDto
                {
                    Value = st.ToString(),
                    Label = st.ToString()
                })
                .ToList(),

            ActivePassiveCodes =
            [
                new ActivePassiveLookupDto { Code = "1", Definition = "Etkin" },
                new ActivePassiveLookupDto { Code = "0", Definition = "Passive" }
            ]
        };

        return Task.FromResult(vm);
    }
}
