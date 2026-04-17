namespace CleanArchitecture.Application.Employees.Commands.UpdateEmployee;

public class UpdateEmployeeCommandValidator : AbstractValidator<UpdateEmployeeCommand>
{
    public UpdateEmployeeCommandValidator()
    {
        RuleFor(c => c.RegistrationNumber)
            .NotEmpty();

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

        RuleFor(c => c.ActivePassiveCode)
            .NotEmpty();

        RuleFor(c => c.SourceTypeStr)
            .NotEmpty();

        RuleFor(c => c.Description)
            .NotEmpty()
            .MinimumLength(20)
            .WithMessage("Description must be at least 20 characters.");

        RuleFor(c => c.CompanyName)
            .NotEmpty();
    }
}
