using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using SpaceLogistics.Missions;
using SpaceLogistics.Space;
using SpaceLogistics.Structures;
using SpaceLogistics.Core;

namespace SpaceLogistics.UI
{
    /// <summary>
    /// ノード編集ダイアログクラス。
    /// ノードの詳細情報（天体、LocationType、軌道パラメータ等）を編集する。
    /// </summary>
    public class MissionNodeEditDialog : MonoBehaviour
    {
        [Header("UI References")]
        public GameObject DialogPanel;
        public Text DialogTitleText;
        
        // 基本情報
        public InputField NodeNameInput;
        public Dropdown LocationTypeDropdown;
        
        // 天体選択
        public Dropdown BodyDropdown;
        public Dropdown BaseDropdown; // Base/Station用
        
        // 軌道パラメータ（Orbit用）
        public GameObject OrbitParamsPanel;
        public InputField OrbitAltitudeInput;
        
        // 地上座標（Surface用）
        public GameObject SurfaceParamsPanel;
        public InputField LatitudeInput;
        public InputField LongitudeInput;
        
        // タスクリスト
        public Transform TaskListContainer;
        public GameObject TaskItemPrefab;
        public Button AddTaskButton;
        
        // ボタン
        public Button ApplyButton;
        public Button CancelButton;

        private MissionNode _editingNode;
        private bool _isNewNode;
        private System.Action<MissionNode> _onApplyCallback;

        private void Start()
        {
            if (ApplyButton != null)
                ApplyButton.onClick.AddListener(OnApplyClicked);
            
            if (CancelButton != null)
                CancelButton.onClick.AddListener(OnCancelClicked);
            
            if (AddTaskButton != null)
                AddTaskButton.onClick.AddListener(OnAddTaskClicked);

            // ダイアログは初期状態で非表示
            if (DialogPanel != null)
                DialogPanel.SetActive(false);

            SetupDropdowns();
        }

        private void SetupDropdowns()
        {
            // LocationTypeドロップダウン
            if (LocationTypeDropdown != null)
            {
                LocationTypeDropdown.ClearOptions();
                LocationTypeDropdown.AddOptions(new List<string> 
                { 
                    "Surface", 
                    "Orbit", 
                    "Surface Base", 
                    "Orbital Station" 
                });
                LocationTypeDropdown.onValueChanged.AddListener(OnLocationTypeChanged);
            }

            // Bodyドロップダウン
            UpdateBodyDropdown();

            // Baseドロップダウン（後で更新）
            UpdateBaseDropdown();
        }

        private void UpdateBodyDropdown()
        {
            if (BodyDropdown == null) return;

            BodyDropdown.ClearOptions();

            // イベントリスナーを一度削除（重複登録を防ぐ）
            BodyDropdown.onValueChanged.RemoveAllListeners();

            RefreshBodyList(); // リストを更新

            if (MapManager.Instance != null && MapManager.Instance.AllBodies.Count > 0)
            {
                var bodyNames = MapManager.Instance.AllBodies.Select(b => b.BodyName).ToList();
                Debug.Log($"MissionNodeEditDialog: Adding {bodyNames.Count} bodies to dropdown: {string.Join(", ", bodyNames)}");
                BodyDropdown.AddOptions(bodyNames);
                
                // ドロップダウンのオプション数が正しく設定されたか確認
                Debug.Log($"MissionNodeEditDialog: Dropdown options count after AddOptions: {BodyDropdown.options.Count}");
                
                // UIを強制的に更新
                BodyDropdown.RefreshShownValue();
            }
            else
            {
                BodyDropdown.AddOptions(new List<string> { "No Bodies Available" });
                Debug.LogWarning("MissionNodeEditDialog: No celestial bodies found. Make sure CelestialBody objects exist in the scene.");
            }

            BodyDropdown.onValueChanged.AddListener(OnBodyChanged);
        }

        private void UpdateBaseDropdown()
        {
            if (BaseDropdown == null) return;

            BaseDropdown.ClearOptions();
            BaseDropdown.AddOptions(new List<string> { "None" });

            if (_editingNode != null && _editingNode.TargetBody != null)
            {
                // 選択された天体の子オブジェクトからBaseを検索
                var bases = _editingNode.TargetBody.GetComponentsInChildren<Base>();
                if (bases.Length > 0)
                {
                    var baseNames = bases.Select(b => b.BaseName).ToList();
                    BaseDropdown.AddOptions(baseNames);
                }
            }
        }

        /// <summary>
        /// ダイアログを開く
        /// </summary>
        /// <param name="node">編集するノード（nullの場合は新規作成）</param>
        /// <param name="onApply">適用時に呼ばれるコールバック</param>
        public void OpenDialog(MissionNode node, System.Action<MissionNode> onApply)
        {
            _editingNode = node;
            _isNewNode = (node == null);
            _onApplyCallback = onApply;

            // ダイアログを開く時に天体リストを更新（MapManagerが初期化されている可能性があるため）
            // UpdateBodyDropdown内でRefreshBodyListが呼ばれるので、ここではUpdateBodyDropdownだけ呼ぶ
            UpdateBodyDropdown();

            if (_isNewNode)
            {
                _editingNode = new MissionNode();
                _editingNode.NodeName = "New Node";
                _editingNode.Type = LocationType.Orbit;
                
                // デフォルトの天体を設定
                if (MapManager.Instance != null && MapManager.Instance.AllBodies.Count > 0)
                {
                    _editingNode.TargetBody = MapManager.Instance.AllBodies[0];
                }
            }

            LoadNodeData();
            
            if (DialogPanel != null)
                DialogPanel.SetActive(true);
            
            if (DialogTitleText != null)
                DialogTitleText.text = _isNewNode ? "Add New Node" : "Edit Node";
        }

        /// <summary>
        /// MapManagerのAllBodiesリストを更新する（遅延初期化対応）
        /// </summary>
        private void RefreshBodyList()
        {
            if (MapManager.Instance == null)
            {
                Debug.LogWarning("MissionNodeEditDialog: MapManager.Instance is null. Make sure MapManager exists in the scene.");
                return;
            }

            // 常にシーン内のすべてのCelestialBodyを検索して更新（AllBodiesが不完全な場合があるため）
            CelestialBody[] bodies = null;
            
            #if UNITY_2021_2_OR_NEWER
            bodies = UnityEngine.Object.FindObjectsByType<CelestialBody>(UnityEngine.FindObjectsSortMode.None);
            #else
            bodies = UnityEngine.Object.FindObjectsOfType<CelestialBody>();
            #endif
            
            if (bodies != null && bodies.Length > 0)
            {
                MapManager.Instance.AllBodies.Clear();
                MapManager.Instance.AllBodies.AddRange(bodies);
                Debug.Log($"MissionNodeEditDialog: Refreshed AllBodies list. Found {bodies.Length} celestial bodies: {string.Join(", ", bodies.Select(b => b.BodyName))}");
            }
            else
            {
                Debug.LogWarning("MissionNodeEditDialog: No CelestialBody objects found in the scene. Please add CelestialBody objects to the scene.");
            }
        }

        private void LoadNodeData()
        {
            if (_editingNode == null) return;

            // ノード名
            if (NodeNameInput != null)
                NodeNameInput.text = _editingNode.NodeName;

            // LocationType
            if (LocationTypeDropdown != null)
            {
                LocationTypeDropdown.value = (int)_editingNode.Type;
                OnLocationTypeChanged(LocationTypeDropdown.value);
            }

            // 天体選択
            if (BodyDropdown != null && _editingNode.TargetBody != null && MapManager.Instance != null)
            {
                int bodyIndex = MapManager.Instance.AllBodies.IndexOf(_editingNode.TargetBody);
                Debug.Log($"MissionNodeEditDialog: Setting dropdown value to {bodyIndex} for body {_editingNode.TargetBody.BodyName}. Dropdown has {BodyDropdown.options.Count} options.");
                if (bodyIndex >= 0 && bodyIndex < BodyDropdown.options.Count)
                {
                    BodyDropdown.value = bodyIndex;
                    // UIを強制的に更新
                    BodyDropdown.RefreshShownValue();
                }
                else if (bodyIndex >= 0)
                {
                    Debug.LogWarning($"MissionNodeEditDialog: bodyIndex {bodyIndex} is out of range. Dropdown has {BodyDropdown.options.Count} options.");
                }
            }

            UpdateBaseDropdown();

            // Base選択
            if (BaseDropdown != null && _editingNode.TargetBase != null && _editingNode.TargetBody != null)
            {
                var bases = _editingNode.TargetBody.GetComponentsInChildren<Base>();
                int baseIndex = System.Array.IndexOf(bases, _editingNode.TargetBase);
                if (baseIndex >= 0)
                {
                    BaseDropdown.value = baseIndex + 1; // +1 for "None" option
                }
            }

            // 軌道パラメータ
            if (_editingNode.ParkingOrbit != null && OrbitAltitudeInput != null)
            {
                double altitude = _editingNode.ParkingOrbit.SemiMajorAxis - 
                                 (_editingNode.TargetBody != null ? _editingNode.TargetBody.Radius.ToMeters() : 0);
                OrbitAltitudeInput.text = (altitude / 1000.0).ToString("F1"); // km単位
            }

            // 地上座標
            if (LatitudeInput != null)
                LatitudeInput.text = _editingNode.SurfaceCoordinates.x.ToString("F2");
            if (LongitudeInput != null)
                LongitudeInput.text = _editingNode.SurfaceCoordinates.y.ToString("F2");

            // タスクリスト
            UpdateTaskList();
        }

        private void OnLocationTypeChanged(int value)
        {
            LocationType newType = (LocationType)value;
            
            // OrbitParamsPanelとSurfaceParamsPanelの表示/非表示を切り替え
            if (OrbitParamsPanel != null)
                OrbitParamsPanel.SetActive(newType == LocationType.Orbit || newType == LocationType.OrbitalStation);
            
            if (SurfaceParamsPanel != null)
                SurfaceParamsPanel.SetActive(newType == LocationType.Surface || newType == LocationType.SurfaceBase);

            // BaseDropdownの表示/非表示
            if (BaseDropdown != null)
            {
                var dropdownObj = BaseDropdown.gameObject;
                dropdownObj.SetActive(newType == LocationType.SurfaceBase || newType == LocationType.OrbitalStation);
                if (dropdownObj.activeSelf)
                    UpdateBaseDropdown();
            }
        }

        private void OnBodyChanged(int value)
        {
            if (MapManager.Instance == null || value < 0 || value >= MapManager.Instance.AllBodies.Count)
                return;

            _editingNode.TargetBody = MapManager.Instance.AllBodies[value];
            UpdateBaseDropdown();
        }

        private void UpdateTaskList()
        {
            if (TaskListContainer == null || _editingNode == null) return;

            // 既存のタスク項目を削除
            foreach (Transform child in TaskListContainer)
            {
                Destroy(child.gameObject);
            }

            // タスク項目を作成
            for (int i = 0; i < _editingNode.Tasks.Count; i++)
            {
                var task = _editingNode.Tasks[i];
                CreateTaskItem(task, i);
            }
        }

        private void CreateTaskItem(MissionTask task, int index)
        {
            if (TaskItemPrefab == null) return;

            GameObject itemObj = Instantiate(TaskItemPrefab, TaskListContainer);
            itemObj.SetActive(true);

            var taskItem = itemObj.GetComponent<MissionTaskItemUI>();
            if (taskItem != null)
            {
                taskItem.Initialize(task, index, (t, idx) => OnTaskDeleteClicked(idx));
            }
        }

        private void OnAddTaskClicked()
        {
            if (_editingNode == null) return;

            var newTask = new MissionTask();
            newTask.Type = TaskType.Wait;
            newTask.Duration = 3600.0; // 1時間
            newTask.ResourceType = ResourceType.Fuel;
            newTask.Amount = 0.0f;

            _editingNode.Tasks.Add(newTask);
            UpdateTaskList();
        }

        private void OnTaskDeleteClicked(int index)
        {
            if (_editingNode == null || index < 0 || index >= _editingNode.Tasks.Count)
                return;

            _editingNode.Tasks.RemoveAt(index);
            UpdateTaskList();
        }

        private void OnApplyClicked()
        {
            if (_editingNode == null) return;

            // 入力値をノードに適用
            SaveNodeData();

            _onApplyCallback?.Invoke(_editingNode);

            // ダイアログを閉じる
            if (DialogPanel != null)
                DialogPanel.SetActive(false);
        }

        private void SaveNodeData()
        {
            if (_editingNode == null) return;

            // ノード名
            if (NodeNameInput != null)
                _editingNode.NodeName = NodeNameInput.text;

            // LocationType
            if (LocationTypeDropdown != null)
                _editingNode.Type = (LocationType)LocationTypeDropdown.value;

            // 天体
            if (BodyDropdown != null && MapManager.Instance != null && 
                BodyDropdown.value >= 0 && BodyDropdown.value < MapManager.Instance.AllBodies.Count)
            {
                _editingNode.TargetBody = MapManager.Instance.AllBodies[BodyDropdown.value];
            }

            // Base
            if (_editingNode.Type == LocationType.SurfaceBase || _editingNode.Type == LocationType.OrbitalStation)
            {
                if (BaseDropdown != null && BaseDropdown.value > 0 && _editingNode.TargetBody != null)
                {
                    var bases = _editingNode.TargetBody.GetComponentsInChildren<Base>();
                    int baseIndex = BaseDropdown.value - 1; // -1 for "None" option
                    if (baseIndex >= 0 && baseIndex < bases.Length)
                    {
                        _editingNode.TargetBase = bases[baseIndex];
                    }
                }
                else
                {
                    _editingNode.TargetBase = null;
                }
            }
            else
            {
                _editingNode.TargetBase = null;
            }

            // 軌道パラメータ
            if (_editingNode.Type == LocationType.Orbit || _editingNode.Type == LocationType.OrbitalStation)
            {
                if (OrbitAltitudeInput != null && _editingNode.TargetBody != null)
                {
                    if (double.TryParse(OrbitAltitudeInput.text, out double altitudeKm))
                    {
                        double altitudeM = altitudeKm * 1000.0;
                        double radius = _editingNode.TargetBody.Radius.ToMeters();
                        double semiMajorAxis = radius + altitudeM;

                        if (_editingNode.ParkingOrbit == null)
                            _editingNode.ParkingOrbit = new OrbitParameters();

                        _editingNode.ParkingOrbit.SemiMajorAxis = semiMajorAxis;
                        _editingNode.ParkingOrbit.Eccentricity = 0; // 円軌道
                        _editingNode.ParkingOrbit.Inclination = 0;
                        
                        // MeanMotionを計算
                        double G = Core.PhysicsConstants.GameGravitationalConstant;
                        double mu = G * _editingNode.TargetBody.Mass.Kilograms;
                        _editingNode.ParkingOrbit.MeanMotion = System.Math.Sqrt(mu / System.Math.Pow(semiMajorAxis, 3));
                    }
                }
            }

            // 地上座標
            if (_editingNode.Type == LocationType.Surface || _editingNode.Type == LocationType.SurfaceBase)
            {
                if (LatitudeInput != null && float.TryParse(LatitudeInput.text, out float lat))
                    _editingNode.SurfaceCoordinates.x = lat;
                if (LongitudeInput != null && float.TryParse(LongitudeInput.text, out float lon))
                    _editingNode.SurfaceCoordinates.y = lon;
            }
        }

        private void OnCancelClicked()
        {
            // ダイアログを閉じる（変更を破棄）
            if (DialogPanel != null)
                DialogPanel.SetActive(false);
        }

#if UNITY_EDITOR
        [ContextMenu("Auto Generate UI")]
        public void AutoGenerateUI()
        {
            // 1. ダイアログパネル
            if (DialogPanel == null)
            {
                DialogPanel = new GameObject("DialogPanel");
                DialogPanel.transform.SetParent(transform, false);
                Image img = DialogPanel.AddComponent<Image>();
                img.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);
                
                RectTransform rt = DialogPanel.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = Vector2.zero;
                rt.sizeDelta = new Vector2(600, 700);
            }

            // 2. タイトル
            if (DialogTitleText == null)
            {
                GameObject titleObj = new GameObject("DialogTitleText");
                titleObj.transform.SetParent(DialogPanel.transform, false);
                Text txt = titleObj.AddComponent<Text>();
                txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                txt.fontSize = 20;
                txt.color = Color.white;
                txt.alignment = TextAnchor.UpperCenter;
                txt.text = "Edit Node";
                
                RectTransform rt = titleObj.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0, 1);
                rt.anchorMax = new Vector2(1, 1);
                rt.pivot = new Vector2(0.5f, 1);
                rt.anchoredPosition = new Vector2(0, -20);
                rt.sizeDelta = new Vector2(0, 30);
                
                DialogTitleText = txt;
            }

            // 3. ノード名入力
            if (NodeNameInput == null)
            {
                NodeNameInput = CreateInputField("NodeNameInput", "Node Name", new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -60), new Vector2(500, 30));
                NodeNameInput.transform.SetParent(DialogPanel.transform, false);
            }

            // 4. LocationTypeドロップダウン
            if (LocationTypeDropdown == null)
            {
                LocationTypeDropdown = CreateDropdown("LocationTypeDropdown", "Location Type", new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -110), new Vector2(500, 30));
                LocationTypeDropdown.transform.SetParent(DialogPanel.transform, false);
            }

            // 5. 天体ドロップダウン
            if (BodyDropdown == null)
            {
                BodyDropdown = CreateDropdown("BodyDropdown", "Celestial Body", new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -160), new Vector2(500, 30));
                BodyDropdown.transform.SetParent(DialogPanel.transform, false);
            }

            // 6. Baseドロップダウン
            if (BaseDropdown == null)
            {
                BaseDropdown = CreateDropdown("BaseDropdown", "Base/Station", new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -210), new Vector2(500, 30));
                BaseDropdown.transform.SetParent(DialogPanel.transform, false);
                BaseDropdown.gameObject.SetActive(false);
            }

            // 7. 軌道パラメータパネル
            if (OrbitParamsPanel == null)
            {
                OrbitParamsPanel = new GameObject("OrbitParamsPanel");
                OrbitParamsPanel.transform.SetParent(DialogPanel.transform, false);
                RectTransform rt = OrbitParamsPanel.AddComponent<RectTransform>();
                rt.anchorMin = new Vector2(0, 0.5f);
                rt.anchorMax = new Vector2(1, 0.7f);
                rt.sizeDelta = Vector2.zero;
                rt.anchoredPosition = Vector2.zero;

                VerticalLayoutGroup vlg = OrbitParamsPanel.AddComponent<VerticalLayoutGroup>();
                vlg.childControlWidth = true;
                vlg.spacing = 5;
                vlg.padding = new RectOffset(10, 10, 10, 10);

                if (OrbitAltitudeInput == null)
                {
                    OrbitAltitudeInput = CreateInputField("OrbitAltitudeInput", "Altitude (km)", new Vector2(0, 0), new Vector2(1, 1), Vector2.zero, new Vector2(0, 30));
                    OrbitAltitudeInput.transform.SetParent(OrbitParamsPanel.transform, false);
                }
            }

            // 8. 地上座標パネル
            if (SurfaceParamsPanel == null)
            {
                SurfaceParamsPanel = new GameObject("SurfaceParamsPanel");
                SurfaceParamsPanel.transform.SetParent(DialogPanel.transform, false);
                RectTransform rt = SurfaceParamsPanel.AddComponent<RectTransform>();
                rt.anchorMin = new Vector2(0, 0.5f);
                rt.anchorMax = new Vector2(1, 0.7f);
                rt.sizeDelta = Vector2.zero;
                rt.anchoredPosition = Vector2.zero;

                HorizontalLayoutGroup hlg = SurfaceParamsPanel.AddComponent<HorizontalLayoutGroup>();
                hlg.childControlWidth = true;
                hlg.spacing = 10;
                hlg.padding = new RectOffset(10, 10, 10, 10);

                if (LatitudeInput == null)
                {
                    LatitudeInput = CreateInputField("LatitudeInput", "Latitude", new Vector2(0, 0), new Vector2(0.5f, 1), Vector2.zero, new Vector2(0, 30));
                    LatitudeInput.transform.SetParent(SurfaceParamsPanel.transform, false);
                }

                if (LongitudeInput == null)
                {
                    LongitudeInput = CreateInputField("LongitudeInput", "Longitude", new Vector2(0.5f, 0), new Vector2(1, 1), Vector2.zero, new Vector2(0, 30));
                    LongitudeInput.transform.SetParent(SurfaceParamsPanel.transform, false);
                }
            }

            // 9. タスクリスト
            if (TaskListContainer == null)
            {
                GameObject taskListObj = new GameObject("TaskListContainer");
                taskListObj.transform.SetParent(DialogPanel.transform, false);
                RectTransform rt = taskListObj.AddComponent<RectTransform>();
                rt.anchorMin = new Vector2(0, 0.2f);
                rt.anchorMax = new Vector2(1, 0.5f);
                rt.sizeDelta = Vector2.zero;
                rt.anchoredPosition = Vector2.zero;

                VerticalLayoutGroup vlg = taskListObj.AddComponent<VerticalLayoutGroup>();
                vlg.childControlWidth = true;
                vlg.childControlHeight = false;
                vlg.spacing = 5;
                vlg.padding = new RectOffset(10, 10, 10, 10);

                Image img = taskListObj.AddComponent<Image>();
                img.color = new Color(0.2f, 0.2f, 0.2f, 0.5f);

                TaskListContainer = taskListObj.transform;
            }

            // 10. タスク項目プレハブ
            if (TaskItemPrefab == null)
            {
                TaskItemPrefab = CreateTaskItemPrefab();
            }

            // 11. タスク追加ボタン
            if (AddTaskButton == null)
            {
                AddTaskButton = CreateSimpleButton("AddTaskButton", "Add Task", new Vector2(0, 0.2f), new Vector2(0, 0.2f), new Vector2(10, 10), new Vector2(100, 30));
                AddTaskButton.transform.SetParent(DialogPanel.transform, false);
            }

            // 12. 適用・キャンセルボタン
            if (ApplyButton == null)
            {
                ApplyButton = CreateSimpleButton("ApplyButton", "Apply", new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-60, 10), new Vector2(100, 40));
                ApplyButton.transform.SetParent(DialogPanel.transform, false);
            }

            if (CancelButton == null)
            {
                CancelButton = CreateSimpleButton("CancelButton", "Cancel", new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(60, 10), new Vector2(100, 40));
                CancelButton.transform.SetParent(DialogPanel.transform, false);
            }

            UnityEditor.EditorUtility.SetDirty(this);
        }

        private GameObject CreateTaskItemPrefab()
        {
            GameObject prefab = new GameObject("TaskItemTemplate");
            prefab.transform.SetParent(transform, false);
            
            RectTransform rt = prefab.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, 50);

            Image img = prefab.AddComponent<Image>();
            img.color = new Color(0.25f, 0.25f, 0.25f, 0.8f);

            HorizontalLayoutGroup hlg = prefab.AddComponent<HorizontalLayoutGroup>();
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.spacing = 10;
            hlg.padding = new RectOffset(5, 5, 5, 5);

            // タスクタイプテキスト
            GameObject typeObj = new GameObject("TaskTypeText");
            typeObj.transform.SetParent(prefab.transform, false);
            Text typeText = typeObj.AddComponent<Text>();
            typeText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            typeText.fontSize = 14;
            typeText.color = Color.white;
            typeText.alignment = TextAnchor.MiddleLeft;
            RectTransform typeRt = typeObj.GetComponent<RectTransform>();
            typeRt.sizeDelta = new Vector2(150, 0);

            // 詳細テキスト
            GameObject detailsObj = new GameObject("TaskDetailsText");
            detailsObj.transform.SetParent(prefab.transform, false);
            Text detailsText = detailsObj.AddComponent<Text>();
            detailsText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            detailsText.fontSize = 12;
            detailsText.color = Color.gray;
            detailsText.alignment = TextAnchor.MiddleLeft;
            RectTransform detailsRt = detailsObj.GetComponent<RectTransform>();
            detailsRt.sizeDelta = new Vector2(200, 0);

            // 削除ボタン
            Button deleteBtn = CreateSimpleButton("DeleteButton", "Delete", new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-10, 0), new Vector2(60, 30));
            deleteBtn.transform.SetParent(prefab.transform, false);

            // MissionTaskItemUIコンポーネントを追加
            var taskItemUI = prefab.AddComponent<MissionTaskItemUI>();
            taskItemUI.SetUIReferences(typeText, detailsText, deleteBtn);

            prefab.SetActive(false);
            return prefab;
        }

        private InputField CreateInputField(string name, string placeholder, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size)
        {
            GameObject inputObj = new GameObject(name);
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

        private Dropdown CreateDropdown(string name, string label, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size)
        {
            GameObject dropdownObj = new GameObject(name);
            
            Image img = dropdownObj.AddComponent<Image>();
            img.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

            Dropdown dropdown = dropdownObj.AddComponent<Dropdown>();

            // Label（左側）
            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(dropdownObj.transform, false);
            Text labelText = labelObj.AddComponent<Text>();
            labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            labelText.fontSize = 14;
            labelText.color = Color.white;
            labelText.alignment = TextAnchor.MiddleLeft;
            labelText.text = label;
            RectTransform labelRt = labelObj.GetComponent<RectTransform>();
            labelRt.anchorMin = new Vector2(0, 0);
            labelRt.anchorMax = new Vector2(0.5f, 1);
            labelRt.sizeDelta = Vector2.zero;

            // Caption（右側の選択値表示）
            GameObject captionObj = new GameObject("Caption");
            captionObj.transform.SetParent(dropdownObj.transform, false);
            Text captionText = captionObj.AddComponent<Text>();
            captionText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            captionText.fontSize = 14;
            captionText.color = Color.white;
            captionText.alignment = TextAnchor.MiddleLeft;
            dropdown.captionText = captionText;
            RectTransform captionRt = captionObj.GetComponent<RectTransform>();
            captionRt.anchorMin = new Vector2(0.5f, 0);
            captionRt.anchorMax = new Vector2(1, 1);
            captionRt.sizeDelta = Vector2.zero;
            captionRt.offsetMin = new Vector2(5, 0);

            // Template（ドロップダウンリスト）
            GameObject templateObj = new GameObject("Template");
            templateObj.transform.SetParent(dropdownObj.transform, false);
            templateObj.SetActive(false);
            
            Image templateImg = templateObj.AddComponent<Image>();
            templateImg.color = new Color(0.2f, 0.2f, 0.2f, 0.95f);
            ScrollRect scrollRect = templateObj.AddComponent<ScrollRect>();
            
            RectTransform templateRt = templateObj.GetComponent<RectTransform>();
            templateRt.anchorMin = new Vector2(0, 0);
            templateRt.anchorMax = new Vector2(1, 0);
            templateRt.pivot = new Vector2(0.5f, 1);
            templateRt.sizeDelta = new Vector2(0, 150);
            templateRt.anchoredPosition = new Vector2(0, 2);

            // Viewport
            GameObject viewportObj = new GameObject("Viewport");
            viewportObj.transform.SetParent(templateObj.transform, false);
            Image viewportImg = viewportObj.AddComponent<Image>();
            Mask mask = viewportObj.AddComponent<Mask>();
            mask.showMaskGraphic = false;
            scrollRect.viewport = viewportObj.GetComponent<RectTransform>();
            RectTransform viewportRt = viewportObj.GetComponent<RectTransform>();
            viewportRt.anchorMin = Vector2.zero;
            viewportRt.anchorMax = Vector2.one;
            viewportRt.sizeDelta = Vector2.zero;

            // Content
            GameObject contentObj = new GameObject("Content");
            contentObj.transform.SetParent(viewportObj.transform, false);
            VerticalLayoutGroup contentVlg = contentObj.AddComponent<VerticalLayoutGroup>();
            contentVlg.childControlWidth = true;  // 子要素の幅を制御
            contentVlg.childControlHeight = false; // 子要素の高さは制御しない（各Itemが高さ30に設定されているため）
            contentVlg.childForceExpandWidth = true; // 子要素を幅いっぱいに展開
            contentVlg.childForceExpandHeight = false;
            contentVlg.spacing = 0;
            contentVlg.padding = new RectOffset(0, 0, 0, 0);
            ContentSizeFitter contentCsf = contentObj.AddComponent<ContentSizeFitter>();
            contentCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scrollRect.content = contentObj.GetComponent<RectTransform>();
            RectTransform contentRt = contentObj.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0, 1);
            contentRt.anchorMax = new Vector2(1, 1);
            contentRt.pivot = new Vector2(0.5f, 1);
            contentRt.sizeDelta = new Vector2(0, 0);

            // Item（リスト項目のテンプレート）
            GameObject itemObj = new GameObject("Item");
            itemObj.transform.SetParent(contentObj.transform, false);
            Toggle itemToggle = itemObj.AddComponent<Toggle>();
            RectTransform itemRt = itemObj.GetComponent<RectTransform>();
            // anchorMinとanchorMaxを左右いっぱいに設定（VerticalLayoutGroupが制御するが、念のため）
            itemRt.anchorMin = new Vector2(0, 0);
            itemRt.anchorMax = new Vector2(1, 0);
            itemRt.pivot = new Vector2(0.5f, 0);
            // 高さは30に固定、幅は親（Content）いっぱい（VerticalLayoutGroupのchildControlWidthにより制御される）
            itemRt.sizeDelta = new Vector2(0, 30);
            itemRt.anchoredPosition = Vector2.zero;

            // Item Background
            GameObject itemBgObj = new GameObject("Item Background");
            itemBgObj.transform.SetParent(itemObj.transform, false);
            Image itemBgImg = itemBgObj.AddComponent<Image>();
            itemBgImg.color = new Color(0.2f, 0.2f, 0.2f, 1);
            itemToggle.targetGraphic = itemBgImg;
            RectTransform itemBgRt = itemBgObj.GetComponent<RectTransform>();
            itemBgRt.anchorMin = Vector2.zero;
            itemBgRt.anchorMax = Vector2.one;
            itemBgRt.pivot = new Vector2(0.5f, 0.5f);
            itemBgRt.anchoredPosition = Vector2.zero;
            itemBgRt.sizeDelta = Vector2.zero;

            // Item Label
            GameObject itemLabelObj = new GameObject("Item Label");
            itemLabelObj.transform.SetParent(itemObj.transform, false);
            Text itemLabelText = itemLabelObj.AddComponent<Text>();
            itemLabelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            itemLabelText.fontSize = 14;
            itemLabelText.color = Color.white;
            itemLabelText.alignment = TextAnchor.MiddleLeft;
            // itemToggle.graphicは設定しない（Textは常に表示されるべきなので）
            // Dropdownコンポーネントが自動的にitemTextプロパティでTextを管理する
            RectTransform itemLabelRt = itemLabelObj.GetComponent<RectTransform>();
            // anchorMinとanchorMaxを異なる位置に設定することで、幅が正しく計算される
            itemLabelRt.anchorMin = new Vector2(0, 0);
            itemLabelRt.anchorMax = new Vector2(1, 1);
            itemLabelRt.pivot = new Vector2(0, 0.5f);
            // offsetMin/offsetMaxで左側に10ピクセルのパディングを追加
            // offsetMin/offsetMaxを使用する場合、anchoredPositionとsizeDeltaは自動計算される
            itemLabelRt.offsetMin = new Vector2(10, 0);
            itemLabelRt.offsetMax = Vector2.zero;

            dropdown.itemText = itemLabelText;
            dropdown.template = templateRt;

            // Dropdownの正しい初期化のため、targetGraphicも設定
            if (img != null)
            {
                dropdown.targetGraphic = img;
            }

            RectTransform rt = dropdownObj.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = anchorMin;
            rt.anchoredPosition = position;
            rt.sizeDelta = size;

            return dropdown;
        }

        private Button CreateSimpleButton(string name, string label, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size)
        {
            GameObject btnObj = new GameObject(name);
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

