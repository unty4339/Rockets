using UnityEngine;
using SpaceLogistics.Missions;
using SpaceLogistics.Space;
using SpaceLogistics.Core;

public class OrbitalVerification : MonoBehaviour
{
    public CelestialBody Earth;
    public CelestialBody Moon;

    void Start()
    {
        if (Earth == null || Moon == null)
        {
            Debug.LogError("Assign Earth and Moon references.");
            return;
        }

        Debug.Log("=== Orbital Verification Start ===");
        
        // 1. Create Plan
        double currentTime = 0; // Epoch
        Debug.Log($"Requesting Plan at Time: {currentTime}");
        
        var plan = MissionBuilder.CreatePlanetToMoonPlan(Earth, Moon, currentTime);
        
        Debug.Log($"Plan Created. Segments: {plan.Segments.Count}");

        foreach (var seg in plan.Segments)
        {
            Debug.Log($"Segment: {seg.Trajectory.GetType().Name}, Start: {seg.StartTime:F1}, End: {seg.EndTime:F1}");
            if (seg.Trajectory is KeplerOrbit ko)
            {
                Debug.Log($"  Axis: {ko.Parameters.SemiMajorAxis:E2} m, Ecc: {ko.Parameters.Eccentricity:F4}");
            }
        }

        // 2. Evaluate Trajectory
        // Launch Time (Wait終了後)
        if (plan.Segments.Count > 1)
        {
            var transferSeg = plan.Segments[1];
            double launchTime = transferSeg.StartTime;
            Debug.Log($"Launch Time: {launchTime:F1} (Wait: {launchTime - currentTime:F1}s)");
            
            // Mid-course
            double midTime = (transferSeg.StartTime + transferSeg.EndTime) / 2.0;
            var midState = plan.Evaluate(midTime);
            Debug.Log($"Mid-Course State (Time {midTime:F1}): Pos {midState.state.Position} (Ref: {midState.currentRef.BodyName})");
        }
        
        Debug.Log("=== Verification End ===");
    }
}
