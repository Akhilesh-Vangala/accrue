using LedgerFlow.Application.Abstractions;
using LedgerFlow.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace LedgerFlow.Infrastructure.Persistence.Interceptors;

/// <summary>Stamps created/updated audit columns on every <see cref="Entity"/> save.</summary>
public sealed class AuditInterceptor(ICurrentUser user, TimeProvider time) : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(eventData);
        Stamp(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        ArgumentNullException.ThrowIfNull(eventData);
        Stamp(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    private void Stamp(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var now = time.GetUtcNow();
        foreach (var entry in context.ChangeTracker.Entries<Entity>())
        {
            if (entry.State == EntityState.Added)
            {
                if (entry.Entity.CreatedAt == default)
                {
                    entry.Property(e => e.CreatedAt).CurrentValue = now;
                }

                entry.Property(e => e.CreatedBy).CurrentValue ??= user.Email;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Property(e => e.UpdatedAt).CurrentValue = now;
                entry.Property(e => e.UpdatedBy).CurrentValue = user.Email;
            }
        }
    }
}
