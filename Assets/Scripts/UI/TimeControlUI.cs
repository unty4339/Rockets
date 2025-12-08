using UnityEngine;
using UnityEngine.UI;
using SpaceLogistics.Core;
using System;

namespace SpaceLogistics.UI
{
    public class TimeControlUI : MonoBehaviour
    {
        public Button PauseButton;
        public Button Speed1xButton;
        public Button Speed100xButton;
        public Button Speed10000xButton;
        public Text TimeDisplay;

        private void Start()
        {
            if (PauseButton) PauseButton.onClick.AddListener(() => SetTimeScale(0));
            if (Speed1xButton) Speed1xButton.onClick.AddListener(() => SetTimeScale(1f));
            if (Speed100xButton) Speed100xButton.onClick.AddListener(() => SetTimeScale(100f));
            if (Speed10000xButton) Speed10000xButton.onClick.AddListener(() => SetTimeScale(10000f));
        }

        private void SetTimeScale(float scale)
        {
            TimeManager.Instance.SetTimeScale(scale);
        }

        private void Update()
        {
            if (TimeDisplay != null)
            {
                var ts = TimeSpan.FromSeconds(TimeManager.Instance.UniverseTime);
                TimeDisplay.text = $"T+{ts.Days}d {ts.Hours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2} (x{TimeManager.Instance.TimeScale})";
            }
        }
        
#if UNITY_EDITOR
        [ContextMenu("Auto Generate UI")]
        public void AutoGenerateUI()
        {
            // Canvas/Panel setup...
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                var go = new GameObject("TimeCanvas");
                canvas = go.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                go.AddComponent<GraphicRaycaster>();
                go.AddComponent<CanvasScaler>();
                transform.SetParent(go.transform, false);
            }
            
            // Create Panel at Top Right
            RectTransform rt = gameObject.GetComponent<RectTransform>();
            if (rt == null) rt = gameObject.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(1, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(1, 1);
            rt.anchoredPosition = new Vector2(-10, -10);
            rt.sizeDelta = new Vector2(300, 60);
            
            // Layout
            HorizontalLayoutGroup hlg = gameObject.GetComponent<HorizontalLayoutGroup>();
            if (hlg == null) hlg = gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.childControlWidth = false;
            hlg.childForceExpandWidth = false;
            hlg.spacing = 5;

            // Buttons
            PauseButton = CreateButton("||", "Pause");
            Speed1xButton = CreateButton(">", "x1");
            Speed100xButton = CreateButton(">>", "x100");
            Speed10000xButton = CreateButton(">>>", "x10000"); // 1 day ~ 8 sec
            
            // Text
            var txtObj = new GameObject("TimeText");
            txtObj.transform.SetParent(transform, false);
            TimeDisplay = txtObj.AddComponent<Text>();
            TimeDisplay.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            TimeDisplay.color = Color.white;
            TimeDisplay.resizeTextForBestFit = true;
            TimeDisplay.text = "T+00:00:00";
            txtObj.GetComponent<RectTransform>().sizeDelta = new Vector2(100, 40);
        }

        private Button CreateButton(string label, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var img = go.AddComponent<Image>();
            img.color = Color.gray;
            var btn = go.AddComponent<Button>();
            
            var txtObj = new GameObject("Text");
            txtObj.transform.SetParent(go.transform, false);
            var txt = txtObj.AddComponent<Text>();
            txt.text = label;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.color = Color.black;
            txt.alignment = TextAnchor.MiddleCenter;
            
            // Fix RectTransform to fill parent exactly
            var txtRt = txtObj.GetComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.sizeDelta = Vector2.zero; // Important to reset size
            txtRt.offsetMin = Vector2.zero;
            txtRt.offsetMax = Vector2.zero;
            
            go.GetComponent<RectTransform>().sizeDelta = new Vector2(40, 40);
            txtObj.GetComponent<RectTransform>().anchorMin = Vector2.zero;
            txtObj.GetComponent<RectTransform>().anchorMax = Vector2.one;
            
            return btn;
        }
#endif
    }
}
