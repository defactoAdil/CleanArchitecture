using CleanArchitecture.Application.Common.Interfaces;
using CleanArchitecture.Application.Common.Security;
using CleanArchitecture.Application.Employees.Common;
using CleanArchitecture.Domain.Constants;
using CleanArchitecture.Domain.Entities;
using CleanArchitecture.Domain.Enums;

namespace CleanArchitecture.Application.Employees.Commands.CreateEmployee;

[Authorize]
public record CreateEmployeeCommand : IRequest<string>
{
    public string IdentityNumber { get; init; } = string.Empty;

    public string Firstname { get; init; } = string.Empty;

    public string Lastname { get; init; } = string.Empty;

    public string? PersonalMobileNumber { get; init; }

    public string SourceTypeStr { get; init; } = string.Empty;

    public string ActivePassiveCode { get; init; } = "1";

    public bool IsTerminated { get; init; }

    public string? CompanyName { get; init; }

    public int? BusinessUnitId { get; init; }

    public string Description { get; init; } = string.Empty;
}

public class CreateEmployeeCommandHandler : IRequestHandler<CreateEmployeeCommand, string>
{
    private readonly IApplicationDbContext _context;
    private readonly IEmployeeRegistrationNumberService _registrationNumberService;
    private readonly IUser _user;

    public CreateEmployeeCommandHandler(
        IApplicationDbContext context,
        IEmployeeRegistrationNumberService registrationNumberService,
        IUser user)
    {
        _context = context;
        _registrationNumberService = registrationNumberService;
        _user = user;
    }

    public async Task<string> Handle(CreateEmployeeCommand request, CancellationToken cancellationToken)
    {
        // Resolve default source type based on role
        var sourceType = string.IsNullOrWhiteSpace(request.SourceTypeStr)
            ? DefaultSourceType()
            : request.SourceTypeStr;

        // BusinessUnitId is only set for admin users
        var isAdmin = _user.Roles?.Contains(Roles.HumanResourcesAdminSourceTypes) ?? false;
        var businessUnitId = isAdmin ? request.BusinessUnitId : null;

        // Wrap number generation + INSERT in one transaction to prevent race conditions.
        // The unique index on RegistrationNumber is the final safety net.
        await using var transaction = await _context.BeginTransactionAsync(cancellationToken);
        try
        {
            var registrationNumber = await _registrationNumberService
                .GenerateNextAsync(sourceType, cancellationToken);

            var employee = new Employee
            {
                RegistrationNumber = registrationNumber,
                IdentityNumber = request.IdentityNumber,
                Firstname = request.Firstname,
                Lastname = request.Lastname,
                PersonalMobileNumber = request.PersonalMobileNumber,
                SourceTypeStr = sourceType,
                ActivePassiveCode = ActivePassiveCodes.Normalize(request.ActivePassiveCode),
                IsTerminated = request.IsTerminated,
                CompanyName = request.CompanyName,
                BusinessUnitId = businessUnitId,
                Description = request.Description
            };

            _context.Employees.Add(employee);
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return registrationNumber;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private string DefaultSourceType() =>
        (_user.Roles?.Contains(Roles.HumanResourcesAdminSourceTypes) ?? false)
            ? SourceType.Ecrou.ToString()
            : SourceType.Other.ToString();
}
