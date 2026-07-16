// ModServiceBridge.cs
// ModFramework v5.2
//
// Optional cross-mod bridge for DLL / Nexus mods that ship a separate
// "addendum" dependency without a compile-time assembly reference.
//
// PROVIDER (dependency DLL mod) — on OnActivate:
//
//     ModServiceHost.Register("MyModNativeService", gameObject =>
//         gameObject.AddComponent<MyNativeService>());
//
// CONSUMER (main DLL mod) — at use time (NOT cached at Initialize):
//
//     if (ModServiceBridge.IsAvailable("MyModNativeService"))
//         ModServiceBridge.Send("MyModNativeService", "DoWork", args);
//
// Or declare via ModDependency.ServiceObjectName and call
// ModServiceBridge.IsDependencyReady(dep).
//
// This mirrors the LogoScape / LogoScapeDependency pattern but is
// intended for ModFramework DLL mods (Nexus/local), not Workshop CS mods.

using System;
using UnityEngine;

namespace ModFramework.Core
{
    /// <summary>
    /// Helper for dependency DLL mods that expose a well-known Unity service object.
    /// </summary>
    public static class ModServiceHost
    {
        private const string Tag = "[ModServiceHost]";

        /// <summary>
        /// Create (or return) a DontDestroyOnLoad service object with the given name.
        /// Optionally run <paramref name="configure"/> to add components / wire callbacks.
        /// </summary>
        public static GameObject Register(string serviceObjectName, Action<GameObject> configure = null)
        {
            if (string.IsNullOrEmpty(serviceObjectName))
                return null;

            try
            {
                GameObject existing = GameObject.Find(serviceObjectName);
                if (existing != null)
                {
                    if (configure != null)
                        configure(existing);
                    return existing;
                }

                GameObject go = new GameObject(serviceObjectName);
                UnityEngine.Object.DontDestroyOnLoad(go);
                if (configure != null)
                    configure(go);
                return go;
            }
            catch (Exception ex)
            {
                Debug.LogError(Tag + " Register failed for '" + serviceObjectName + "': " + ex.Message);
                return null;
            }
        }

        /// <summary>Destroy a previously registered service object, if present.</summary>
        public static void Unregister(string serviceObjectName)
        {
            if (string.IsNullOrEmpty(serviceObjectName))
                return;
            try
            {
                GameObject existing = GameObject.Find(serviceObjectName);
                if (existing != null)
                    UnityEngine.Object.Destroy(existing);
            }
            catch (Exception ex)
            {
                Debug.LogWarning(Tag + " Unregister failed for '" + serviceObjectName + "': " + ex.Message);
            }
        }
    }

    /// <summary>
    /// Consumer-side helpers for locating and messaging optional service objects.
    /// Always call at use time — do not cache availability across the session
    /// unless you re-check after GameReady or dependency activation.
    /// </summary>
    public static class ModServiceBridge
    {
        private const string Tag = "[ModServiceBridge]";

        /// <summary>True when a live GameObject with this name exists in the scene.</summary>
        public static bool IsAvailable(string serviceObjectName)
        {
            if (string.IsNullOrEmpty(serviceObjectName))
                return false;
            try
            {
                return GameObject.Find(serviceObjectName) != null;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>Find the service object, or null.</summary>
        public static GameObject Find(string serviceObjectName)
        {
            if (string.IsNullOrEmpty(serviceObjectName))
                return null;
            try
            {
                return GameObject.Find(serviceObjectName);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>Send a Unity message to the service object. No-op if missing.</summary>
        public static void Send(string serviceObjectName, string methodName, object argument = null,
            SendMessageOptions options = SendMessageOptions.RequireReceiver)
        {
            GameObject service = Find(serviceObjectName);
            if (service == null || string.IsNullOrEmpty(methodName))
                return;
            try
            {
                if (argument == null)
                    service.SendMessage(methodName, options);
                else
                    service.SendMessage(methodName, argument, options);
            }
            catch (Exception ex)
            {
                Debug.LogWarning(Tag + " Send failed (" + serviceObjectName + "." + methodName + "): " + ex.Message);
            }
        }

        /// <summary>
        /// Live dependency check: service object (if declared) OR standard ModDependencies.IsPresent.
        /// Use this instead of caching a bool at Initialize for optional addendum mods.
        /// </summary>
        public static bool IsDependencyReady(ModController.DLLMod parentMod, ModDependency dep)
        {
            return ModDependencies.IsPresent(parentMod, dep);
        }

        /// <summary>
        /// Subscribe once to GameSettings.GameReady, then invoke callback with the live ready state.
        /// Useful when the addendum DLL activates after your mod's OnActivate.
        /// </summary>
        public static void WhenDependencyReady(ModController.DLLMod parentMod, ModDependency dep, Action<bool> callback)
        {
            if (callback == null)
                return;

            EventHandler handler = null;
            handler = delegate(object sender, EventArgs e)
            {
                try
                {
                    GameSettings.GameReady -= handler;
                }
                catch { }

                bool ready = IsDependencyReady(parentMod, dep);
                try
                {
                    callback(ready);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning(Tag + " WhenDependencyReady callback failed: " + ex.Message);
                }
            };

            try
            {
                GameSettings.GameReady += handler;
            }
            catch (Exception ex)
            {
                Debug.LogWarning(Tag + " Could not subscribe to GameReady: " + ex.Message);
                callback(IsDependencyReady(parentMod, dep));
            }
        }
    }
}
