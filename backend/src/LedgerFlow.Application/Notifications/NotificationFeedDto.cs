namespace LedgerFlow.Application.Notifications;

public sealed record NotificationFeedDto(int UnreadCount, IReadOnlyList<NotificationDto> Items);
