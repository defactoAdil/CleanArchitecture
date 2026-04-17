using CleanArchitecture.Application.Common.Interfaces;
using CleanArchitecture.Application.Common.Security;

namespace CleanArchitecture.Application.Employees.Queries.GetNextRegistrationNumber;

[Authorize]
public record GetNextRegistrationNumberQuery(string SourceType) : IRequest<string>;

public class GetNextRegistrationNumberQueryHandler
    : IRequestHandler<GetNextRegistrationNumberQuery, string>
{
    private readonly IEmployeeRegistrationNumberService _registrationNumberService;

    public GetNextRegistrationNumberQueryHandler(
        IEmployeeRegistrationNumberService registrationNumberService)
    {
        _registrationNumberService = registrationNumberService;
    }

    public Task<string> Handle(
        GetNextRegistrationNumberQuery request,
        CancellationToken cancellationToken)
    {
        return _registrationNumberService.PeekNextAsync(request.SourceType, cancellationToken);
    }
}
