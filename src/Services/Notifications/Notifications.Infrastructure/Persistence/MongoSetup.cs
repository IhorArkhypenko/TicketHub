using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using Notifications.Domain;

namespace Notifications.Infrastructure.Persistence;

/// <summary>BSON class map and index initialization for the notifications collection.</summary>
internal static class MongoSetup
{
    private static int _mapped;

    public static void RegisterClassMaps()
    {
        if (Interlocked.Exchange(ref _mapped, 1) == 1)
        {
            return;
        }

        BsonClassMap.RegisterClassMap<Notification>(cm =>
        {
            cm.AutoMap();
            cm.SetIgnoreExtraElements(true);
            cm.MapIdMember(n => n.Id).SetSerializer(new GuidSerializer(BsonType.String));
            cm.MapMember(n => n.UserId).SetSerializer(new GuidSerializer(BsonType.String));
            cm.MapMember(n => n.BookingId).SetSerializer(new GuidSerializer(BsonType.String));
            cm.MapMember(n => n.Type).SetSerializer(new EnumSerializer<NotificationType>(BsonType.String));
        });
    }

    public static async Task EnsureIndexesAsync(IMongoDatabase database, CancellationToken cancellationToken)
    {
        IMongoCollection<Notification> collection = database.GetCollection<Notification>("notifications");

        // Business-key idempotency: one notification per (booking, type).
        var unique = new CreateIndexModel<Notification>(
            Builders<Notification>.IndexKeys.Ascending(n => n.BookingId).Ascending(n => n.Type),
            new CreateIndexOptions { Unique = true, Name = "ux_booking_type" });

        var byUser = new CreateIndexModel<Notification>(
            Builders<Notification>.IndexKeys.Ascending(n => n.UserId).Descending(n => n.CreatedAtUtc),
            new CreateIndexOptions { Name = "ix_user_created" });

        await collection.Indexes.CreateManyAsync(new[] { unique, byUser }, cancellationToken);
    }
}
