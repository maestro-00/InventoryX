using InventoryX.Application.Services.IServices;
using InventoryX.Domain.Models.Auditing;
using InventoryX.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InventoryX.Infrastructure.Services
{
    public class NotificationService(AppDbContext context) : INotificationService
    {
        public async Task RaiseAsync(
            NotificationType type,
            string consolidationKey,
            string title,
            string? message = null,
            string? userId = null,
            NotificationChannel channel = NotificationChannel.InApp,
            CancellationToken cancellationToken = default)
        {
            var existing = await context.Notifications
                .FirstOrDefaultAsync(n =>
                    n.ConsolidationKey == consolidationKey &&
                    n.Channel == channel &&
                    n.UserId == userId &&
                    n.ResolvedAt == null, cancellationToken);

            if (existing is not null)
            {
                existing.OccurrenceCount++;
                existing.LastRaisedAt = DateTime.UtcNow;
                existing.Title = title;
                existing.Message = message;
                existing.IsRead = false;
            }
            else
            {
                context.Notifications.Add(new Notification
                {
                    Type = type,
                    Channel = channel,
                    ConsolidationKey = consolidationKey,
                    Title = title,
                    Message = message,
                    UserId = userId,
                    LastRaisedAt = DateTime.UtcNow,
                });
            }

            await context.SaveChangesAsync(cancellationToken);
        }

        public async Task ResolveAsync(string consolidationKey, CancellationToken cancellationToken = default)
        {
            var open = await context.Notifications
                .Where(n => n.ConsolidationKey == consolidationKey && n.ResolvedAt == null)
                .ToListAsync(cancellationToken);
            foreach (var notification in open) notification.ResolvedAt = DateTime.UtcNow;
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}
