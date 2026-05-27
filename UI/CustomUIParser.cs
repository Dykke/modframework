using System;
using System.Collections.Generic;
using UnityEngine;
using HarmonyLib;

namespace ModFramework
{
    public static class CustomUIParser
    {
        private static Dictionary<string, Func<string, ValueTuple<Component, GameObject>>> _objectTags;
        private static Dictionary<string, Func<GameObject, Component>> _layoutTags;
        private static HashSet<string> _ignoreAttributes;
        private static bool _initialized = false;

        public static void Initialize()
        {
            if (_initialized) return;

            try
            {
                var tm = Traverse.Create(typeof(WindowManager));
                
                _objectTags = tm.Field("_objectTags").GetValue<Dictionary<string, Func<string, ValueTuple<Component, GameObject>>>>();
                _layoutTags = tm.Field("_layoutTags").GetValue<Dictionary<string, Func<GameObject, Component>>>();
                _ignoreAttributes = tm.Field("_ignoreAttributes").GetValue<HashSet<string>>();

                _initialized = true;
            }
            catch (Exception ex)
            {
                Debug.LogError("[ModFramework] Failed to bypass sandbox using Harmony Traverse: " + ex.Message);
            }
        }

        public static void RegisterCustomElement(string tag, Func<string, ValueTuple<Component, GameObject>> factory)
        {
            Initialize();
            if (_objectTags != null)
            {
                _objectTags[tag.ToLower()] = factory;
            }
            else
            {
                Debug.LogError("[ModFramework] Failed to inject custom XML tag: " + tag);
            }
        }

        public static void RegisterCustomLayout(string tag, Func<GameObject, Component> factory)
        {
            Initialize();
            if (_layoutTags != null)
            {
                _layoutTags[tag.ToLower()] = factory;
            }
            else
            {
                Debug.LogError("[ModFramework] Failed to inject custom XML layout tag: " + tag);
            }
        }

        public static void AddIgnoredAttribute(string attributeName)
        {
            Initialize();
            if (_ignoreAttributes != null)
            {
                _ignoreAttributes.Add(attributeName.ToLower());
            }
        }
    }
}
