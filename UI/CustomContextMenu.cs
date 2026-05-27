using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

namespace ModFramework.UI
{
    public class ContextMenuElement : MonoBehaviour, IPointerExitHandler, IPointerEnterHandler
    {
        private RectTransform _rect;
        private bool _isHovered = false;

        private void Awake()
        {
            _rect = GetComponent<RectTransform>();
            
            // Hide by default
            gameObject.SetActive(false);
        }

        private void Start()
        {
        }

        private void Update()
        {
            // Close if user clicks somewhere else
            if (gameObject.activeSelf && Input.GetMouseButtonDown(0) && !_isHovered)
            {
                Close();
            }
        }

        public void OpenAtCursor()
        {
            if (WindowManager.Instance != null && WindowManager.Instance.Canvas != null && transform.parent != WindowManager.Instance.Canvas.transform)
            {
                transform.SetParent(WindowManager.Instance.Canvas.transform, false);
            }
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            
            // Position at cursor
            Vector2 mousePos = Input.mousePosition;
            
            // Convert to canvas space
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                (RectTransform)transform.parent, 
                mousePos, 
                null, 
                out Vector2 localPoint
            );

            _rect.anchoredPosition = localPoint;
        }

        public void Close()
        {
            gameObject.SetActive(false);
            _isHovered = false;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _isHovered = true;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _isHovered = false;
        }

        public static void Register()
        {
            CustomUIParser.RegisterCustomElement("contextmenu", (string title) =>
            {
                GameObject root = new GameObject("ContextMenu");
                RectTransform rect = root.AddComponent<RectTransform>();
                
                // Add background panel
                var img = root.AddComponent<Image>();
                img.color = new Color(0.15f, 0.15f, 0.15f, 0.95f); // Dark background
                
                // Add layout
                var vLayout = root.AddComponent<VerticalLayoutGroup>();
                vLayout.childControlHeight = true;
                vLayout.childControlWidth = true;
                vLayout.childForceExpandHeight = false;
                vLayout.childForceExpandWidth = true;
                vLayout.spacing = 2f;
                vLayout.padding = new RectOffset(5, 5, 5, 5);
                
                var fitter = root.AddComponent<ContentSizeFitter>();
                fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

                var menu = root.AddComponent<ContextMenuElement>();
                return new ValueTuple<Component, GameObject>(menu, root);
            });
        }
    }
}
