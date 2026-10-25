namespace Accrue.Application.Notifications;

public sealed record NotificationDto(int Id, string Title, string Body, string? Link, DateTimeOffset CreatedAt, bool IsRead);
