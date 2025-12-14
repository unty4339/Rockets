using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using SpaceLogistics.Core;
using SpaceLogistics.Missions;
using SpaceLogistics.Space;
using SpaceLogistics.Structures;

namespace SpaceLogistics.UI
{
    /// <summary>
    /// ミッション計画を編集するUIクラス。
    /// タイムライン形式でノードを追加・編集し、自動的にレッグを計算する。
    /// </summary>
    public class MissionPlanEditorUI : MonoBehaviour
    {
        [Header("Data")]
        public MissionPlan CurrentPlan;

        [Header("UI References")]
        public Text PlanNameText;
        public InputField PlanNameInput;
        public Text PlanDescriptionText;
        public InputField PlanDescriptionInput;
        
        // ノードリスト
        public Transform NodeListContainer; // ノード表示の親
        public GameObject NodeItemPrefab; // ノード項目のプレハブ
        
        // ノード編集ダイアログ
        public MissionNodeEditDialog NodeEditDialog;
        
        // 統計表示
        public Text StatsText; // 総ΔV、総時間などを表示
        
        // ボタン
        public Button AddNodeButton;
        public Button CalculateLegsButton;
        public Button SavePlanButton;
        public Button LoadPlanButton;
        public Button CloseButton;

        private List<MissionNode> _tempNodes = new List<MissionNode>();

        private void Start()
        {
            InitializePlan();
            SetupUI();
            UpdateDisplay();
            
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnStateChanged += HandleStateChanged;
                HandleStateChanged(GameManager.Instance.CurrentState);
            }
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnStateChanged -= HandleStateChanged;
            }
        }

        private void HandleStateChanged(GameState newState)
        {
            // エディタモードの時のみ表示（RocketEditorと同様の動作）
            bool isEditor = (newState == GameState.RocketEditor);
            
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                canvas.enabled = isEditor;
                var raycaster = canvas.GetComponent<GraphicRaycaster>();
                if (raycaster != null) raycaster.enabled = isEditor;
            }
            else
            {
                gameObject.SetActive(isEditor);
            }
        }

        private void InitializePlan()
        {
            CurrentPlan = new MissionPlan();
            CurrentPlan.PlanName = "New Mission Plan";
            CurrentPlan.Description = "";
            _tempNodes.Clear();
        }

        private void SetupUI()
        {
            if (AddNodeButton != null)
                AddNodeButton.onClick.AddListener(OnAddNodeClicked);
            
            if (CalculateLegsButton != null)
                CalculateLegsButton.onClick.AddListener(OnCalculateLegsClicked);
            
            if (SavePlanButton != null)
                SavePlanButton.onClick.AddListener(OnSavePlanClicked);
            
            if (LoadPlanButton != null)
                LoadPlanButton.onClick.AddListener(OnLoadPlanClicked);
            
            if (CloseButton != null)
                CloseButton.onClick.AddListener(OnCloseClicked);

            if (PlanNameInput != null)
            {
                PlanNameInput.onEndEdit.AddListener((value) => 
                {
                    if (CurrentPlan != null) CurrentPlan.PlanName = value;
                    UpdateDisplay();
                });
            }

            if (PlanDescriptionInput != null)
            {
                PlanDescriptionInput.onEndEdit.AddListener((value) => 
                {
                    if (CurrentPlan != null) CurrentPlan.Description = value;
                });
            }
        }

        private void UpdateDisplay()
        {
            UpdatePlanInfo();
            UpdateNodeList();
            UpdateStats();
        }

        private void UpdatePlanInfo()
        {
            if (PlanNameText != null && CurrentPlan != null)
                PlanNameText.text = CurrentPlan.PlanName;
            
            if (PlanNameInput != null && CurrentPlan != null)
                PlanNameInput.text = CurrentPlan.PlanName;
            
            if (PlanDescriptionText != null && CurrentPlan != null)
                PlanDescriptionText.text = CurrentPlan.Description;
            
            if (PlanDescriptionInput != null && CurrentPlan != null)
                PlanDescriptionInput.text = CurrentPlan.Description;
        }

        private void UpdateNodeList()
        {
            if (NodeListContainer == null) return;

            // 既存のノード項目を削除
            foreach (Transform child in NodeListContainer)
            {
                Destroy(child.gameObject);
            }

            // 現在のノードを表示（一時ノードリストまたはプランのノードリスト）
            List<MissionNode> nodesToShow = CurrentPlan != null && CurrentPlan.Nodes.Count > 0 
                ? CurrentPlan.Nodes 
                : _tempNodes;

            for (int i = 0; i < nodesToShow.Count; i++)
            {
                var node = nodesToShow[i];
                CreateNodeItem(node, i);
            }
        }

        private void CreateNodeItem(MissionNode node, int index)
        {
            if (NodeItemPrefab == null) return;

            GameObject itemObj = Instantiate(NodeItemPrefab, NodeListContainer);
            itemObj.SetActive(true);

            // ノード情報を表示するコンポーネントがあれば設定
            var nodeItem = itemObj.GetComponent<MissionNodeItemUI>();
            if (nodeItem != null)
            {
                nodeItem.Initialize(node, index, this);
            }
            else
            {
                // フォールバック: テキストで表示
                var text = itemObj.GetComponentInChildren<Text>();
                if (text != null)
                {
                    string bodyName = node.TargetBody != null ? node.TargetBody.BodyName : "Unknown";
                    text.text = $"{index + 1}. {node.NodeName} ({node.Type}) - {bodyName}";
                }
            }
        }

        private void UpdateStats()
        {
            if (StatsText == null || CurrentPlan == null) return;

            // レッグが計算済みの場合のみ統計を表示
            if (CurrentPlan.Legs.Count > 0)
            {
                StatsText.text = $"<b>Mission Statistics</b>\n" +
                               $"Total ΔV: {CurrentPlan.TotalRequiredDeltaV:F0} m/s\n" +
                               $"Total Duration: {CurrentPlan.TotalEstimatedDuration:F0} s\n" +
                               $"Nodes: {CurrentPlan.Nodes.Count}\n" +
                               $"Legs: {CurrentPlan.Legs.Count}\n" +
                               $"Valid: {(CurrentPlan.IsValid() ? "Yes" : "No")}";
            }
            else
            {
                StatsText.text = $"<b>Mission Statistics</b>\n" +
                               $"Nodes: {(_tempNodes.Count > 0 ? _tempNodes.Count : CurrentPlan.Nodes.Count)}\n" +
                               $"<color=orange>Calculate Legs to see statistics</color>";
            }
        }

        // === イベントハンドラー ===

        private void OnAddNodeClicked()
        {
            // ノード編集ダイアログを開く（新規作成）
            if (NodeEditDialog != null)
            {
                NodeEditDialog.OpenDialog(null, OnNodeDialogApplied);
            }
            else
            {
                // ダイアログがない場合は簡易追加
                var newNode = CreateNewNode();
                
                if (_tempNodes.Count == 0 && CurrentPlan.Nodes.Count == 0)
                {
                    // 最初のノードの到着時刻を現在時刻に設定
                    if (TimeManager.Instance != null)
                    {
                        newNode.ArrivalTime = TimeManager.Instance.UniverseTime;
                    }
                }
                else
                {
                    // 最後のノードの出発時刻を計算
                    MissionNode lastNode = CurrentPlan.Nodes.Count > 0 
                        ? CurrentPlan.Nodes[CurrentPlan.Nodes.Count - 1]
                        : _tempNodes[_tempNodes.Count - 1];
                    
                    newNode.ArrivalTime = lastNode.DepartureTime;
                }

                _tempNodes.Add(newNode);
                UpdateDisplay();
            }
        }

        private void OnNodeDialogApplied(MissionNode node)
        {
            // 新規ノードの場合、時刻を設定
            if (!_tempNodes.Contains(node) && !CurrentPlan.Nodes.Contains(node))
            {
                if (_tempNodes.Count == 0 && CurrentPlan.Nodes.Count == 0)
                {
                    // 最初のノードの到着時刻を現在時刻に設定
                    if (TimeManager.Instance != null)
                    {
                        node.ArrivalTime = TimeManager.Instance.UniverseTime;
                    }
                }
                else
                {
                    // 最後のノードの出発時刻を計算
                    MissionNode lastNode = CurrentPlan.Nodes.Count > 0 
                        ? CurrentPlan.Nodes[CurrentPlan.Nodes.Count - 1]
                        : _tempNodes[_tempNodes.Count > 0 ? _tempNodes.Count - 1 : 0];
                    
                    if (lastNode != null)
                        node.ArrivalTime = lastNode.DepartureTime;
                }

                _tempNodes.Add(node);
            }

            UpdateDisplay();
        }

        private MissionNode CreateNewNode()
        {
            MissionNode node = new MissionNode();
            node.NodeName = $"Node {_tempNodes.Count + CurrentPlan.Nodes.Count + 1}";
            node.Type = LocationType.Orbit;
            
            // デフォルトの天体を設定（利用可能な最初の天体）
            if (MapManager.Instance != null && MapManager.Instance.AllBodies.Count > 0)
            {
                node.TargetBody = MapManager.Instance.AllBodies[0];
            }

            return node;
        }

        private void OnCalculateLegsClicked()
        {
            // 一時ノードをプランに移行
            if (_tempNodes.Count > 0)
            {
                CurrentPlan.Nodes.AddRange(_tempNodes);
                _tempNodes.Clear();
            }

            if (CurrentPlan.Nodes.Count < 2)
            {
                Debug.LogWarning("MissionPlanEditor: Need at least 2 nodes to calculate legs");
                return;
            }

            // 各ノード間のレッグを計算
            CurrentPlan.Legs.Clear();
            
            double currentTime = CurrentPlan.Nodes[0].ArrivalTime;

            for (int i = 0; i < CurrentPlan.Nodes.Count - 1; i++)
            {
                var fromNode = CurrentPlan.Nodes[i];
                var toNode = CurrentPlan.Nodes[i + 1];

                // 到着時刻と出発時刻を設定
                fromNode.ArrivalTime = currentTime;
                fromNode.DepartureTime = currentTime + fromNode.StayDuration;

                // レッグを計算
                var leg = MissionPlanner.CalculateLeg(fromNode, toNode, fromNode.DepartureTime);
                
                if (leg != null)
                {
                    // レッグの参照を正しく設定
                    leg.FromNode = fromNode;
                    leg.ToNode = toNode;
                    
                    CurrentPlan.Legs.Add(leg);
                    
                    // 次のノードの到着時刻を計算
                    currentTime = fromNode.DepartureTime + leg.TravelTime;
                    toNode.ArrivalTime = currentTime;
                    toNode.DepartureTime = currentTime + toNode.StayDuration;
                }
                else
                {
                    Debug.LogError($"MissionPlanEditor: Failed to calculate leg from Node[{i}] to Node[{i + 1}]");
                }
            }

            // 最後のノードの時刻も設定
            if (CurrentPlan.Nodes.Count > 0)
            {
                var lastNode = CurrentPlan.Nodes[CurrentPlan.Nodes.Count - 1];
                lastNode.DepartureTime = lastNode.ArrivalTime + lastNode.StayDuration;
            }

            UpdateDisplay();
            Debug.Log($"MissionPlanEditor: Calculated {CurrentPlan.Legs.Count} legs. Plan valid: {CurrentPlan.IsValid()}");
        }

        private void OnSavePlanClicked()
        {
            // TODO: プランの保存機能（ScriptableObjectまたはJSON）
            Debug.Log("Save Plan clicked - Not yet implemented");
        }

        private void OnLoadPlanClicked()
        {
            // TODO: プランの読み込み機能
            Debug.Log("Load Plan clicked - Not yet implemented");
        }

        private void OnCloseClicked()
        {
            // エディタを閉じる（マップビューに戻る）
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetState(GameState.LocalMap);
            }
        }

        // 外部から呼び出されるメソッド（ノード項目から）
        public void OnNodeEditClicked(MissionNode node, int index)
        {
            if (NodeEditDialog != null)
            {
                NodeEditDialog.OpenDialog(node, (editedNode) => {
                    UpdateDisplay();
                });
            }
            else
            {
                Debug.LogWarning($"Edit Node[{index}]: {node.NodeName} - NodeEditDialog not assigned");
            }
        }

        public void OnNodeDeleteClicked(MissionNode node, int index)
        {
            if (CurrentPlan.Nodes.Contains(node))
            {
                CurrentPlan.Nodes.RemoveAt(index);
                // レッグも再計算が必要
                CurrentPlan.Legs.Clear();
            }
            else if (_tempNodes.Contains(node))
            {
                _tempNodes.RemoveAt(index);
            }
            
            UpdateDisplay();
        }

#if UNITY_EDITOR
        [ContextMenu("Auto Generate UI")]
        public void AutoGenerateUI()
        {
            // 1. Canvasの確保
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObj = new GameObject("MissionPlanEditorCanvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                canvasObj.AddComponent<GraphicRaycaster>();
                
                transform.SetParent(canvasObj.transform, false);
            }

            // 2. 背景パネル
            if (transform.Find("Panel") == null)
            {
                GameObject panel = new GameObject("Panel");
                panel.transform.SetParent(transform, false);
                Image img = panel.AddComponent<Image>();
                img.color = new Color(0, 0, 0, 0.9f);
                RectTransform rt = panel.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.sizeDelta = Vector2.zero;
            }

            // 3. タイトルとプラン名
            if (PlanNameText == null)
            {
                GameObject titleObj = new GameObject("PlanNameText");
                titleObj.transform.SetParent(transform, false);
                Text txt = titleObj.AddComponent<Text>();
                txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                txt.fontSize = 24;
                txt.color = Color.white;
                txt.alignment = TextAnchor.UpperLeft;
                txt.text = "Mission Plan Editor";
                
                RectTransform rt = titleObj.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0, 1);
                rt.anchorMax = new Vector2(0, 1);
                rt.pivot = new Vector2(0, 1);
                rt.anchoredPosition = new Vector2(20, -20);
                rt.sizeDelta = new Vector2(400, 30);
                
                PlanNameText = txt;
            }

            if (PlanNameInput == null)
            {
                PlanNameInput = CreateInputField("PlanNameInput", "Plan Name", new Vector2(0, 1), new Vector2(0, 1), new Vector2(20, -60), new Vector2(300, 30));
            }

            if (PlanDescriptionInput == null)
            {
                PlanDescriptionInput = CreateInputField("PlanDescriptionInput", "Description", new Vector2(0, 1), new Vector2(0, 1), new Vector2(20, -100), new Vector2(400, 60));
            }

            // 4. ノードリスト（ScrollView）
            if (NodeListContainer == null)
            {
                GameObject scrollView = new GameObject("NodeListScrollView");
                scrollView.transform.SetParent(transform, false);
                RectTransform svRt = scrollView.AddComponent<RectTransform>();
                svRt.anchorMin = new Vector2(0, 0);
                svRt.anchorMax = new Vector2(0.5f, 1);
                svRt.pivot = new Vector2(0, 0.5f);
                svRt.anchoredPosition = new Vector2(20, 0);
                svRt.sizeDelta = new Vector2(-40, -200);

                Image svImg = scrollView.AddComponent<Image>();
                svImg.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

                VerticalLayoutGroup vlg = scrollView.AddComponent<VerticalLayoutGroup>();
                vlg.childControlWidth = true;
                vlg.childControlHeight = false;
                vlg.padding = new RectOffset(5, 5, 5, 5);
                vlg.spacing = 5;

                ContentSizeFitter csf = scrollView.AddComponent<ContentSizeFitter>();
                csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                NodeListContainer = scrollView.transform;
            }

            // 5. ノード項目プレハブ
            if (NodeItemPrefab == null)
            {
                NodeItemPrefab = CreateNodeItemPrefab();
            }

            // 6. 統計表示
            if (StatsText == null)
            {
                GameObject statsObj = new GameObject("StatsText");
                statsObj.transform.SetParent(transform, false);
                Text txt = statsObj.AddComponent<Text>();
                txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                txt.fontSize = 16;
                txt.color = Color.white;
                txt.alignment = TextAnchor.UpperLeft;
                
                RectTransform rt = statsObj.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0, 1);
                rt.anchoredPosition = new Vector2(20, 100);
                rt.sizeDelta = new Vector2(400, 200);
                
                StatsText = txt;
            }

            // 7. ボタン
            if (AddNodeButton == null)
            {
                AddNodeButton = CreateSimpleButton("AddNodeButton", "Add Node", new Vector2(0, 0), new Vector2(0, 0), new Vector2(20, 20), new Vector2(120, 40));
            }

            if (CalculateLegsButton == null)
            {
                CalculateLegsButton = CreateSimpleButton("CalculateLegsButton", "Calculate Legs", new Vector2(0, 0), new Vector2(0, 0), new Vector2(150, 20), new Vector2(120, 40));
            }

            if (SavePlanButton == null)
            {
                SavePlanButton = CreateSimpleButton("SavePlanButton", "Save", new Vector2(1, 0), new Vector2(1, 0), new Vector2(-150, 20), new Vector2(80, 40));
            }

            if (LoadPlanButton == null)
            {
                LoadPlanButton = CreateSimpleButton("LoadPlanButton", "Load", new Vector2(1, 0), new Vector2(1, 0), new Vector2(-60, 20), new Vector2(80, 40));
            }

            if (CloseButton == null)
            {
                CloseButton = CreateSimpleButton("CloseButton", "Close", new Vector2(1, 1), new Vector2(1, 1), new Vector2(-20, -20), new Vector2(80, 40));
            }

            // 8. ノード編集ダイアログ
            if (NodeEditDialog == null)
            {
                GameObject dialogObj = new GameObject("NodeEditDialog");
                dialogObj.transform.SetParent(transform.parent, false); // Canvasの直下
                NodeEditDialog = dialogObj.AddComponent<MissionNodeEditDialog>();
                
                // ダイアログのAuto Generate UIを実行
                var dialogMethod = typeof(MissionNodeEditDialog).GetMethod("AutoGenerateUI", 
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                dialogMethod?.Invoke(NodeEditDialog, null);
            }

            UnityEditor.EditorUtility.SetDirty(this);
        }

        private GameObject CreateNodeItemPrefab()
        {
            GameObject prefab = new GameObject("NodeItemTemplate");
            prefab.transform.SetParent(transform, false);
            
            RectTransform rt = prefab.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, 80);

            Image img = prefab.AddComponent<Image>();
            img.color = new Color(0.3f, 0.3f, 0.3f, 0.8f);

            HorizontalLayoutGroup hlg = prefab.AddComponent<HorizontalLayoutGroup>();
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.padding = new RectOffset(5, 5, 5, 5);
            hlg.spacing = 10;

            // ノード情報テキスト
            GameObject infoObj = new GameObject("NodeInfoText");
            infoObj.transform.SetParent(prefab.transform, false);
            Text infoText = infoObj.AddComponent<Text>();
            infoText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            infoText.fontSize = 14;
            infoText.color = Color.white;
            infoText.alignment = TextAnchor.MiddleLeft;
            RectTransform infoRt = infoObj.GetComponent<RectTransform>();
            infoRt.sizeDelta = new Vector2(200, 0);

            // 詳細テキスト
            GameObject detailsObj = new GameObject("NodeDetailsText");
            detailsObj.transform.SetParent(prefab.transform, false);
            Text detailsText = detailsObj.AddComponent<Text>();
            detailsText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            detailsText.fontSize = 12;
            detailsText.color = Color.gray;
            detailsText.alignment = TextAnchor.MiddleLeft;
            RectTransform detailsRt = detailsObj.GetComponent<RectTransform>();
            detailsRt.sizeDelta = new Vector2(200, 0);

            // ボタンコンテナ
            GameObject btnContainer = new GameObject("ButtonContainer");
            btnContainer.transform.SetParent(prefab.transform, false);
            HorizontalLayoutGroup btnHlg = btnContainer.AddComponent<HorizontalLayoutGroup>();
            btnHlg.childControlWidth = false;
            btnHlg.spacing = 5;
            RectTransform btnRt = btnContainer.GetComponent<RectTransform>();
            btnRt.sizeDelta = new Vector2(100, 0);

            // 編集ボタン
            Button editBtn = CreateSimpleButton("EditButton", "Edit", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(50, 30));
            editBtn.transform.SetParent(btnContainer.transform, false);

            // 削除ボタン
            Button deleteBtn = CreateSimpleButton("DeleteButton", "Delete", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(50, 30));
            deleteBtn.transform.SetParent(btnContainer.transform, false);

            // MissionNodeItemUIコンポーネントを追加
            var nodeItemUI = prefab.AddComponent<MissionNodeItemUI>();
            nodeItemUI.SetUIReferences(infoText, detailsText, editBtn, deleteBtn);

            prefab.SetActive(false);
            return prefab;
        }

        private InputField CreateInputField(string name, string placeholder, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size)
        {
            GameObject inputObj = new GameObject(name);
            inputObj.transform.SetParent(transform, false);
            
            Image img = inputObj.AddComponent<Image>();
            img.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

            InputField inputField = inputObj.AddComponent<InputField>();
            
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(inputObj.transform, false);
            Text text = textObj.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 14;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleLeft;
            inputField.textComponent = text;
            RectTransform textRt = textObj.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.sizeDelta = Vector2.zero;
            textRt.offsetMin = new Vector2(5, 0);
            textRt.offsetMax = new Vector2(-5, 0);

            GameObject placeholderObj = new GameObject("Placeholder");
            placeholderObj.transform.SetParent(inputObj.transform, false);
            Text placeholderText = placeholderObj.AddComponent<Text>();
            placeholderText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            placeholderText.fontSize = 14;
            placeholderText.color = new Color(1, 1, 1, 0.5f);
            placeholderText.alignment = TextAnchor.MiddleLeft;
            placeholderText.text = placeholder;
            inputField.placeholder = placeholderText;
            RectTransform placeholderRt = placeholderObj.GetComponent<RectTransform>();
            placeholderRt.anchorMin = Vector2.zero;
            placeholderRt.anchorMax = Vector2.one;
            placeholderRt.sizeDelta = Vector2.zero;
            placeholderRt.offsetMin = new Vector2(5, 0);
            placeholderRt.offsetMax = new Vector2(-5, 0);

            RectTransform rt = inputObj.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = anchorMin;
            rt.anchoredPosition = position;
            rt.sizeDelta = size;

            return inputField;
        }

        private Button CreateSimpleButton(string name, string label, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size)
        {
            GameObject btnObj = new GameObject(name);
            btnObj.transform.SetParent(transform, false);
            Image img = btnObj.AddComponent<Image>();
            Button btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = img;
            
            GameObject txtObj = new GameObject("Text");
            txtObj.transform.SetParent(btnObj.transform, false);
            Text txt = txtObj.AddComponent<Text>();
            txt.text = label;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.color = Color.black;
            txt.alignment = TextAnchor.MiddleCenter;
            RectTransform txtRt = txtObj.GetComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.sizeDelta = Vector2.zero;
            
            RectTransform rt = btnObj.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = anchorMin;
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
            
            return btn;
        }
#endif
    }
}

