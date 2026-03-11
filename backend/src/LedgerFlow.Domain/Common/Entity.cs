namespace LedgerFlow.Domain.Common;

/// <summary>Base type for aggregate roots and entities persisted with an identity key and audit stamps.</summary>
public abstract class Entity
{
    public int Id { get; protected set; }

    public DateTimeOffset CreatedAt { get; set; }

    public string? CreatedBy { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public string? UpdatedBy { get; set; }
}
