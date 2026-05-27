using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace ModFramework.UI
{
    public class NodeGraphElement : MonoBehaviour
    {
        public RectTransform GraphCanvas;
        
        private void Awake()
        {
            GraphCanvas = GetComponent<RectTransform>();
        }

        public static void Register()
        {
            CustomUIParser.RegisterCustomElement("nodegraph", (string title) =>
            {
                // Create a basic scroll view setup for the node graph
                GameObject root = new GameObject("NodeGraphRoot");
                var rect = root.AddComponent<RectTransform>();
                
                // Software Inc native UI system expects Top-Left anchors/pivots by default
                rect.anchorMin = new Vector2(0, 1);
                rect.anchorMax = new Vector2(0, 1);
                rect.pivot = new Vector2(0, 1);
                
                var img = root.AddComponent<Image>();
                img.color = new Color(0.1f, 0.1f, 0.1f, 1f); // Dark background for the graph
                
                // Add masking so nodes don't draw outside
                var mask = root.AddComponent<RectMask2D>();
                
                // Create the inner canvas that can be dragged/zoomed
                GameObject canvasObj = new GameObject("GraphCanvas");
                canvasObj.transform.SetParent(root.transform, false);
                var canvasRect = canvasObj.AddComponent<RectTransform>();
                // Make the canvas huge so it can be panned
                canvasRect.sizeDelta = new Vector2(4000, 4000);
                canvasRect.anchoredPosition = Vector2.zero;
                canvasRect.anchorMin = new Vector2(0.5f, 0.5f);
                canvasRect.anchorMax = new Vector2(0.5f, 0.5f);
                canvasRect.pivot = new Vector2(0.5f, 0.5f);

                // Add ScrollRect to root to allow panning the canvas
                var scroll = root.AddComponent<ScrollRect>();
                scroll.content = canvasRect;
                scroll.horizontal = true;
                scroll.vertical = true;
                scroll.movementType = ScrollRect.MovementType.Clamped;
                scroll.inertia = false;
                scroll.scrollSensitivity = 0f; // Disable native ScrollRect scrolling so we can use wheel for zoom

                // Add custom zoom component
                var zoom = root.AddComponent<NodeGraphZoomer>();
                zoom.targetCanvas = canvasRect;

                var graph = root.AddComponent<NodeGraphElement>();
                graph.GraphCanvas = canvasRect;

                return new ValueTuple<Component, GameObject>(graph, root);
            });
        }
    }

    public class NodeGraphZoomer : MonoBehaviour
    {
        public RectTransform targetCanvas;
        private float _currentZoom = 1f;

        void Update()
        {
            if (targetCanvas == null) return;

            // Only zoom if mouse is over the graph
            if (RectTransformUtility.RectangleContainsScreenPoint(GetComponent<RectTransform>(), Input.mousePosition))
            {
                float scroll = Input.GetAxis("Mouse ScrollWheel");
                if (Mathf.Abs(scroll) > 0.01f)
                {
                    _currentZoom += scroll * 1.5f;
                    _currentZoom = Mathf.Clamp(_currentZoom, 0.2f, 3f);
                    targetCanvas.localScale = new Vector3(_currentZoom, _currentZoom, 1f);
                }
            }
        }
    }
}
