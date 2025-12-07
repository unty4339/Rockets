using UnityEngine;
using UnityEditor;
using SpaceLogistics.Rocketry;
using System.IO;

namespace SpaceLogistics.Editor
{
    public class RocketPartGenerator
    {
        [MenuItem("SpaceLogistics/Generate Default Parts")]
        public static void GenerateDefaultParts()
        {
            string path = "Assets/Resources/RocketParts";
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            // Command Pod
            CreatePart(path, "CommandPod_Mk1", PartType.CommandModule, 1000f, 0.5f, 0, 0, 0);

            // Fuel Tanks
            CreatePart(path, "FuelTank_Small", PartType.FuelTank, 500f, 0.1f, 0, 0, 2.0f);
            CreatePart(path, "FuelTank_Medium", PartType.FuelTank, 1200f, 0.25f, 0, 0, 5.0f);

            // Engines
            CreatePart(path, "Engine_Basic", PartType.Engine, 1500f, 0.5f, 100f, 280f, 0);
            CreatePart(path, "Engine_Powerful", PartType.Engine, 3000f, 1.0f, 250f, 310f, 0);

            AssetDatabase.Refresh();
            Debug.Log($"Default rocket parts generated in {path}");
        }

        private static void CreatePart(string folder, string name, PartType type, float cost, float massDry, float thrust, float isp, float fuel)
        {
            string fullPath = $"{folder}/{name}.asset";
            
            // Check if exists
            RocketPart part = AssetDatabase.LoadAssetAtPath<RocketPart>(fullPath);
            if (part == null)
            {
                part = ScriptableObject.CreateInstance<RocketPart>();
                AssetDatabase.CreateAsset(part, fullPath);
            }

            part.PartName = name;
            part.Type = type;
            part.Cost = cost;
            part.MassDry = massDry;
            part.Thrust = thrust;
            part.Isp = isp;
            part.FuelCapacity = fuel;

            EditorUtility.SetDirty(part);
        }
    }
}
