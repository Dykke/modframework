// ModServiceBridge.cs
// ModFramework v6.1
//
// Optional cross-mod bridge for DLL / Nexus mods that ship a separate
// "addendum" dependency without a compile-time assembly reference.
//
// PROVIDER (dependency DLL mod) — on OnActivate:
//
//     ServiceToken token = ModServiceHost.Register(myIdentity, "MyModNativeService", go => {
//         go.AddComponent<MyNativeService>();
//     });
//     // Optionally publish the token somewhere consumers can find it.
//
// CONSUMER (main DLL mod) — at use time (NOT cached at Initialize):
//
//     if (ModServiceBridge.IsAvailable(myIdentity, token))
//         ModServiceBridge.Send(myIdentity, token, "DoWork", args);
//
// v6.0 changes:
//   - The provider gets back a ServiceToken (unforgeable handle).
//   - The consumer must hold that ServiceToken to Find/Send the service.
//   - The provider's ModIdentity must declare Permission.ServiceRegister.
//   - The consumer's ModIdentity must declare Permission.ServiceConsume.
//   - All operations are audit-logged.
//
// v6.1 changes:
//   - Removed the v5.x [Obsolete] string-name lookups (Register(string),
//     Unregister(string), IsAvailable(string), Find(string), Send(string, ...)).
//     These intentionally skipped the permission check for back-compat, but
//     a malicious Nexus DLL could exploit that to hijack any service
//     GameObject by name. The only way to find/send a service in v6.1+ is
//     via the publisher's ServiceToken (mod authors must hold the token
//     returned from Register).
//
// v5.x: GameObject.Find(string) was used. This had a name-collision
// problem (mod A's "PlayerNameService" could be hijacked by mod B's
// "PlayerNameService"). v6.0's ServiceToken-based lookup cannot be
// hijacked — only the publisher of the token can use it.

using System;
using System.Collections.Generic;
using UnityEngine;

namespace ModFramework.Core
{
    /// <summary>
    /// Provider-side helpers for registering a service object that other mods
    /// can find + call. v6.0 returns a ServiceToken that other mods must
    /// obtain (e.g. via a static method on the provider's mod) to use.
    /// </summary>
    [ModFrameworkPublicAPI("v6.0", Reason = "Service registration API")]
    public static class ModServiceHost
    {
        private const string Tag = "[ModServiceHost]";

        private struct HostEntry
        {
            public ServiceToken Token;
            public GameObject GameObject;
        }

        // ServiceToken -> {GameObject}. Mod identity is in the token itself.
        private static readonly Dictionary<ServiceToken, HostEntry> _hosts =
            new Dictionary<ServiceToken, HostEntry>();

        /// <summary>
        /// Register a service object. Creates a DontDestroyOnLoad GameObject
        /// with the given name and runs the optional configure callback to
        /// add components / wire callbacks. Returns a ServiceToken that other
        /// mods can use to find the service.
        /// </summary>
        /// <exception cref="ModPermissionException">If the calling mod's identity does not have Permission.ServiceRegister.</exception>
        [ModFrameworkPublicAPI("v6.0")]
        public static ServiceToken Register(ModIdentity id, string serviceName, Action<GameObject> configure = null)
        {
            if (id == null) throw new ArgumentNullException("id");
            if (string.IsNullOrEmpty(serviceName)) throw new ArgumentNullException("serviceName");
            SecurityGuards.RequirePermission(id, Permission.ServiceRegister);

            try
            {
                // Check for collision: a service with this name is already registered
                // by another mod. (Even if it has a different ServiceToken, the
                // same GameObject name would GameObject.Find-collide.)
                foreach (var kv in _hosts)
                {
                    if (kv.Value.GameObject != null && kv.Value.GameObject.name == serviceName
                        && kv.Value.Token.OwnerModId != id.ModId)
                    {
                        throw new ModSecurityException(
                            "Service name '" + serviceName + "' is already registered by mod '" +
                            kv.Value.Token.OwnerModId + "'. Refusing to register a duplicate.");
                    }
                }

                // Find or create the GameObject.
                GameObject go = GameObject.Find(serviceName);
                if (go == null)
                {
                    go = new GameObject(serviceName);
                    UnityEngine.Object.DontDestroyOnLoad(go);
                }

                if (configure != null)
                {
                    try { configure(go); }
                    catch (Exception ex)
                    {
                        Debug.LogWarning(Tag + " Configure callback for '" + serviceName + "' threw: " + ex.Message);
                    }
                }

                var token = new ServiceToken(serviceName, id.ModId, Guid.NewGuid());
                _hosts[token] = new HostEntry { Token = token, GameObject = go };

                AuditLog.Log(id.ModId, id.DisplayName, "SERVICE_REGISTER", serviceName, "OK",
                    "token=" + token);
                return token;
            }
            catch (ModSecurityException)
            {
                throw; // re-throw the explicit security violation
            }
            catch (Exception ex)
            {
                AuditLog.Log(id.ModId, id.DisplayName, "SERVICE_REGISTER", serviceName, "ERROR",
                    ex.GetType().Name + ": " + ex.Message);
                Debug.LogError(Tag + " Register failed for '" + serviceName + "': " + ex.Message);
                return default(ServiceToken);
            }
        }

        /// <summary>Destroy a previously registered service, if present.</summary>
        /// <exception cref="ModPermissionException">If the calling mod's identity does not have Permission.ServiceRegister.</exception>
        [ModFrameworkPublicAPI("v6.0")]
        public static void Unregister(ModIdentity id, ServiceToken token)
        {
            if (id == null) throw new ArgumentNullException("id");
            SecurityGuards.RequirePermission(id, Permission.ServiceRegister);

            HostEntry entry;
            if (!_hosts.TryGetValue(token, out entry))
            {
                Debug.LogWarning(Tag + " Token not found: " + token);
                return;
            }

            if (entry.Token.OwnerModId != id.ModId)
            {
                throw new ModSecurityException(
                    "Cannot unregister service '" + token.ServiceName + "' — owned by '" +
                    entry.Token.OwnerModId + "', not '" + id.ModId + "'.");
            }

            try
            {
                if (entry.GameObject != null) UnityEngine.Object.Destroy(entry.GameObject);
                _hosts.Remove(token);
                AuditLog.Log(id.ModId, id.DisplayName, "SERVICE_UNREGISTER", token.ServiceName, "OK", "");
            }
            catch (Exception ex)
            {
                Debug.LogWarning(Tag + " Unregister failed for '" + token.ServiceName + "': " + ex.Message);
            }
        }

        // ==================================================================
        // IsDependencyReady and WhenDependencyReady were removed in v6.0 — they
        // depended on ModDependency (a v5.x type from Core/ModDependencies.cs,
        // not currently compiled). If a mod needs them, re-enable
        // Core/ModDependencies.cs in ModFramework.csproj and add them back as
        // [Obsolete] wrappers. The v6.0 replacement is a per-mod subscription
        // to the GlobalEventKind.OnGameLoaded event.
    }

    /// <summary>
    /// Consumer-side helpers for finding + messaging optional service objects.
    /// In v6.0, the consumer must hold a ServiceToken (obtained from the
    /// provider) to Find/Send the service.
    /// </summary>
    [ModFrameworkPublicAPI("v6.0", Reason = "Service consumption API")]
    public static class ModServiceBridge
    {
        private const string Tag = "[ModServiceBridge]";

        /// <summary>True when a live service object with the given token exists.</summary>
        /// <exception cref="ModPermissionException">If the calling mod's identity does not have Permission.ServiceConsume.</exception>
        [ModFrameworkPublicAPI("v6.0")]
        public static bool IsAvailable(ModIdentity id, ServiceToken token)
        {
            if (id == null) throw new ArgumentNullException("id");
            SecurityGuards.RequirePermission(id, Permission.ServiceConsume);
            if (token.OwnerModId == null) return false;
            var go = Find(id, token);
            return go != null;
        }

        /// <summary>Find the service object via its token, or null if not present.</summary>
        /// <exception cref="ModPermissionException">If the calling mod's identity does not have Permission.ServiceConsume.</exception>
        [ModFrameworkPublicAPI("v6.0")]
        public static GameObject Find(ModIdentity id, ServiceToken token)
        {
            if (id == null) throw new ArgumentNullException("id");
            SecurityGuards.RequirePermission(id, Permission.ServiceConsume);
            try
            {
                return GameObject.Find(token.ServiceName);
            }
            catch { return null; }
        }

        /// <summary>Send a Unity message to the service object identified by the token.</summary>
        /// <exception cref="ModPermissionException">If the calling mod's identity does not have Permission.ServiceConsume.</exception>
        [ModFrameworkPublicAPI("v6.0")]
        public static void Send(ModIdentity id, ServiceToken token, string methodName, object argument = null,
            SendMessageOptions options = SendMessageOptions.RequireReceiver)
        {
            if (id == null) throw new ArgumentNullException("id");
            SecurityGuards.RequirePermission(id, Permission.ServiceConsume);
            GameObject service = Find(id, token);
            if (service == null || string.IsNullOrEmpty(methodName))
            {
                AuditLog.Log(id.ModId, id.DisplayName, "SERVICE_SEND", token.ServiceName + "." + methodName, "MISSING", "");
                return;
            }
            try
            {
                if (argument == null) service.SendMessage(methodName, options);
                else service.SendMessage(methodName, argument, options);
                AuditLog.Log(id.ModId, id.DisplayName, "SERVICE_SEND", token.ServiceName + "." + methodName, "OK", "");
            }
            catch (Exception ex)
            {
                AuditLog.Log(id.ModId, id.DisplayName, "SERVICE_SEND", token.ServiceName + "." + methodName, "ERROR", ex.GetType().Name);
                Debug.LogWarning(Tag + " Send failed (" + token.ServiceName + "." + methodName + "): " + ex.Message);
            }
        }
    }
}
