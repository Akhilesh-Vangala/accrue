namespace Accrue.Api.Controllers;

public sealed record MeDto(string Email, string DisplayName, IReadOnlyList<string> Roles);
