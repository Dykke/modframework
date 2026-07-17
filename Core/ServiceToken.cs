// ServiceToken.cs
// ModFramework v6.0
//
// Ownership token for a registered service object. Replaces the v4.x string
// service-name lookups (which had GameObject.Find name-collision problems).
//
// Closes: AV-4 (service-object spoofing via name collision). A mod can no
// longer do ModServiceBridge.Find("PlayerNameService") and hope it gets the
// right one — it must hold a ServiceToken that was handed out by the publisher.
//
// Full implementation lands in Phase 2 (D5 = M-4). This file is the Phase 0
// skeleton — struct shape and the cross-mod usage pattern.

using System;

namespace ModFramework.Core
{
    /// <summary>
    /// Opaque token for a registered service. Only the mod that called
    /// ModServiceHost.Register holds the ServiceToken for its service; other
    /// mods must request a token from the publisher (e.g. via a static method
    /// on the publisher's mod) to be able to Find/Call the service.
    /// </summary>
    [ModFrameworkPublicAPI("v6.0", Reason = "Replaces string service names; closes AV-4")]
    public readonly struct ServiceToken : IEquatable<ServiceToken>
    {
        private readonly string _serviceName;
        private readonly string _ownerModId;
        private readonly Guid _serviceGuid;

        internal ServiceToken(string serviceName, string ownerModId, Guid serviceGuid)
        {
            this._serviceName = serviceName ?? string.Empty;
            this._ownerModId = ownerModId ?? string.Empty;
            this._serviceGuid = serviceGuid;
        }

        /// <summary>The human-readable service name (e.g. "PlayerNameService").</summary>
        public string ServiceName { get { return this._serviceName; } }

        /// <summary>The mod that owns (registered) this service.</summary>
        public string OwnerModId { get { return this._ownerModId; } }

        public bool Equals(ServiceToken other)
        {
            return this._serviceGuid.Equals(other._serviceGuid)
                && string.Equals(this._ownerModId, other._ownerModId, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is ServiceToken && this.Equals((ServiceToken)obj);
        }

        public override int GetHashCode()
        {
            int hash = 17;
            hash = hash * 31 + (this._serviceName ?? string.Empty).GetHashCode();
            hash = hash * 31 + (this._ownerModId ?? string.Empty).GetHashCode();
            hash = hash * 31 + this._serviceGuid.GetHashCode();
            return hash;
        }

        public static bool operator ==(ServiceToken a, ServiceToken b) { return a.Equals(b); }
        public static bool operator !=(ServiceToken a, ServiceToken b) { return !a.Equals(b); }

        public override string ToString()
        {
            return string.Format("ServiceToken({0}, {1}:{2})", this._serviceName, this._ownerModId, this._serviceGuid);
        }
    }
}
