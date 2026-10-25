using FluentValidation;
using Accrue.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Accrue.Application.Common;

/// <summary>
/// Rejects edits made against a stale copy. The up-front comparison catches conflicts even when the edit changes
/// nothing; the original-value swap makes EF Core's UPDATE fail if another write lands between load and save.
/// </summary>
internal static class ConcurrencyToken
{
    public static void Apply<TEntity>(IAccrueDb db, TEntity entity, string? rowVersion)
        where TEntity : class
    {
        if (string.IsNullOrEmpty(rowVersion))
        {
            return;
        }

        byte[] original;
        try
        {
            original = Convert.FromBase64String(rowVersion);
        }
        catch (FormatException)
        {
            throw new ValidationException("rowVersion is not valid base64.");
        }

        var property = db.Entry(entity).Property<byte[]>("RowVersion");
        if (!original.AsSpan().SequenceEqual(property.CurrentValue))
        {
            throw new DbUpdateConcurrencyException("The record was changed by someone else.");
        }

        property.OriginalValue = original;
    }
}
