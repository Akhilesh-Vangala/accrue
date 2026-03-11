using FluentValidation;

namespace LedgerFlow.Application.Invoices;

public sealed class SaveInvoiceValidator : AbstractValidator<SaveInvoiceRequest>
{
    public SaveInvoiceValidator(TimeProvider time)
    {
        ArgumentNullException.ThrowIfNull(time);
        RuleFor(x => x.VendorId).GreaterThan(0);
        RuleFor(x => x.InvoiceNumber).NotEmpty().MaximumLength(50);
        RuleFor(x => x.InvoiceDate)
            .Must(d => d <= DateOnly.FromDateTime(time.GetUtcNow().UtcDateTime).AddDays(1))
            .WithMessage("Invoice date cannot be in the future.");
        RuleFor(x => x.TaxAmount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Notes).MaximumLength(1000);
        RuleFor(x => x.Lines).NotEmpty().WithMessage("Add at least one line.");
        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.Description).NotEmpty().MaximumLength(300);
            line.RuleFor(l => l.GlAccount).NotEmpty().Matches(@"^\d{4}$").WithMessage("GL account must be 4 digits.");
            line.RuleFor(l => l.Quantity).GreaterThan(0);
            line.RuleFor(l => l.UnitPrice).GreaterThanOrEqualTo(0);
        });
    }
}
