using UnityEngine;
using UnityEditor;
using SpaceLogistics.Space;
using SpaceLogistics.Core;

namespace SpaceLogistics.Editor
{
    public class SolarSystemGenerator
    {
        [MenuItem("SpaceLogistics/Generate Solar System")]
        public static void Generate()
        {
            // 親となるコンテナ
            GameObject spaceContainer = GameObject.Find("Space");
            if (spaceContainer == null) spaceContainer = new GameObject("Space");

            // 1. Earth
            CelestialBody earth = CreateBody("Earth", spaceContainer.transform, 
                massKg: 5.972e24, radiusKm: 6371, 
                orbitAxis: 0, globalPos: new Vector3(-10, 0, 0), 
                color: Color.blue);

            // 2. Moon (Orbiting Earth)
            CreateBody("Moon", earth.transform, 
                massKg: 7.342e22, radiusKm: 1737, 
                orbitAxis: 5, globalPos: Vector3.zero, // GlobalPos ignored for satellite
                color: Color.gray);

            // 3. Mars
            CelestialBody mars = CreateBody("Mars", spaceContainer.transform, 
                massKg: 6.39e23, radiusKm: 3389, 
                orbitAxis: 0, globalPos: new Vector3(10, 0, 0), 
                color: new Color(1f, 0.3f, 0f)); // Orange

            // 4. Phobos (Orbiting Mars)
            CreateBody("Phobos", mars.transform, 
                massKg: 1.0659e16, radiusKm: 11, 
                orbitAxis: 3, globalPos: Vector3.zero, 
                color: new Color(0.6f, 0.4f, 0.2f));

            // 5. Deimos (Orbiting Mars)
            CreateBody("Deimos", mars.transform, 
                massKg: 1.4762e15, radiusKm: 6, 
                orbitAxis: 6, globalPos: Vector3.zero, 
                color: new Color(0.5f, 0.3f, 0.1f));

            Debug.Log("Solar System Generated: Earth, Moon, Mars, Phobos, Deimos");
            
            // ManagersのMapManager設定更新を促す
            Debug.LogWarning("Please assign 'Earth' to ActiveLocalBody in MapManager manually or via script.");
        }

        private static CelestialBody CreateBody(string name, Transform parent, double massKg, double radiusKm, double orbitAxis, Vector3 globalPos, Color color)
        {
            // 既存を探す
            GameObject old = GameObject.Find(name);
            if (old != null)
            {
                // 親が違う場合は移動
                if (old.transform.parent != parent) old.transform.parent = parent;
            }
            
            GameObject go = old != null ? old : new GameObject(name);
            go.transform.SetParent(parent);

            CelestialBody body = go.GetComponent<CelestialBody>();
            if (body == null) body = go.AddComponent<CelestialBody>();

            // パラメータ設定
            body.BodyName = name;
            
            // SerializeField経由で値を流し込む (CelestialBody.OnValidateを利用してMass等を更新)
            SerializedObject so = new SerializedObject(body);
            so.FindProperty("_massKg").doubleValue = massKg;
            so.FindProperty("_radiusKm").doubleValue = radiusKm;
            so.FindProperty("_soiRadiusKm").doubleValue = radiusKm * 10; // 簡易計算
            so.ApplyModifiedProperties();

            // 直接Structも設定（Runtime用）
            body.Mass = new Mass(massKg);
            body.Radius = Distance.FromKilometers(radiusKm);
            body.SOIRadius = Distance.FromKilometers(radiusKm * 10);
            body.AbstractGlobalPosition = globalPos;

            // 軌道設定
            if (body.OrbitData == null) body.OrbitData = new OrbitParameters();
            body.OrbitData.SemiMajorAxis = orbitAxis;
            body.OrbitData.MeanMotion = 1.0; // 簡易速度
            
            // 親設定
            CelestialBody parentBody = parent.GetComponent<CelestialBody>();
            body.ParentBody = parentBody;

            // Visuals
            SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
            if (sr == null) sr = go.AddComponent<SpriteRenderer>();
            
            // Sprite設定 (デフォルトのKnobがあれば使う、なければ白丸生成は面倒なので今は色だけ変える)
            // エディタ標準のKnobスプライトを探す
            if (sr.sprite == null)
            {
                sr.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            }
            sr.color = color;
            body.BodyRenderer = sr;
            
            // スケール
            body.VisualScaleLocal = (float)(radiusKm / 1000.0); // 表示サイズ調整
            if (body.VisualScaleLocal < 0.5f) body.VisualScaleLocal = 0.5f;
            body.VisualScaleGlobal = 2.0f; // GlobalMapでのアイコンサイズ

            return body;
        }
    }
}
