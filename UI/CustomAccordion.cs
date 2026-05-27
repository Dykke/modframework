using System;
using UnityEngine;
using UnityEngine.UI;

namespace ModFramework.UI
{
    public class AccordionElement : MonoBehaviour
    {
        public Button HeaderButton;
        public GameObject ContentPanel;
        public Text HeaderText;

        public bool IsExpanded = true;

        public void Toggle()
        {
            IsExpanded = !IsExpanded;
            if (ContentPanel != null)
            {
                ContentPanel.SetActive(IsExpanded);
            }
        }

        public static void Register()
        {
            CustomUIParser.RegisterCustomElement("accordion", (string title) =>
            {
                // Clean the innerText because XMLParser passes all child text too
                string cleanTitle = title != null ? title.Trim() : "";
                if (cleanTitle.Contains("\n"))
                {
                    cleanTitle = cleanTitle.Substring(0, cleanTitle.IndexOf('\n')).Trim();
                }

                // 1. Root Container (Vertical Layout)
                GameObject root = new GameObject("AccordionRoot");
                var rootRect = root.AddComponent<RectTransform>();
                var vLayout = root.AddComponent<VerticalLayoutGroup>();
                vLayout.childControlHeight = false;
                vLayout.childControlWidth = true;
                vLayout.childForceExpandHeight = false;
                vLayout.childForceExpandWidth = true;
                vLayout.spacing = 2;
                var fitter = root.AddComponent<ContentSizeFitter>();
                fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                // 2. Header Button
                Button headerBtn = WindowManager.SpawnButton();
                headerBtn.transform.SetParent(root.transform, false);
                Text headerText = headerBtn.GetComponentInChildren<Text>();
                headerText.text = "▼ " + cleanTitle;
                headerText.alignment = TextAnchor.MiddleLeft;
                
                var btnLayout = headerBtn.gameObject.AddComponent<LayoutElement>();
                btnLayout.minHeight = 30;

                // 3. Content Panel
                RectTransform contentPanel = WindowManager.SpawnPanel();
                contentPanel.transform.SetParent(root.transform, false);
                var cLayout = contentPanel.gameObject.AddComponent<VerticalLayoutGroup>();
                cLayout.childControlHeight = false;
                cLayout.childControlWidth = true;
                cLayout.childForceExpandHeight = false;
                cLayout.childForceExpandWidth = true;
                cLayout.spacing = 5;
                cLayout.padding = new RectOffset(10, 10, 10, 10);
                var cFitter = contentPanel.gameObject.AddComponent<ContentSizeFitter>();
                cFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                // 4. Accordion Logic Component
                var accordion = root.AddComponent<AccordionElement>();
                accordion.HeaderButton = headerBtn;
                accordion.ContentPanel = contentPanel.gameObject;
                accordion.HeaderText = headerText;

                // Setup toggle callback to change arrow and toggle panel
                headerBtn.onClick.AddListener(() => {
                    accordion.Toggle();
                    headerText.text = (accordion.IsExpanded ? "▼ " : "▶ ") + cleanTitle;
                });

                // Return the Accordion Component, and the ContentPanel as the child container
                // This means any XML tags inside <Accordion> will be parented to contentPanel!
                return new ValueTuple<Component, GameObject>(accordion, contentPanel.gameObject);
            });
        }
    }
}
