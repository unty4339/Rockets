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
            
            // Runtime Init (Ensure instance exists for serialization)
            if (body.OrbitData == null) body.OrbitData = new OrbitParameters();
            
            // SerializeField更新 (Persistent Data)
            SerializedObject so = new SerializedObject(body);
            so.Update(); // Fetch current values (including new OrbitData instance)

            // Body Stats
            so.FindProperty("_massKg").doubleValue = massKg;
            so.FindProperty("_radiusKm").doubleValue = radiusKm;
            so.FindProperty("_soiRadiusKm").doubleValue = 0; // Auto calculate
            
            // Orbit Data (Physics Calculation)
            double M = 0;
            CelestialBody parentBody = parent.GetComponent<CelestialBody>();
            if (parentBody != null) M = parentBody.Mass.Kilograms; // Note: parent might need its Mass initialized if it was just created? 
            // Since we create in order (Earth then Moon), Earth's Mass property might theoretically be 0 if Awake hasn't run?
            // Actually, we set Earth's SerializedProperty. But we also should set its Runtime property for immediate usage by next children.
            
            // Apply logic
            SerializedProperty orbitProp = so.FindProperty("OrbitData");
            orbitProp.FindPropertyRelative("SemiMajorAxis").doubleValue = orbitAxis;

            if (parentBody != null)
            {
                double a = orbitAxis;
                if (a > 0.001)
                {
                     double n = System.Math.Sqrt(PhysicsConstants.GameGravitationalConstant * M / (a * a * a));
                     orbitProp.FindPropertyRelative("MeanMotion").doubleValue = n;
                }
                else
                {
                     orbitProp.FindPropertyRelative("MeanMotion").doubleValue = 0;
                }
            }
            else
            {
                 orbitProp.FindPropertyRelative("MeanMotion").doubleValue = 0;
            }

            so.ApplyModifiedProperties();

            // Runtime Init for immediate use (e.g. by subsequent CreateBody calls relying on Mass)
            body.Mass = new Mass(massKg);
            body.Radius = Distance.FromKilometers(radiusKm);
            body.AbstractGlobalPosition = globalPos;
            
            // 親設定
            body.ParentBody = parentBody;
            
            // Visuals
            SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
            if (sr == null) sr = go.AddComponent<SpriteRenderer>();
            
            if (sr.sprite == null)
            {
                sr.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            }
            sr.color = color;
            body.BodyRenderer = sr;
            
            // Default Scale (Overridden by caller immediately after, but caller sets property directly. 
            // VisualScaleLocal is float, likely standard serialization handles it. 
            // Caller should also set it via SerializedObject or we assume float fields work fine?)
            // Floats on Monobehaviour usually work fine if public. 
            // But strict correctness suggests caller should just set body.VisualScaleLocal.
            // body.VisualScaleLocal is public field. Unity serializes it.
            // But we must mark dirty if we set it directly. 
            // Since CreateBody returns body, and caller sets fields, caller should set dirty or we do it here.
            
            // Let's set defaults here
            body.VisualScaleLocal = 1.0f;
            body.VisualScaleGlobal = 2.0f;
            
            EditorUtility.SetDirty(body); // Ensure public field changes and SO changes trigger save

            return body;
        }

        // Removed outdated SetOrbitFromPhysics helper since logic is integrated above

    }
}
