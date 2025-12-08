using UnityEngine;
using SpaceLogistics.Rocketry;
using SpaceLogistics.Core;
using SpaceLogistics.Missions;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

namespace SpaceLogistics.UI
{
    /// <summary>
    /// ロケット設計画面（VAB）のUIクラス。
    /// パーツの選択、追加、統計情報の表示、および発射シーケンスへの移行を管理する。
    /// </summary>
    public class RocketEditorUI : MonoBehaviour
    {
        [Header("Data")]
        public RocketBlueprint CurrentBlueprint;
        private List<RocketPart> _availableParts;

        [Header("UI References")]
        public Transform PartsListContainer; // パーツ選択ボタンの親
        public GameObject PartButtonPrefab; // パーツ選択ボタンのプレハブ
        public Text StatsText;
        public Button ClearButton;
        public Button LaunchButton;

        private void Start()
        {
            InitializeBlueprint();
            LoadParts();
            SetupUI();
            UpdateStatsDisplay();

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnStateChanged += HandleStateChanged;
                // 初期状態適用のために呼び出し
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
            // エディタモードの時のみ表示
            bool isEditor = (newState == GameState.RocketEditor);
            
            // 親のCanvas、または自分自身のActiveを切り替え
            // ここでは親のCanvas（通常RocketEditorCanvas）を探して切り替えるのが安全
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                canvas.enabled = isEditor;
                // Raycasterも切らないとクリックできてしまう場合がある
                var raycaster = canvas.GetComponent<GraphicRaycaster>();
                if (raycaster != null) raycaster.enabled = isEditor;
            }
            else
            {
                // Canvasが見つからない場合は自身をActive切り替え（非推奨だがフォールバック）
                gameObject.SetActive(isEditor);
            }
        }

        private void InitializeBlueprint()
        {
            CurrentBlueprint = new RocketBlueprint();
            CurrentBlueprint.DesignName = "New Rocket";
            // 初期ステージを作成
            CurrentBlueprint.Stages.Add(new RocketStage { StageIndex = 0 });
        }

        private void LoadParts()
        {
            // Resources/RocketParts からすべてのパーツをロード
            _availableParts = Resources.LoadAll<RocketPart>("RocketParts").ToList();
        }

        private void SetupUI()
        {
            // パーツリストの生成
            if (PartsListContainer != null && PartButtonPrefab != null)
            {
                foreach (var part in _availableParts)
                {
                    GameObject btnObj = Instantiate(PartButtonPrefab, PartsListContainer);
                    btnObj.SetActive(true); // プレハブが非アクティブな場合があるため
                    var btn = btnObj.GetComponent<Button>();
                    var txt = btnObj.GetComponentInChildren<Text>();
                    
                    if (txt != null) txt.text = $"{part.PartName}\n({part.Type})";
                    
                    if (btn != null)
                    {
                        btn.onClick.AddListener(() => OnAddPartClicked(part));
                    }
                }
            }

            if (ClearButton != null)
                ClearButton.onClick.AddListener(OnClearClicked);

            if (LaunchButton != null)
                LaunchButton.onClick.AddListener(OnLaunchClicked);
        }

        /// <summary>
        /// パーツを追加する。現在は最下段（Stage 0）にすべて追加する簡易実装。
        /// </summary>
        public void OnAddPartClicked(RocketPart part)
        {
            if (CurrentBlueprint.Stages.Count == 0)
            {
                CurrentBlueprint.Stages.Add(new RocketStage { StageIndex = 0 });
            }
            
            // 簡易実装: 常に Stage[0] に追加
            CurrentBlueprint.Stages[0].Parts.Add(part);
            UpdateStatsDisplay();
        }

        public void OnClearClicked()
        {
            InitializeBlueprint();
            UpdateStatsDisplay();
        }

        public void OnLaunchClicked()
        {
            Debug.Log("Launching Rocket!");

            // 1. MissionManagerの確認
            if (MissionManager.Instance == null)
            {
                Debug.LogError("MissionManager not found!");
                return;
            }

            // 2. ルートの設定（MVP用のダミー）
            // マップ内に少なくとも2つの天体があれば、それらを結ぶルートを作成
            if (Space.MapManager.Instance == null || Space.MapManager.Instance.AllBodies.Count < 2)
            {
                Debug.LogWarning("Not enough celestial bodies for a mission.");
                // 天体が不足していても、とりあえず動作確認のためにnullルートで発射を試みるか、
                // あるいはここであきらめる。ここではエラーを出してリターン。
                return;
            }

            // 明示的に "Earth" を出発地、 "Moon" を目的地にする
            var originBody = Space.MapManager.Instance.AllBodies.Find(b => b.BodyName == "Earth");
            var destBody = Space.MapManager.Instance.AllBodies.Find(b => b.BodyName == "Moon");

            // 見つからない場合はリスト順のフォールバック
            if (originBody == null && Space.MapManager.Instance.AllBodies.Count > 0) originBody = Space.MapManager.Instance.AllBodies[0];
            if (destBody == null && Space.MapManager.Instance.AllBodies.Count > 1) destBody = Space.MapManager.Instance.AllBodies[1];
            
            // それでもnullならエラー
            if (originBody == null || destBody == null)
            {
                 Debug.LogError("Could not determine Origin or Destination bodies.");
                 return;
            }

            // 簡易的にRouteオブジェクトを作成 (本来はRoute Selection UIが必要)
            // RouteはMonoBehaviourではないのでnewで作成する
            
            // 天体の子にBaseがあるか探す、なければ仮定する
            var originBase = originBody.GetComponentInChildren<SpaceLogistics.Structures.Base>();
            var destBase = destBody.GetComponentInChildren<SpaceLogistics.Structures.Base>();

            // BaseがないとRouteが作れないので、テスト用に一時的にコンポーネント追加（乱暴だがテスト進行のため）
            if (originBase == null) originBase = originBody.gameObject.AddComponent<SpaceLogistics.Structures.Base>();
            if (destBase == null) destBase = destBody.gameObject.AddComponent<SpaceLogistics.Structures.Base>();

            Route dummyRoute = new Route(originBase, destBase, TrajectoryProfile.Hohmann);
            dummyRoute.RequiredDeltaV = 1000f; // テスト用の低い値

            // 3. ミッション開始
            ActiveRocket rocket = MissionManager.Instance.StartMission(dummyRoute, CurrentBlueprint);

            if (rocket != null)
            {
                 Debug.Log("Mission Started Successfully!");
                 
                // 強制的にローカルマップへ切り替え
                Core.GameManager.Instance.SetState(Core.GameState.LocalMap);

                // Hide editor
                gameObject.SetActive(false);
            }
            else
            {
                Debug.LogError("Mission Failed to Start.");
            }
        }

        private void UpdateStatsDisplay()
        {
            if (StatsText == null) return;

            RocketStats stats = CurrentBlueprint.CalculateTotalStats();
            
            StatsText.text = $"<b>Rocket Stats</b>\n" +
                             $"Mass: {stats.TotalMass:F1} t\n" +
                             $"Dry Mass: {stats.DryMass:F1} t\n" +
                             $"Delta-V: {stats.DeltaV:F0} m/s\n" +
                             $"TWR (Surface): {stats.TWR_Surface:F2}\n" +
                             $"Parts: {CurrentBlueprint.Stages[0].Parts.Count}";
        }

#if UNITY_EDITOR
        [ContextMenu("Auto Generate UI")]
        public void AutoGenerateUI()
        {
            // 1. Canvasの確保
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObj = new GameObject("RocketEditorCanvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                canvasObj.AddComponent<GraphicRaycaster>();
                
                // 自分自身をCanvasの子にする
                transform.SetParent(canvasObj.transform, false);
            }

            // 2. 背景パネル (Panel)
            if (transform.Find("Panel") == null)
            {
                GameObject panel = new GameObject("Panel");
                panel.transform.SetParent(transform, false);
                Image img = panel.AddComponent<Image>();
                img.color = new Color(0, 0, 0, 0.8f);
                RectTransform rt = panel.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.sizeDelta = Vector2.zero;
            }

            // 3. 統計テキスト (StatsText)
            if (StatsText == null)
            {
                GameObject txtObj = new GameObject("StatsText");
                txtObj.transform.SetParent(transform, false);
                Text txt = txtObj.AddComponent<Text>();
                txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                txt.fontSize = 20;
                txt.color = Color.white;
                txt.alignment = TextAnchor.UpperLeft;
                
                RectTransform rt = txtObj.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0, 1);
                rt.anchorMax = new Vector2(0, 1);
                rt.pivot = new Vector2(0, 1);
                rt.anchoredPosition = new Vector2(20, -20);
                rt.sizeDelta = new Vector2(300, 200);
                
                StatsText = txt;
            }

            // 4. パーツリストコンテナ (PartsListContainer) - Scroll View
            if (PartsListContainer == null)
            {
                // ScrollViewの作成は複雑なので、簡易的にPanel + VerticalLayoutGroupを作成する
                GameObject svObj = new GameObject("PartsScrollView");
                svObj.transform.SetParent(transform, false);
                RectTransform svRt = svObj.AddComponent<RectTransform>();
                svRt.anchorMin = new Vector2(1, 0);
                svRt.anchorMax = new Vector2(1, 1);
                svRt.pivot = new Vector2(1, 0.5f);
                svRt.anchoredPosition = new Vector2(-10, 0);
                svRt.sizeDelta = new Vector2(200, 0); // 全画面高さ

                // 背景
                Image svImg = svObj.AddComponent<Image>();
                svImg.color = new Color(0.2f, 0.2f, 0.2f, 0.5f);

                // Viewport等は省略し、直接LayoutGroupをつける（MVP用）
                VerticalLayoutGroup vlg = svObj.AddComponent<VerticalLayoutGroup>();
                vlg.childControlWidth = true;
                vlg.childControlHeight = false;
                vlg.padding = new RectOffset(5, 5, 5, 5);
                vlg.spacing = 5;
                
                PartsListContainer = svObj.transform;
            }

            // 5. ボタンプレハブ (PartButtonPrefab)
            // これはシーン内の隠しオブジェクトとして生成して参照する
            if (PartButtonPrefab == null)
            {
                 GameObject btnTemplate = new GameObject("PartButtonTemplate");
                 btnTemplate.transform.SetParent(transform, false);
                 Image btnImg = btnTemplate.AddComponent<Image>();
                 Button btn = btnTemplate.AddComponent<Button>();
                 btn.targetGraphic = btnImg;
                 
                 RectTransform rt = btnTemplate.GetComponent<RectTransform>();
                 rt.sizeDelta = new Vector2(0, 40); // 幅は親に合わせる

                 // 子のテキスト
                 GameObject txtObj = new GameObject("Text");
                 txtObj.transform.SetParent(btnTemplate.transform, false);
                 Text txt = txtObj.AddComponent<Text>();
                 txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                 txt.color = Color.black;
                 txt.alignment = TextAnchor.MiddleCenter;
                 RectTransform txtRt = txtObj.GetComponent<RectTransform>();
                 txtRt.anchorMin = Vector2.zero;
                 txtRt.anchorMax = Vector2.one;
                 txtRt.sizeDelta = Vector2.zero;

                 // テンプレートなので非アクティブにしておく
                 btnTemplate.SetActive(false);
                 PartButtonPrefab = btnTemplate;
            }
            
            // 6. アクションボタン (Clear, Launch)
            if (ClearButton == null)
            {
                ClearButton = CreateSimpleButton("ClearButton", "Clear", new Vector2(100, -20), new Vector2(0.5f, 0));
                RectTransform rt = ClearButton.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0);
                rt.anchorMax = new Vector2(0.5f, 0);
                rt.anchoredPosition = new Vector2(-60, 50);
            }
            if (LaunchButton == null)
            {
                LaunchButton = CreateSimpleButton("LaunchButton", "Launch", new Vector2(100, -20), new Vector2(0.5f, 0));
                RectTransform rt = LaunchButton.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0);
                rt.anchorMax = new Vector2(0.5f, 0);
                rt.anchoredPosition = new Vector2(60, 50);
            }
            
            UnityEditor.EditorUtility.SetDirty(this);
        }

        private Button CreateSimpleButton(string name, string label, Vector2 pos, Vector2 pivot)
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
            rt.sizeDelta = new Vector2(100, 30);
            return btn;
        }
#endif
    }
}
