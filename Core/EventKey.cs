// EventKey.cs
// ModFramework v6.0
//
// Namespaced event identifier (struct, not string) for ModEvents. Replaces
// the v4.x string-based event names that any mod could fire/subscribe to
// with no namespacing.
//
// Closes: AV-3 (event spoofing). A mod can no longer do
// `ModEvents.Trigger("OnGameSaved", ...)` — it must hold the EventKey the
// publisher minted, and only the publisher can fire it.
//
// Full implementation lands in Phase 2 (D4 = M-3). This file is the Phase 0
// skeleton — struct shape and the static "well-known event" catalogue.

using System;

namespace ModFramework.Core
{
    /// <summary>
    /// Opaque token representing a specific event channel. Only the publisher
    /// (the mod that called ModEvents.Publish) holds the EventKey for its
    /// event. Subscribers receive the same key and use it to register callbacks.
    /// </summary>
    [ModFrameworkPublicAPI("v6.0", Reason = "Replaces string event names; closes AV-3")]
    public readonly struct EventKey : IEquatable<EventKey>
    {
        // Internal: the mod-id that owns this event + a per-event GUID.
        // We don't expose the GUID because that's an opaque reference.
        private readonly string _ownerModId;
        private readonly Guid _eventGuid;

        internal EventKey(string ownerModId, Guid eventGuid)
        {
            this._ownerModId = ownerModId ?? string.Empty;
            this._eventGuid = eventGuid;
        }

        /// <summary>The mod that owns (created) this event. Used for audit log + collision detection.</summary>
        public string OwnerModId { get { return this._ownerModId; } }

        public bool Equals(EventKey other)
        {
            return this._eventGuid.Equals(other._eventGuid) && string.Equals(this._ownerModId, other._ownerModId, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is EventKey && this.Equals((EventKey)obj);
        }

        public override int GetHashCode()
        {
            // Combine ownerModId hash + guid hash. Order-independent.
            int hash = 17;
            hash = hash * 31 + (this._ownerModId ?? string.Empty).GetHashCode();
            hash = hash * 31 + this._eventGuid.GetHashCode();
            return hash;
        }

        public static bool operator ==(EventKey a, EventKey b) { return a.Equals(b); }
        public static bool operator !=(EventKey a, EventKey b) { return !a.Equals(b); }

        public override string ToString()
        {
            return string.Format("EventKey({0}:{1})", this._ownerModId, this._eventGuid);
        }
    }
}
