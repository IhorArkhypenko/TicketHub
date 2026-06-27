using MongoDB.Driver;
using Notifications.Application.Abstractions;
using Notifications.Domain;

namespace Notifications.Infrastructure.Persistence;

internal sealed class NotificationRepository : INotificationRepository
{
    private readonly IMongoCollection<Notification> _collection;

    public NotificationRepository(IMongoDatabase database)
        => _collection = database.GetCollection<Notification>("notifications");

    public async Task<bool> AddIfNotExistsAsync(Notification notification, CancellationToken cancellationToken)
    {
        try
        {
            await _collection.InsertOneAsync(notification, cancellationToken: cancellationToken);
            return true;
        }
        catch (MongoWriteException ex) when (ex.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            // A notification for this (booking, type) already exists — redelivery, ignore.
            return false;
        }
    }

    public async Task<IReadOnlyList<Notification>> ListByUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        List<Notification> results = await _collection
            .Find(n => n.UserId == userId)
            .SortByDescending(n => n.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return results;
    }
}
