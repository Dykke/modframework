using System;
using UnityEngine;
using UnityEngine.UI;

namespace ModFramework.UI
{
    public class CustomCharts
    {
        public static void Register()
        {
            CustomUIParser.RegisterCustomElement("barchart", (string title) =>
            {
                GameObject root = new GameObject("BarChart");
                var rect = root.AddComponent<RectTransform>();
                rect.anchorMin = new Vector2(0, 1);
                rect.anchorMax = new Vector2(0, 1);
                rect.pivot = new Vector2(0, 1);
                
                // Unity UI native BarChart
                var chart = root.AddComponent<GUIBarChart>();
                
                return new ValueTuple<Component, GameObject>(chart, root);
            });

            CustomUIParser.RegisterCustomElement("piechart", (string title) =>
            {
                GameObject root = new GameObject("PieChart");
                var rect = root.AddComponent<RectTransform>();
                rect.anchorMin = new Vector2(0, 1);
                rect.anchorMax = new Vector2(0, 1);
                rect.pivot = new Vector2(0, 1);
                
                // Unity UI native PieChart
                var chart = root.AddComponent<GUIPieChart>();
                root.AddComponent<ChartClipFixer>();

                // Initialize AnimationCurves to prevent NullReferenceException
                chart.DarkCurve = AnimationCurve.Linear(0f, 1f, 1f, 1f);
                chart.TransparentCurve = AnimationCurve.Linear(0f, 1f, 1f, 1f);

                // Create a generic LabelPrefab so the chart doesn't crash when instantiating labels
                GameObject labelPrefabObj = new GameObject("PieChartLabelPrefab");
                labelPrefabObj.transform.SetParent(root.transform, false);
                labelPrefabObj.SetActive(false); // Keep it disabled as a prefab template
                
                var textObj = labelPrefabObj.AddComponent<Text>();
                textObj.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                textObj.fontSize = 14;
                textObj.color = Color.white;
                textObj.alignment = TextAnchor.MiddleCenter;
                
                chart.LabelPrefab = textObj;
                
                return new ValueTuple<Component, GameObject>(chart, root);
            });

            CustomUIParser.RegisterCustomElement("linechart", (string title) =>
            {
                GameObject root = new GameObject("LineChart");
                var rect = root.AddComponent<RectTransform>();
                rect.anchorMin = new Vector2(0, 1);
                rect.anchorMax = new Vector2(0, 1);
                rect.pivot = new Vector2(0, 1);
                
                // Unity UI native LineChart
                var chart = root.AddComponent<GUILineChart>();
                root.AddComponent<ChartClipFixer>();
                
                return new ValueTuple<Component, GameObject>(chart, root);
            });
        }
    }

    public class ChartClipFixer : MonoBehaviour
    {
        private CanvasRenderer _canvasRenderer;
        private UnityEngine.UI.RectMask2D _mask;

        void Start()
        {
            _canvasRenderer = GetComponent<CanvasRenderer>();
            _mask = GetComponentInParent<UnityEngine.UI.RectMask2D>();
        }

        void Update()
        {
            if (_canvasRenderer != null && _mask != null && _mask.canvasRect != null)
            {
                _canvasRenderer.EnableRectClipping(_mask.canvasRect);
            }
        }
    }
}
