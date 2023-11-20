#nullable enable

namespace TenantIsolation.Events
{
    /// <summary>
    /// Extension methods for <see cref="TenantEvent"/> providing utility operations for event handling and type checking
    /// </summary>
    public static class TenantEventExtensions
    {
        /// <summary>
        /// Gets a human-readable description of the tenant event including its type, ID, timestamp, and tenant information
        /// </summary>
        /// <param name="event">The tenant event to describe</param>
        /// <returns>A formatted string containing event details</returns>
        /// <exception cref="ArgumentNullException"><paramref name="event"/> is <see langword="null"/></exception>
        public static string GetEventDescription(this TenantEvent @event)
        {
            ArgumentNullException.ThrowIfNull(@event);

            return $"Event [{@event.GetType().Name}] - ID: {@event.EventId}, OccurredAt: {@event.OccurredAt:O}, TenantId: {@event.TenantId}";
        }

        /// <summary>
        /// Determines whether the event is a <see cref="TenantActivatedEvent"/> instance
        /// </summary>
        /// <param name="event">The tenant event to check</param>
        /// <returns><see langword="true"/> if the event is a tenant activation event; otherwise, <see langword="false"/></returns>
        /// <exception cref="ArgumentNullException"><paramref name="event"/> is <see langword="null"/></exception>
        public static bool IsTenantActivatedEvent(this TenantEvent @event) => @event is TenantActivatedEvent;

        /// <summary>
        /// Determines whether the event is a <see cref="TenantSuspendedEvent"/> instance
        /// </summary>
        /// <param name="event">The tenant event to check</param>
        /// <returns><see langword="true"/> if the event is a tenant suspension event; otherwise, <see langword="false"/></returns>
        /// <exception cref="ArgumentNullException"><paramref name="event"/> is <see langword="null"/></exception>
        public static bool IsTenantSuspendedEvent(this TenantEvent @event) => @event is TenantSuspendedEvent;

        /// <summary>
        /// Determines whether the event is a <see cref="TenantDeletedEvent"/> instance
        /// </summary>
        /// <param name="event">The tenant event to check</param>
        /// <returns><see langword="true"/> if the event is a tenant deletion event; otherwise, <see langword="false"/></returns>
        /// <exception cref="ArgumentNullException"><paramref name="event"/> is <see langword="null"/></exception>
        public static bool IsTenantDeletedEvent(this TenantEvent @event) => @event is TenantDeletedEvent;
    }
}
