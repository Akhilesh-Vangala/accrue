namespace Accrue.Application.Notifications;

public sealed record NotificationFeedDto(int UnreadCount, IReadOnlyList<NotificationDto> Items);
