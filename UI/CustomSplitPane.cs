using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace ModFramework.UI
{
    public class SplitPaneElement : MonoBehaviour
    {
        public RectTransform LeftPane;
        public RectTransform RightPane;
        public Image Divider;
        public float DividerWidth = 8f;

        private RectTransform _rect;
        private HorizontalLayoutGroup _layout;

        private void Start()
        {
            _rect = GetComponent<RectTransform>();
            _layout = gameObject.GetComponent<HorizontalLayoutGroup>();
            if (_layout == null) _layout = gameObject.AddComponent<HorizontalLayoutGroup>();
            
            _layout.childControlHeight = true;
            _layout.childControlWidth = false; // We control width manually using LayoutElement
            _layout.childForceExpandHeight = true;
            _layout.childForceExpandWidth = false;
            _layout.spacing = DividerWidth;

            // Wait until end of frame to let XML parser finish adding all children
            Invoke("InitSplitter", 0.05f);
        }

        private void InitSplitter()
        {
            if (transform.childCount >= 2)
            {
                LeftPane = transform.GetChild(0) as RectTransform;
                RightPane = transform.GetChild(1) as RectTransform;
            }

            if (LeftPane != null && RightPane != null)
            {
                // Create Divider
                GameObject divObj = new GameObject("SplitPaneDivider");
                divObj.transform.SetParent(transform, false);
                divObj.transform.SetSiblingIndex(1); // Place between left and right
                
                Divider = divObj.AddComponent<Image>();
                Divider.color = new Color(0.2f, 0.2f, 0.2f, 1f); // Dark handle
                var divLayout = divObj.AddComponent<LayoutElement>();
                divLayout.minWidth = DividerWidth;

                // Add drag events
                var trigger = divObj.AddComponent<EventTrigger>();
                
                EventTrigger.Entry dragEntry = new EventTrigger.Entry();
                dragEntry.eventID = EventTriggerType.Drag;
                dragEntry.callback.AddListener((data) => { OnDrag((PointerEventData)data); });
                trigger.triggers.Add(dragEntry);
                
                // Ensure LayoutElements exist
                var leftLayout = LeftPane.gameObject.GetComponent<LayoutElement>();
                if (leftLayout == null) leftLayout = LeftPane.gameObject.AddComponent<LayoutElement>();
                
                var rightLayout = RightPane.gameObject.GetComponent<LayoutElement>();
                if (rightLayout == null) rightLayout = RightPane.gameObject.AddComponent<LayoutElement>();
                
                // Force right pane to fill remaining space
                rightLayout.flexibleWidth = 1f;
                
                // Apply current width as minWidth for left pane
                if (leftLayout.minWidth <= 0) leftLayout.minWidth = LeftPane.rect.width > 0 ? LeftPane.rect.width : 150f;
            }
        }

        private void OnDrag(PointerEventData eventData)
        {
            if (LeftPane == null) return;
            var leftLayout = LeftPane.GetComponent<LayoutElement>();
            
            // Convert drag delta to local space width changes
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_rect, eventData.position, eventData.pressEventCamera, out Vector2 localMousePos);
            
            // Calculate new width relative to the left edge
            float newWidth = localMousePos.x + (_rect.rect.width / 2f) - (DividerWidth / 2f);
            
            // Clamp to avoid squishing
            newWidth = Mathf.Clamp(newWidth, 50f, _rect.rect.width - 50f);
            leftLayout.minWidth = newWidth;
        }

        public static void Register()
        {
            CustomUIParser.RegisterCustomElement("splitpane", (string title) =>
            {
                GameObject root = new GameObject("SplitPaneRoot");
                RectTransform rect = root.AddComponent<RectTransform>();
                var splitter = root.AddComponent<SplitPaneElement>();
                return new ValueTuple<Component, GameObject>(splitter, root);
            });
        }
    }
}
