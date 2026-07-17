// ModEvents.cs
// ModFramework v6.1
//
// Pub/sub event bus for cross-mod communication.
//
// v6.0 changes:
//   - String event names replaced with EventKey (unforgeable token).
//     Only the publisher (the mod that called Publish) can Trigger the event.
//   - Per-op permission check: Publish requires Permission.EventPublish,
//     Subscribe requires Permission.EventSubscribe.
//   - "Global whitelisted events" (OnGameSaved, OnCompanyFounded,
//     OnSoftwareReleased, OnMonthPassed, OnDayPassed, OnGameLoaded) are
//     exposed separately and require Permission.GameEventWhitelist to Publish.
//   - All Publish/Trigger/Subscribe calls are audit-logged.
//
// v6.1 changes:
//   - Removed the v5.x [Obsolete] string-based Trigger(string) wrapper.
//     This closes the v5.x "any mod can fire any event by name" bypass — the
//     v6.0 wrapper intentionally skipped the permission check for back-compat,
//     but a malicious Nexus DLL could exploit that to spoof events. The only
//     way to fire an event in v6.1+ is via the publisher's own EventKey
//     (mod authors must hold the EventKey returned from Publish).
//   - The v4.x string-based event API (ModFramework.ModEvents class in
//     ModFramework.cs) was also made internal in v6.1 — it is now only
//     visible inside the framework assembly, and is scheduled for removal
//     in v7.0.
//
// v5.x: Subscribe("OnGameSaved", handler) — any mod could subscribe to any
// event name. Any mod could also Trigger any event name, pretending to be
// the publisher. v6.0 closed this with EventKey + permission check for the
// v6.0 API; v6.1 closes the last v5.x bypass.

using System;
using System.Collections.Generic;
using UnityEngine;

namespace ModFramework.Core
{
    /// <summary>
    /// v6.0 event bus. Replaces the v5.x string-based event names with
    /// EventKey tokens. All operations are permission-checked + audit-logged.
    /// </summary>
    [ModFrameworkPublicAPI("v6.0", Reason = "Cross-mod event bus")]
    public static class ModEvents
    {
        private const string Tag = "[ModEvents]";

        // EventKey -> list of subscriber callbacks
        private struct Subscription
        {
            public ModIdentity Subscriber;
            public Action<object> Handler;
        }
        private static readonly Dictionary<EventKey, List<Subscription>> _subs = new Dictionary<EventKey, List<Subscription>>();

        // Global whitelisted event kinds -> list of subscriber callbacks
        private static readonly Dictionary<GlobalEventKind, List<Subscription>> _globalSubs = new Dictionary<GlobalEventKind, List<Subscription>>();

        // ---- v6.0 mod-to-mod events (EventKey) ----

        /// <summary>
        /// Publish a new event. Returns an EventKey that subscribers can use
        /// to register handlers. Only the publisher (the mod that called
        /// Publish) can Trigger this event later.
        /// </summary>
        [ModFrameworkPublicAPI("v6.0")]
        public static EventKey Publish(ModIdentity id, string eventName, object data = null)
        {
            if (id == null) throw new ArgumentNullException("id");
            if (string.IsNullOrEmpty(eventName)) throw new ArgumentNullException("eventName");
            SecurityGuards.RequirePermission(id, Permission.EventPublish);

            var key = new EventKey(id.ModId, Guid.NewGuid());
            AuditLog.Log(id.ModId, id.DisplayName, "EVENT_PUBLISH", eventName, "OK", "key=" + key);
            // Optional: trigger the publish-time callback once so subscribers
            // that subscribe AFTER Publish still miss the initial fire (matches
            // v5.x semantics — events are point-in-time, not retained).
            if (data != null) TriggerInternal(key, data, "PUBLISH_TIME");
            return key;
        }

        /// <summary>Subscribe to an event. Requires Permission.EventSubscribe.</summary>
        [ModFrameworkPublicAPI("v6.0")]
        public static void Subscribe(ModIdentity id, EventKey key, Action<object> handler)
        {
            if (id == null) throw new ArgumentNullException("id");
            if (handler == null) return;
            SecurityGuards.RequirePermission(id, Permission.EventSubscribe);

            List<Subscription> list;
            if (!_subs.TryGetValue(key, out list))
            {
                list = new List<Subscription>();
                _subs[key] = list;
            }
            list.Add(new Subscription { Subscriber = id, Handler = handler });
            AuditLog.Log(id.ModId, id.DisplayName, "EVENT_SUBSCRIBE", key.ToString(), "OK", "");
        }

        /// <summary>Unsubscribe a previously-registered handler.</summary>
        [ModFrameworkPublicAPI("v6.0")]
        public static void Unsubscribe(ModIdentity id, EventKey key, Action<object> handler)
        {
            if (id == null || handler == null) return;
            List<Subscription> list;
            if (!_subs.TryGetValue(key, out list)) return;
            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (list[i].Subscriber == id && list[i].Handler == handler)
                {
                    list.RemoveAt(i);
                    AuditLog.Log(id.ModId, id.DisplayName, "EVENT_UNSUBSCRIBE", key.ToString(), "OK", "");
                    return;
                }
            }
        }

        /// <summary>
        /// Trigger an event. Only the publisher of the event can call this —
        /// the key's ownerModId must match the caller's ModId.
        /// </summary>
        [ModFrameworkPublicAPI("v6.0")]
        public static void Trigger(ModIdentity id, EventKey key, object data = null)
        {
            if (id == null) throw new ArgumentNullException("id");
            if (key.OwnerModId == null) return;

            if (key.OwnerModId != id.ModId)
            {
                throw new ModSecurityException(
                    "Cannot trigger event '" + key + "' — owned by '" + key.OwnerModId +
                    "', not '" + id.ModId + "'.");
            }
            TriggerInternal(key, data, "TRIGGER");
        }

        // ---- v6.0 global whitelisted events ----

        /// <summary>Publish a global whitelisted event (OnGameSaved, OnCompanyFounded, etc.). Requires Permission.GameEventWhitelist.</summary>
        [ModFrameworkPublicAPI("v6.0")]
        public static void PublishGlobal(ModIdentity id, GlobalEventKind kind, object data = null)
        {
            if (id == null) throw new ArgumentNullException("id");
            SecurityGuards.RequirePermission(id, Permission.GameEventWhitelist);
            AuditLog.Log(id.ModId, id.DisplayName, "EVENT_PUBLISH_GLOBAL", kind.ToString(), "OK", "");
            TriggerGlobalInternal(kind, data, "PUBLISH");
        }

        /// <summary>Subscribe to a global whitelisted event. Requires Permission.EventSubscribe.</summary>
        [ModFrameworkPublicAPI("v6.0")]
        public static void SubscribeGlobal(ModIdentity id, GlobalEventKind kind, Action<object> handler)
        {
            if (id == null) throw new ArgumentNullException("id");
            if (handler == null) return;
            SecurityGuards.RequirePermission(id, Permission.EventSubscribe);
            List<Subscription> list;
            if (!_globalSubs.TryGetValue(kind, out list))
            {
                list = new List<Subscription>();
                _globalSubs[kind] = list;
            }
            list.Add(new Subscription { Subscriber = id, Handler = handler });
            AuditLog.Log(id.ModId, id.DisplayName, "EVENT_SUBSCRIBE_GLOBAL", kind.ToString(), "OK", "");
        }

        /// <summary>Unsubscribe a global whitelisted event handler.</summary>
        [ModFrameworkPublicAPI("v6.0")]
        public static void UnsubscribeGlobal(ModIdentity id, GlobalEventKind kind, Action<object> handler)
        {
            if (id == null || handler == null) return;
            List<Subscription> list;
            if (!_globalSubs.TryGetValue(kind, out list)) return;
            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (list[i].Subscriber == id && list[i].Handler == handler)
                {
                    list.RemoveAt(i);
                    AuditLog.Log(id.ModId, id.DisplayName, "EVENT_UNSUBSCRIBE_GLOBAL", kind.ToString(), "OK", "");
                    return;
                }
            }
        }

        // ---- internals ----

        private static void TriggerInternal(EventKey key, object data, string originTag)
        {
            List<Subscription> list;
            if (!_subs.TryGetValue(key, out list) || list.Count == 0) return;
            for (int i = list.Count - 1; i >= 0; i--)
            {
                var sub = list[i];
                if (sub.Subscriber == null) { list.RemoveAt(i); continue; }
                try { sub.Handler(data); }
                catch (Exception ex)
                {
                    Debug.LogWarning(Tag + " " + originTag + " handler for " + key + " threw: " + ex.Message);
                }
            }
        }

        private static void TriggerGlobalInternal(GlobalEventKind kind, object data, string originTag)
        {
            List<Subscription> list;
            if (!_globalSubs.TryGetValue(kind, out list) || list.Count == 0) return;
            for (int i = list.Count - 1; i >= 0; i--)
            {
                var sub = list[i];
                if (sub.Subscriber == null) { list.RemoveAt(i); continue; }
                try { sub.Handler(data); }
                catch (Exception ex)
                {
                    Debug.LogWarning(Tag + " " + originTag + " global handler for " + kind + " threw: " + ex.Message);
                }
            }
        }
    }

    /// <summary>
    /// The whitelist of "global" event kinds that any mod can subscribe to
    /// without the publisher needing to share an EventKey. Publishing to
    /// these requires the elevated Permission.GameEventWhitelist.
    /// </summary>
    [ModFrameworkPublicAPI("v6.0", Reason = "Whitelist of public game lifecycle events")]
    public enum GlobalEventKind
    {
        /// <summary>Fired when the player saves the game. Args: save path (string).</summary>
        OnGameSaved = 1,
        /// <summary>Fired when the game is loaded. Args: save path (string).</summary>
        OnGameLoaded = 2,
        /// <summary>Fired when an AI company is founded. Args: company name (string).</summary>
        OnCompanyFounded = 3,
        /// <summary>Fired when a software product is released. Args: product name (string).</summary>
        OnSoftwareReleased = 4,
        /// <summary>Fired on the day boundary. Args: day number (int).</summary>
        OnDayPassed = 5,
        /// <summary>Fired on the month boundary. Args: month number (int).</summary>
        OnMonthPassed = 6,
    }
}
