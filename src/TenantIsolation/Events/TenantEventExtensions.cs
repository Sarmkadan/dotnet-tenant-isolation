using System;

namespace TenantIsolation.Events
{
    public static class TenantEventExtensions
    {
        public static string GetEventDescription(this TenantEvent @event)
        {
            return $"EventId: {@event.EventId}, OccurredAt: {@event.OccurredAt}, TenantId: {@event.TenantId}";
        }

        public static bool IsTenantActivatedEvent(this TenantEvent @event)
        {
            return @event is TenantActivatedEvent;
        }

        public static bool IsTenantSuspendedEvent(this TenantEvent @event)
        {
            return @event is TenantSuspendedEvent;
        }

        public static bool IsTenantDeletedEvent(this TenantEvent @event)
        {
            return @event is TenantDeletedEvent;
        }
    }
}
