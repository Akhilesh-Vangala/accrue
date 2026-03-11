using FluentValidation;

namespace LedgerFlow.Application.Vendors;

public sealed class SaveVendorValidator : AbstractValidator<SaveVendorRequest>
{
    public static readonly IReadOnlyList<string> Categories =
        ["Software", "Cloud", "Facilities", "Professional Services", "Marketing", "Hardware", "Logistics", "Utilities"];

    public SaveVendorValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.TaxId).NotEmpty().Matches(@"^\d{2}-\d{7}$").WithMessage("Tax ID must look like 12-3456789.");
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Category).NotEmpty().Must(c => Categories.Contains(c)).WithMessage("Unknown category.");
        RuleFor(x => x.PaymentTermsDays).InclusiveBetween(0, 120);
    }
}
