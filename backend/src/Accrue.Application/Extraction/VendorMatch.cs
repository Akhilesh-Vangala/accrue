namespace Accrue.Application.Extraction;

/// <summary>The vendor an extracted invoice was matched to, and how (<c>tax_id</c> or <c>name</c>).</summary>
public sealed record VendorMatch(VendorCandidate Vendor, string MatchedBy);
