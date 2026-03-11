namespace LedgerFlow.Domain.Operations;

/// <summary>In-app notification produced by background jobs and workflow events.</summary>
public sealed class Notification
{
    private Notification()
    {
        Audience = string.Empty;
        Title = string.Empty;
        Body = string.Empty;
    }

    public Notification(string audience, string title, string body, string? link, DateTimeOffset createdAt)
    {
        Audience = audience;
        Title = title;
        Body = body;
        Link = link;
        CreatedAt = createdAt;
    }

    public int Id { get; private set; }

    /// <summary>A role name or a user's email.</summary>
    public string Audience { get; private set; }

    public string Title { get; private set; }

    public string Body { get; private set; }

    public string? Link { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? ReadAt { get; private set; }

    public void MarkRead(DateTimeOffset now) => ReadAt ??= now;
}
