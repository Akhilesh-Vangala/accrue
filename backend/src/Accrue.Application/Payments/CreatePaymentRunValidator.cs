using FluentValidation;

namespace Accrue.Application.Payments;

public sealed class CreatePaymentRunValidator : AbstractValidator<CreatePaymentRunRequest>
{
    public CreatePaymentRunValidator(TimeProvider time)
    {
        ArgumentNullException.ThrowIfNull(time);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.PaymentDate)
            .Must(d => d >= DateOnly.FromDateTime(time.GetUtcNow().UtcDateTime))
            .WithMessage("Payment date cannot be in the past.");
        RuleFor(x => x.InvoiceIds).NotEmpty().WithMessage("Select at least one approved invoice.");
        RuleFor(x => x.InvoiceIds).Must(ids => ids.Distinct().Count() == ids.Count).WithMessage("Invoice ids must be unique.");
    }
}
