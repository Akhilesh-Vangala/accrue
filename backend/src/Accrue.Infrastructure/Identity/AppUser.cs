using Microsoft.AspNetCore.Identity;

namespace Accrue.Infrastructure.Identity;

public sealed class AppUser : IdentityUser
{
    public string DisplayName { get; set; } = string.Empty;
}
