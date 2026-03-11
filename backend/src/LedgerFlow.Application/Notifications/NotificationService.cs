using LedgerFlow.Application.Abstractions;
using LedgerFlow.Domain.Operations;
using Microsoft.EntityFrameworkCore;

namespace LedgerFlow.Application.Notifications;

public sealed class NotificationService(ILedgerFlowDb db, ICurrentUser user, TimeProvider time)
{
    public async Task<NotificationFeedDto> FeedAsync(CancellationToken ct)
    {
        var mine = Mine().AsNoTracking();
        var unread = await mine.CountAsync(n => n.ReadAt == null, ct);
        var items = await mine
            .OrderByDescending(n => n.CreatedAt).ThenByDescending(n => n.Id)
            .Take(50)
            .Select(n => new NotificationDto(n.Id, n.Title, n.Body, n.Link, n.CreatedAt, n.ReadAt != null))
            .ToListAsync(ct);
        return new NotificationFeedDto(unread, items);
    }

    public async Task MarkReadAsync(int id, CancellationToken ct)
    {
        var notification = await Mine().SingleOrDefaultAsync(n => n.Id == id, ct) ?? throw NotFoundException.For<Notification>(id);
        notification.MarkRead(time.GetUtcNow());
        await db.SaveChangesAsync(ct);
    }

    public async Task MarkAllReadAsync(CancellationToken ct)
    {
        var now = time.GetUtcNow();
        await Mine().Where(n => n.ReadAt == null).ExecuteUpdateAsync(s => s.SetProperty(n => n.ReadAt, now), ct);
    }

    /// <remarks>Notifications addressed to a role are shared by everyone in that role.</remarks>
    private IQueryable<Notification> Mine()
    {
        var audiences = user.Roles.Append(user.Email).ToList();
        return db.Notifications.Where(n => audiences.Contains(n.Audience));
    }
}
