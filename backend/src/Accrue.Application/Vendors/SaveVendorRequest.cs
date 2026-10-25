namespace Accrue.Application.Vendors;

public sealed record SaveVendorRequest(string Name, string TaxId, string Email, string Category, int PaymentTermsDays, string? RowVersion = null);
