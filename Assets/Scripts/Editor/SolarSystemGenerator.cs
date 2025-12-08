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
            earth.VisualScaleLocal = 4.0f; // Manually set good visual size
            earth.LocalMapRadius = 50.0f;  // Expand map boundary

            // 2. Moon (Orbiting Earth)
            // Distance: ~384,400 km = 384,400,000 meters
            CelestialBody moon = CreateBody("Moon", earth.transform, 
                massKg: 7.342e22, radiusKm: 1737, 
                orbitAxis: 384400000.0, globalPos: Vector3.zero, 
                color: Color.gray);
            moon.VisualScaleLocal = 1.0f; 

            // 3. Mars
            CelestialBody mars = CreateBody("Mars", spaceContainer.transform, 
                massKg: 6.39e23, radiusKm: 3389, 
                orbitAxis: 0, globalPos: new Vector3(10, 0, 0), 
                color: new Color(1f, 0.3f, 0f)); 
            mars.VisualScaleLocal = 2.1f; 

            // 4. Phobos (Orbiting Mars)
            // Distance: ~9,376 km = 9,376,000 meters
            CelestialBody phobos = CreateBody("Phobos", mars.transform, 
                massKg: 1.0659e16, radiusKm: 11, 
                orbitAxis: 9376000.0, globalPos: Vector3.zero, 
                color: new Color(0.6f, 0.4f, 0.2f));
            phobos.VisualScaleLocal = 0.2f;

            // 5. Deimos (Orbiting Mars)
            // Distance: ~23,463 km = 23,463,000 meters
            CelestialBody deimos = CreateBody("Deimos", mars.transform, 
                massKg: 1.4762e15, radiusKm: 6, 
                orbitAxis: 23463000.0, globalPos: Vector3.zero, 
                color: new Color(0.5f, 0.3f, 0.1f));
            deimos.VisualScaleLocal = 0.15f;

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
            
            // SerializeField更新
            SerializedObject so = new SerializedObject(body);
            so.FindProperty("_massKg").doubleValue = massKg;
            so.FindProperty("_radiusKm").doubleValue = radiusKm;
            so.FindProperty("_soiRadiusKm").doubleValue = 0; // Auto calculate
            so.ApplyModifiedProperties();

            // Runtime Init
            body.Mass = new Mass(massKg);
            body.Radius = Distance.FromKilometers(radiusKm);
            body.AbstractGlobalPosition = globalPos;

            // 親設定
            CelestialBody parentBody = parent.GetComponent<CelestialBody>();
            body.ParentBody = parentBody;

            // 軌道設定 (Physics Base)
            SetOrbitFromPhysics(body, parentBody, orbitAxis);

            // Visuals
            SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
            if (sr == null) sr = go.AddComponent<SpriteRenderer>();
            
            if (sr.sprite == null)
            {
                sr.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            }
            sr.color = color;
            body.BodyRenderer = sr;
            
            // Default Scale
            body.VisualScaleLocal = 1.0f; // Default, overriden in main function
            body.VisualScaleGlobal = 2.0f;

            return body;
        }

        private static void SetOrbitFromPhysics(CelestialBody body, CelestialBody parent, double orbitAxis)
        {
             if (body.OrbitData == null) body.OrbitData = new OrbitParameters();
             body.OrbitData.SemiMajorAxis = orbitAxis;

             if (parent != null)
             {
                 // n = sqrt(G * M / a^3)
                 double M = parent.Mass.Kilograms;
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
                 body.OrbitData.MeanMotion = 0; // Sun or orphaned
             }
        }
    }
}
