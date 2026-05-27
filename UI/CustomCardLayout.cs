using System;
using UnityEngine;
using UnityEngine.UI;

namespace ModFramework.UI
{
    public class CardLayoutElement : MonoBehaviour
    {
        public static void Register()
        {
            CustomUIParser.RegisterCustomElement("cardlayout", (string title) =>
            {
                // Clean the innerText
                string cleanTitle = title != null ? title.Trim() : "";
                if (cleanTitle.Contains("\n"))
                {
                    cleanTitle = cleanTitle.Substring(0, cleanTitle.IndexOf('\n')).Trim();
                }

                // Create a Panel
                RectTransform panel = WindowManager.SpawnPanel();
                panel.gameObject.name = "CardLayout";
                
                // Styling
                Image img = panel.GetComponent<Image>();
                if (img != null)
                {
                    img.color = new Color(0.1f, 0.1f, 0.1f, 0.6f); // Dark translucent backing
                }

                // Layout
                var vLayout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
                vLayout.childControlHeight = false;
                vLayout.childControlWidth = true;
                vLayout.childForceExpandHeight = false;
                vLayout.childForceExpandWidth = true;
                vLayout.padding = new RectOffset(15, 15, 15, 15);
                vLayout.spacing = 8;
                
                var fitter = panel.gameObject.AddComponent<ContentSizeFitter>();
                fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                // Optional Title Header
                if (!string.IsNullOrEmpty(cleanTitle))
                {
                    Text header = WindowManager.SpawnLabel();
                    header.transform.SetParent(panel, false);
                    header.text = cleanTitle;
                    header.fontStyle = FontStyle.Bold;
                    header.fontSize = 16;
                    header.color = new Color(0.9f, 0.9f, 0.9f, 1f);
                }

                return new ValueTuple<Component, GameObject>(panel, panel.gameObject);
            });
        }
    }
}
