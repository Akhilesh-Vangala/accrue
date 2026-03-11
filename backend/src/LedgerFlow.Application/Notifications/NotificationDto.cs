namespace LedgerFlow.Application.Notifications;

public sealed record NotificationDto(int Id, string Title, string Body, string? Link, DateTimeOffset CreatedAt, bool IsRead);
