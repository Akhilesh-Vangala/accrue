namespace LedgerFlow.Application.Extraction;

/// <summary>Expense accounts an invoice line can be coded to, keyed by vendor category.</summary>
public static class ChartOfAccounts
{
    public static readonly IReadOnlyList<GlAccount> Accounts =
    [
        new("1510", "Computer hardware (capitalized)", "Hardware"),
        new("6100", "Software and subscriptions", "Software"),
        new("6110", "Cloud infrastructure", "Cloud"),
        new("6200", "Marketing and advertising", "Marketing"),
        new("6300", "Professional services", "Professional Services"),
        new("6400", "Facilities and office", "Facilities"),
        new("6500", "Freight and logistics", "Logistics"),
        new("6600", "Utilities", "Utilities"),
    ];

    public static bool IsValid(string? code) => code is not null && Accounts.Any(a => a.Code == code);

    public static string? DefaultFor(string? vendorCategory) =>
        Accounts.FirstOrDefault(a => string.Equals(a.Category, vendorCategory, StringComparison.OrdinalIgnoreCase))?.Code;
}
