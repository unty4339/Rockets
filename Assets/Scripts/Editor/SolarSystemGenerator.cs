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
            earth.VisualScaleLocal = 40.0f; // Increased for visibility at MapScale 1e-6
            earth.LocalMapRadius = 50.0f;  

            // 2. Moon (Orbiting Earth)
            // Distance: ~384,400 km = 384,400,000 meters
            CelestialBody moon = CreateBody("Moon", earth.transform, 
                massKg: 7.342e22, radiusKm: 1737, 
                orbitAxis: 384400000.0, globalPos: Vector3.zero, 
                color: Color.gray);
            moon.VisualScaleLocal = 10.0f; 

            // 3. Mars
            CelestialBody mars = CreateBody("Mars", spaceContainer.transform, 
                massKg: 6.39e23, radiusKm: 3389, 
                orbitAxis: 0, globalPos: new Vector3(10, 0, 0), 
                color: new Color(1f, 0.3f, 0f)); 
            mars.VisualScaleLocal = 20.0f; 

            // 4. Phobos (Orbiting Mars)
            // Distance: ~9,376 km = 9,376,000 meters
            CelestialBody phobos = CreateBody("Phobos", mars.transform, 
                massKg: 1.0659e16, radiusKm: 11, 
                orbitAxis: 9376000.0, globalPos: Vector3.zero, 
                color: new Color(0.6f, 0.4f, 0.2f));
            phobos.VisualScaleLocal = 2.0f;

            // 5. Deimos (Orbiting Mars)
            // Distance: ~23,463 km = 23,463,000 meters
            CelestialBody deimos = CreateBody("Deimos", mars.transform, 
                massKg: 1.4762e15, radiusKm: 6, 
                orbitAxis: 23463000.0, globalPos: Vector3.zero, 
                color: new Color(0.5f, 0.3f, 0.1f));
            deimos.VisualScaleLocal = 1.5f;

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
                if (old.transform.parent != parent) old.transform.parent = parent;
            }
            
            GameObject go = old != null ? old : new GameObject(name);
            go.transform.SetParent(parent);

            CelestialBody body = go.GetComponent<CelestialBody>();
            if (body == null) body = go.AddComponent<CelestialBody>();

            // パラメータ設定
            body.BodyName = name;
            
            // Runtime Init
            if (body.OrbitData == null) body.OrbitData = new OrbitParameters();
            
            // Orbit Data (Physics Calculation)
            body.OrbitData.SemiMajorAxis = orbitAxis;

            double M = 0;
            CelestialBody parentBody = parent.GetComponent<CelestialBody>();
            if (parentBody != null) M = parentBody.Mass.Kilograms; // Accessing property reads from _massKg

            if (parentBody != null)
            {
                double a = orbitAxis;
                if (a > 0.001)
                {
                     double n = System.Math.Sqrt(PhysicsConstants.GameGravitationalConstant * M / (a * a * a));
                     body.OrbitData.MeanMotion = n;
                }
                else
                {
                     body.OrbitData.MeanMotion = 0;
                }
            }
            else
            {
                 body.OrbitData.MeanMotion = 0;
            }

            // Body Stats
            // Setting properties updates the backing private fields in CelestialBody
            body.Mass = new Mass(massKg);
            body.Radius = Distance.FromKilometers(radiusKm);
            body.SOIRadius = new Distance(0); // This should trigger auto calc logic if property has logic, or we set field?
            // CelestialBody SOIRadius propery logic?
            // public Distance SOIRadius; <- This is a field in the class, not a property wrapper for _soiRadiusKm!
            // Wait, looking at CelestialBody.cs viewed earlier:
            // public Mass Mass; 
            // [SerializeField] private double _massKg;
            // Awake/OnValidate syncs them.
            // BUT setters? 
            // No setters defined in the snippet I saw?
            // Let's check CelestialBody.cs content I saw in Step 624.
            // Lines 71-76 Awake sets Mass = new Mass(_massKg).
            // Lines 80-84 OnValidate sets Mass = new Mass(_massKg).
            // It does NOT show properties connecting Mass -> _massKg.
            // It shows `public Mass Mass;` which is a field of type struct Mass.
            // struct Mass might just be a wrapper.
            // If I set `body.Mass = ...` in Editor script, I am setting the struct field.
            // I am NOT setting `_massKg`.
            // When serialization happens, Unity serializes `_massKg`.
            // It might NOT serialize `public Mass Mass` if Mass is not serializable or if custom inspector/logic ignores it.
            // actually `Mass` struct in PhysicsTypes is likely `[Serializable]`.
            // But `CelestialBody` has explicit `_massKg`.
            // The intention seems to be `_massKg` is the source of truth for Inspector.
            // So I MUST set `_massKg` via Reflection or modify CelestialBody to have properties.
            
            // To be safe and clean, I should SET the serialized fields (via SerializedObject) OR update CelestialBody to have proper properties.
            // Given I cannot easily verify if Mass struct is serializable (it probably is), 
            // BUT `CelestialBody` relies on `_massKg` in Awake.
            // So I NEED to set `_massKg`.
            
            // However, OrbitParameters IS the issue right now.
            // OrbitParameters is a class instance `body.OrbitData`.
            // I setting `body.OrbitData.SemiMajorAxis = ...` updates memory.
            // `OnBeforeSerialize` takes that and puts it in string.
            // This works for OrbitData.
            
            // For Mass/Radius, I should likely stick to SerializedObject OR set the private fields via reflection?
            // Or just use SerializedObject for Mass/Radius and C# for OrbitData.
            // Hybrid approach.
            
            SerializedObject so = new SerializedObject(body);
            so.Update();
            so.FindProperty("_massKg").doubleValue = massKg;
            so.FindProperty("_radiusKm").doubleValue = radiusKm;
            so.FindProperty("_soiRadiusKm").doubleValue = 0;
            so.ApplyModifiedProperties();
            
            // Parent Body
            body.ParentBody = parentBody;
            
            // Abstract Global Position
            body.AbstractGlobalPosition = globalPos;

            // Visuals (Separate GameObject to avoid Scale Inheritance issues)
            Transform visualT = go.transform.Find("Visuals");
            GameObject visualGO;
            if (visualT != null)
            {
                visualGO = visualT.gameObject;
            }
            else
            {
                visualGO = new GameObject("Visuals");
                visualGO.transform.SetParent(go.transform);
                visualGO.transform.localPosition = Vector3.zero;
                visualGO.transform.localRotation = Quaternion.identity;
            }
            // Ensure Logic Body Scale is 1
            go.transform.localScale = Vector3.one;

            SpriteRenderer sr = visualGO.GetComponent<SpriteRenderer>();
            if (sr == null) sr = visualGO.AddComponent<SpriteRenderer>();
            
            if (sr.sprite == null)
            {
                sr.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            }
            sr.color = color;
            body.BodyRenderer = sr;
            
            // Default Scale
            body.VisualScaleLocal = 1.0f;
            body.VisualScaleGlobal = 2.0f;
            
            EditorUtility.SetDirty(body); 

            // Initialize Transform Position for Scene View (Editor)
            float initialDist = (float)(orbitAxis * SpaceLogistics.Space.MapManager.MapScale);
            body.transform.localPosition = new Vector3(initialDist, 0, 0);

            return body;
        }

        // Removed outdated SetOrbitFromPhysics helper since logic is integrated above

    }
}
