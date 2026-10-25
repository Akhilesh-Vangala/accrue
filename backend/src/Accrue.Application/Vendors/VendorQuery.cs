namespace Accrue.Application.Vendors;

public sealed record VendorQuery(string? Search = null, bool IncludeInactive = false, int Page = 1, int PageSize = 25);
