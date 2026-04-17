using CleanArchitecture.Domain.Enums;

namespace CleanArchitecture.Application.Employees.Commands.CreateEmployee;

public class CreateEmployeeCommandValidator : AbstractValidator<CreateEmployeeCommand>
{
    private static readonly string[] RestrictedSourceTypes =
    [
        SourceType.SAP.ToString(),
        SourceType.OzonTekstil.ToString()
    ];

    public CreateEmployeeCommandValidator()
    {
        RuleFor(c => c.IdentityNumber)
            .NotEmpty()
            .MaximumLength(11)
            .Matches(@"^\d+$").WithMessage("Identity number must contain only digits.");

        RuleFor(c => c.Firstname)
            .NotEmpty()
            .MaximumLength(100)
            .Matches(@"^[\p{L}]+$").WithMessage("First name must contain letters only.");

        RuleFor(c => c.Lastname)
            .NotEmpty()
            .MaximumLength(100)
            .Matches(@"^[\p{L}]+$").WithMessage("Last name must contain letters only.");

        RuleFor(c => c.PersonalMobileNumber)
            .Length(12).WithMessage("Personal mobile number must be exactly 12 characters.")
            .Must(n => n!.StartsWith("90")).WithMessage("Personal mobile number must start with '90'.")
            .When(c => !string.IsNullOrEmpty(c.PersonalMobileNumber));

        RuleFor(c => c.SourceTypeStr)
            .Must(st => !RestrictedSourceTypes.Contains(st))
            .WithMessage("SAP and OzonTekstil source types cannot be selected when creating an employee.")
            .When(c => !string.IsNullOrWhiteSpace(c.SourceTypeStr));

        RuleFor(c => c.Description)
            .NotEmpty();

        // Duplicate registration number prevention is handled atomically in the command
        // handler via IEmployeeRegistrationNumberService and the unique DB index.
    }
}
