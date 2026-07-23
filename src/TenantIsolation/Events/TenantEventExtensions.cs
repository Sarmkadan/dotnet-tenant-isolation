#nullable enable

namespace TenantIsolation.Events
{
    /// <summary>
    /// Provides extension methods for <see cref="TenantEvent"/> to facilitate event description generation and type-specific checks.
    /// These utilities are useful for logging, debugging, and routing events based on their type.
    /// </summary>
    public static class TenantEventExtensions
    {
        /// <summary>
        /// Generates a human-readable description of a <see cref="TenantEvent"/> by including its type name, event ID, timestamp in ISO 8601 format, and associated tenant ID.
        /// </summary>
        /// <param name="event">The <see cref="TenantEvent"/> to describe.</param>
        /// <returns>
        /// A formatted string in the format: "Event [Type] - ID: {EventId}, OccurredAt: {Timestamp}, TenantId: {TenantId}".
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="event"/> is <see langword="null"/>.
        /// </exception>
        public static string GetEventDescription(this TenantEvent @event)
        {
            ArgumentNullException.ThrowIfNull(@event);

            return $"Event [{@event.GetType().Name}] - ID: {@event.EventId}, OccurredAt: {@event.OccurredAt:O}, TenantId: {@event.TenantId}";
        }

        /// <summary>
        /// Determines whether the specified <see cref="TenantEvent"/> is of type <see cref="TenantActivatedEvent"/>.
        /// </summary>
        /// <param name="event">The <see cref="TenantEvent"/> to check.</param>
        /// <returns>
        /// <see langword="true"/> if <paramref name="event"/> is a <see cref="TenantActivatedEvent"/>; otherwise, <see langword="false"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="event"/> is <see langword="null"/>.
        /// </exception>
        public static bool IsTenantActivatedEvent(this TenantEvent @event) => @event is TenantActivatedEvent;

        /// <summary>
        /// Determines whether the specified <see cref="TenantEvent"/> is of type <see cref="TenantSuspendedEvent"/>.
        /// </summary>
        /// <param name="event">The <see cref="TenantEvent"/> to check.</param>
        /// <returns>
        /// <see langword="true"/> if <paramref name="event"/> is a <see cref="TenantSuspendedEvent"/>; otherwise, <see langword="false"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="event"/> is <see langword="null"/>.
        /// </exception>
        public static bool IsTenantSuspendedEvent(this TenantEvent @event) => @event is TenantSuspendedEvent;

        /// <summary>
        /// Determines whether the specified <see cref="TenantEvent"/> is of type <see cref="TenantDeactivatedEvent"/>.
        /// </summary>
        /// <param name="event">The <see cref="TenantEvent"/> to check.</param>
        /// <returns>
        /// <see langword="true"/> if <paramref name="event"/> is a <see cref="TenantDeactivatedEvent"/>; otherwise, <see langword="false"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="event"/> is <see langword="null"/>.
        /// </exception>
        public static bool IsTenantDeactivatedEvent(this TenantEvent @event) => @event is TenantDeactivatedEvent;

        /// <summary>
        /// Determines whether the specified <see cref="TenantEvent"/> is of type <see cref="TenantReactivatedEvent"/>.
        /// </summary>
        /// <param name="event">The <see cref="TenantEvent"/> to check.</param>
        /// <returns>
        /// <see langword="true"/> if <paramref name="event"/> is a <see cref="TenantReactivatedEvent"/>; otherwise, <see langword="false"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="event"/> is <see langword="null"/>.
        /// </exception>
        public static bool IsTenantReactivatedEvent(this TenantEvent @event) => @event is TenantReactivatedEvent;

        /// <summary>
        /// Determines whether the specified <see cref="TenantEvent"/> is of type <see cref="TenantDeletedEvent"/>.
        /// </summary>
        /// <param name="event">The <see cref="TenantEvent"/> to check.</param>
        /// <returns>
        /// <see langword="true"/> if <paramref name="event"/> is a <see cref="TenantDeletedEvent"/>; otherwise, <see langword="false"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="event"/> is <see langword="null"/>.
        /// </exception>
        public static bool IsTenantDeletedEvent(this TenantEvent @event) => @event is TenantDeletedEvent;
    }
}
