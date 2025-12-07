using UnityEngine;
using UnityEditor;
using SpaceLogistics.Missions;

namespace SpaceLogistics.Editor
{
    public class PrefabGenerator
    {
        [MenuItem("SpaceLogistics/Create Active Rocket Prefab")]
        public static void CreateActiveRocketPrefab()
        {
            // 1. Create GameObject
            GameObject go = new GameObject("ActiveRocket_Prefab");
            
            // 2. Add ActiveRocket Component
            ActiveRocket rocket = go.AddComponent<ActiveRocket>();
            
            // 3. Add Sprite Renderer for visual
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            // Use a default knob or ui sprite if available, otherwise just create a white square
            sr.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            sr.color = Color.cyan;
            rocket.Icon = sr;

            // 4. Add Trail Renderer
            TrailRenderer trail = go.AddComponent<TrailRenderer>();
            trail.startWidth = 0.5f;
            trail.endWidth = 0.0f;
            trail.time = 2.0f;
            rocket.Trail = trail;

            // 5. Save as Prefab
            string path = "Assets/Resources";
            if (!System.IO.Directory.Exists(path)) System.IO.Directory.CreateDirectory(path);
            
            path += "/ActiveRocket.prefab";
            PrefabUtility.SaveAsPrefabAsset(go, path);
            
            // 6. Cleanup
            GameObject.DestroyImmediate(go);
            
            Debug.Log($"Created ActiveRocket prefab at {path}");
        }
    }
}
