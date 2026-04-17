using CleanArchitecture.Application.Common.Interfaces;
using CleanArchitecture.Application.Common.Security;
using CleanArchitecture.Domain.Constants;
using CleanArchitecture.Domain.Enums;

namespace CleanArchitecture.Application.Employees.Commands.UpdateEmployee;

[Authorize]
public record UpdateEmployeeCommand : IRequest
{
    public string RegistrationNumber { get; init; } = string.Empty;

    public string IdentityNumber { get; init; } = string.Empty;

    public string Firstname { get; init; } = string.Empty;

    public string Lastname { get; init; } = string.Empty;

    public string? PersonalMobileNumber { get; init; }

    public string SourceTypeStr { get; init; } = string.Empty;

    public string ActivePassiveCode { get; init; } = string.Empty;

    public bool IsTerminated { get; init; }

    public string CompanyName { get; init; } = string.Empty;

    public int? BusinessUnitId { get; init; }

    public string Description { get; init; } = string.Empty;
}

public class UpdateEmployeeCommandHandler : IRequestHandler<UpdateEmployeeCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    private static readonly string[] RestrictedSourceTypes =
    [
        SourceType.SAP.ToString(),
        SourceType.OzonTekstil.ToString()
    ];

    public UpdateEmployeeCommandHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task Handle(UpdateEmployeeCommand request, CancellationToken cancellationToken)
    {
        var employee = await _context.Employees
            .FirstOrDefaultAsync(e => e.RegistrationNumber == request.RegistrationNumber, cancellationToken);

        Guard.Against.NotFound(request.RegistrationNumber, employee);

        var isAdmin = _user.Roles?.Contains(Roles.HumanResourcesAdminSourceTypes) ?? false;

        // Non-admin users cannot edit SAP or OzonTekstil employees
        if (!isAdmin && RestrictedSourceTypes.Contains(employee.SourceTypeStr))
        {
            throw new UnauthorizedAccessException(
                "You do not have permission to edit SAP or OzonTekstil employees.");
        }

        // SourceType cannot be changed to SAP or OzonTekstil
        var normalizedSourceType = request.SourceTypeStr;

        employee.IdentityNumber = request.IdentityNumber;
        employee.Firstname = request.Firstname;
        employee.Lastname = request.Lastname;
        employee.PersonalMobileNumber = request.PersonalMobileNumber;
        employee.SourceTypeStr = normalizedSourceType;
        employee.ActivePassiveCode = NormalizeActivePassiveCode(request.ActivePassiveCode);
        employee.IsTerminated = request.IsTerminated;
        employee.CompanyName = request.CompanyName;
        employee.Description = request.Description;

        // BusinessUnitId is only updatable by admin
        if (isAdmin)
        {
            employee.BusinessUnitId = request.BusinessUnitId;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    private static string NormalizeActivePassiveCode(string code) =>
        code.Equals("active", StringComparison.OrdinalIgnoreCase) || code == "1" ? "1" : "0";
}
