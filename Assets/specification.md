宇宙物流シミュレーションゲーム アーキテクチャ設計書このドキュメントでは、多段ロケットを用いた惑星間物流ゲームの要件定義、クラス設計、および主要なメカニクスについて解説します。1. ゲーム要件定義 (Game Requirements)1.1 宇宙空間とマップ構造 (World & Map Structure)2D独立マップシステム（二層構造）:ローカルマップ (Local Map): 惑星とその周辺の衛星を含む領域。物理演算が適用される。全体マップ (Global Map): 太陽系全体を俯瞰するマップ。いくつかの星が大雑把に並んで表示される。マップ切り替え: UI上のボタンで任意のタイミングで切り替え可能。マップ間移動: * ロケットがローカルマップの端に到達すると消滅し、全体マップ上に「移動中の輝点」として出現する。全体マップ上の移動では、複雑な太陽重力シミュレーションは行わず、計算された所要時間に基づいて点と点の間を移動する抽象的な表現とする。視覚的スケールと表現:天体: 惑星や衛星は、実際の物理スケールよりも相対的に大きく表示し、外見の特徴を視認しやすくする（デフォルメ表示）。ロケット・基地: * リアルスケールの3Dモデルは見えず、**発光する点（アイコン）**として表示する。ロケットは移動の軌跡（トレイル）を描画し、打ち上げ時は噴煙のエフェクトを表示する。1.2 ロケット工学と設計メカニクス (Rocket Engineering)多段ロケット設計:ロケットは複数の「ステージ（段）」で構成される。各ステージにはエンジン、燃料タンク、追加パーツを設定可能。物理パラメータ:ΔV (Delta-V): 到達可能距離の指標。推力重量比 (TWR): 1ステージごとの初期重量（燃料満タン時）に基づく値を採用し、各フェーズ（離陸、遷移など）の条件を満たすか判定する。1.3 物流ネットワークと運航計画 (Logistics & Operations)運航計画: 始点・終点・中継点（パーキング軌道など）を含むルートを設定。技術によるコスト軽減: マスドライバー、エアロブレーキ等の技術により、特定のフェーズで必要なΔVを軽減可能。検証 (Validation): 設計されたロケットがルート上の全マニューバ（必要ΔVとTWR）をクリアできるか、シミュレーションで判定する。1.4 経済・資源・施設 (Economy, Resources & Facilities)基地と施設建設:基地（Base）は単なる枠組みであり、その中に**施設（Facility）**を建設することで機能を持つ。施設の建設には基地在庫のリソースを消費する。資源生産と変換:無限資源: メイン惑星の鉄・燃料などは無尽蔵。施設を置くだけで時間経過により生産される。地域固有資源: 惑星ごとに採掘可能な資源が異なる。変換生産: 一部の施設は「入力資源」を消費して「出力資源」を生産する（例：鉄鉱石 → 合金）。研究と技術ツリー:技術ツリーは分岐構造を持つ。研究ポイントの生産: 特定の施設（例：軌道研究ラボ）が、特定の資源（例：食料）を消費することで時間経過により生産される。これにより「軌道上に人を滞在させるための補給線」を維持する動機が生まれる。2. クラス図 (Mermaid)classDiagram
    %% --- Core Managers ---
    class GameManager {
        +GameState CurrentState
        +void ToggleMapMode()
    }

    class TimeManager {
        +double UniverseTime
        +float TimeScale
        +event OnTick
    }

    class ResourceManager {
        +float ResearchPoints
        +void AddResearchPoints(float amount)
        +bool UnlockTech(string techId)
    }

    %% --- Space & Map System ---
    class CelestialBody {
        +string Name
        +double Mass
        +double Radius
        +double SOI_Radius
        +bool HasAtmosphere
        +OrbitParameters OrbitData
        +List~Base~ Bases
        +Vector3 GetLocalPosition(double time)
        +Vector3 GetGlobalPosition(double time)
    }

    class MapManager {
        +MapMode CurrentMode
        +CelestialBody ActiveLocalBody
        +void SwitchToLocalMap(CelestialBody body)
        +void SwitchToGlobalMap()
        +void RenderVisuals()
    }

    %% --- Structures & Facilities ---
    class Base {
        +string Name
        +BaseType Type
        +OrbitInfo LocalOrbit
        +Inventory Storage
        +List~Facility~ Facilities
        +void TickProduction(float deltaTime)
    }

    class Facility {
        +string Name
        +FacilityType Type
        +bool IsActive
        +List~ResourceIO~ InputResources
        +List~ResourceIO~ OutputResources
        +void Process(Inventory storage, float deltaTime)
    }

    class ResourceIO {
        +ResourceType Type
        +float RatePerSecond
    }

    class Inventory {
        +Dictionary~ResourceType, float~ Resources
        +bool TryConsume(ResourceType type, float amount)
        +void Add(ResourceType type, float amount)
    }

    %% --- Rocket Design ---
    class RocketBlueprint {
        +string DesignName
        +List~RocketStage~ Stages
        +RocketStats CalculateTotalStats()
    }

    class RocketStage {
        +int StageIndex
        +List~RocketPart~ Parts
        +RocketStats InitialStats
    }

    class RocketStats {
        +float TotalMass
        +float DeltaV
        +float TWR_Surface
    }

    %% --- Active Missions ---
    class MissionManager {
        +List~ActiveMission~ OngoingMissions
        +ValidationResult ValidateMission(Route route, RocketBlueprint rocket)
    }

    class ActiveRocket {
        +RocketBlueprint Blueprint
        +RocketState State
        +Vector3 Position
        +Route AssignedRoute
        +void UpdatePosition(double time)
    }

    class Route {
        +Base Origin
        +Base Destination
        +TrajectoryProfile Profile
        +float TotalDuration
    }

    %% --- Relationships ---
    GameManager --> MapManager
    GameManager --> ResourceManager
    
    CelestialBody o-- Base
    Base *-- Facility
    Base *-- Inventory
    Facility --> ResourceIO
    
    RocketBlueprint *-- RocketStage
    MissionManager --> ActiveRocket
    ActiveRocket --> Route
3. クラス詳細説明A. 宇宙・マップ・描画システムMapManager機能: ローカルマップと全体マップの表示切り替えを管理します。描画ロジック:ローカルモード: 中心となる惑星（ActiveLocalBody）を原点とし、物理演算に基づき衛星やロケットを描画します。天体は VisualScale 倍率を適用して大きく表示します。グローバルモード: 背景を星系図に切り替え、各惑星をアイコンとして配置します。ロケットは「A星とB星を結ぶ直線上（あるいは曲線）」の進捗度として表示します。CelestialBody機能:GetGlobalPosition(): 全体マップ用の座標（簡略化された位置）を返します。GetLocalPosition(): ローカルマップ用の精密な座標を返します。B. 経済・施設システムFacility (施設)基地内に建設される建物です。フィールド:InputResources: 稼働に必要な資源と消費レート（例: 食料 0.1/sec）。空なら消費なし（無限生産）。OutputResources: 生産される資源と生産レート（例: 研究ポイント 5/sec）。メソッド:Process(Inventory storage, float dt):毎フレーム（または一定間隔）呼ばれます。storage から Input 分の資源を引き落とせるか確認し、成功すれば Output 分の資源を storage（またはグローバルな ResearchPoints）に加算します。資源不足時は IsActive を false にし、稼働を停止します。ResourceManager & TechTree (未図示だが関連)研究ポイント: 特定の ResourceType（例: ResearchData）として扱うか、特別なグローバル変数として管理します。アンロック: UnlockTech(id) を呼ぶことで、新しい RocketPart や Facility が建設可能になります。C. ミッション・運航システムActiveRocket (稼働中のロケット)視覚表現:3Dモデルではなく SpriteRenderer や ParticleSystem (Trail Renderer) を持ちます。状態変化:Local_Ascending: ローカルマップで地表から上昇中（噴煙エフェクト）。Global_Transit: ローカルマップ端で消え、全体マップ上の点として移動中。Local_Orbiting: 目的地のローカルマップで軌道上の点として周回中。Route (運航ルート)全体マップ移動の計算:複雑なN体シミュレーションは行わず、TotalDuration（所要時間）を設定します。ActiveRocket は、出発時刻と現在時刻の差分から進行割合（0.0 〜 1.0）を計算し、全体マップ上の位置を決定します。ただし、移動可否の判定（ΔV計算）には、出発時の惑星間の位相角などを考慮した値を採用し、ゲーム性を保ちます。4. UI/UXフローの整理4.1 マップ操作デフォルト: ローカルマップ（現在注視している惑星）。ズームアウト/ボタン: 全体マップへ遷移。全体マップでの操作: 別の惑星をクリックすると、その惑星のローカルマップへ遷移。4.2 ロケット設計と発射基地をクリック → 「ロケット建造」ボタン。設計画面 (VAB) でパーツを組み合わせる。TWRとΔVがリアルタイム表示される。「運航計画作成」画面へ。目的地を選択し、ルートタイプ（直接/パーキング）を選ぶ。シミュレーション結果（可否、所要時間、コスト）が表示される。「発射」ボタンでインスタンス化。4.3 施設管理基地をクリック → 「施設管理」タブ。リソース在庫と、稼働中の施設リストが表示される。新規建設ボタンからリストを選んで建設（即時または時間経過）。稼働/停止トグルで電力や消費資源を節約可能。5. Unityプロジェクト構成案 (Project Structure)本プロジェクトにおける Assets/Scripts 以下のフォルダ構成と、各クラスの格納場所の提案です。機能（ドメイン）ごとに分割することで、依存関係を整理しやすくします。フォルダ階層構造Assets/
 ├── Scripts/
 │    ├── Core/             # ゲームの根幹となるマネージャー
 │    │    ├── GameManager.cs
 │    │    └── TimeManager.cs
 │    │
 │    ├── Space/            # 天体・マップ・物理演算関連
 │    │    ├── CelestialBody.cs
 │    │    ├── OrbitParameters.cs
 │    │    └── MapManager.cs
 │    │
 │    ├── Structures/       # 基地・施設・経済関連
 │    │    ├── Base.cs
 │    │    ├── Facility.cs
 │    │    ├── Inventory.cs
 │    │    ├── ResourceManager.cs
 │    │    └── ResourceIO.cs (ScriptableObject推奨)
 │    │
 │    ├── Rocketry/         # ロケット設計・パーツ関連
 │    │    ├── RocketBlueprint.cs
 │    │    ├── RocketStage.cs
 │    │    ├── RocketPart.cs (ScriptableObject推奨)
 │    │    └── RocketStats.cs
 │    │
 │    ├── Missions/         # 運航計画・アクティブな機体制御
 │    │    ├── MissionManager.cs
 │    │    ├── Route.cs
 │    │    └── ActiveRocket.cs
 │    │
 │    └── UI/               # UI制御
 │         ├── MapUI.cs
 │         ├── RocketEditorUI.cs
 │         └── FacilityUI.cs
 │
 ├── Prefabs/
 │    ├── Space/ (Planets, Moons)
 │    └── Rockets/ (Visual effects, Icons)
 │
 └── Resources/ (or Addressables)
      ├── RocketParts/ (Part data)
      └── Facilities/ (Facility data)
実装のヒントScriptableObjectの活用:RocketPart（エンジンの性能データなど）や Facility（施設の基本データ）は、C#クラスそのものではなく ScriptableObject として定義し、アセットとして保存することを推奨します。これにより、プログラマーがコードを触らずにUnityエディタ上でゲームバランスを調整できます。名前空間 (Namespace):フォルダ構成に合わせて SpaceLogistics.Core, SpaceLogistics.Space などの名前空間を設定すると、クラス名の衝突を防げます。