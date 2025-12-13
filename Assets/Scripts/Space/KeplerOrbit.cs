using UnityEngine;
using SpaceLogistics.Rocketry;
using System;
using SpaceLogistics.Core;

namespace SpaceLogistics.Space
{
    [System.Serializable]
    public class KeplerOrbit : ITrajectory
    {
        public CelestialBody ReferenceBody { get; private set; }
        public OrbitParameters Parameters;
        
        public double StartTime { get; private set; }
        public double EndTime { get; private set; }

        private double _mu;
        public double Epoch { get; private set; } // 基準時刻

        public KeplerOrbit(CelestialBody body, OrbitParameters paramsData, double start, double end, double epoch = -1.0)
        {
            ReferenceBody = body;
            Parameters = paramsData;
            StartTime = start;
            EndTime = end;

            // epochが指定されていない場合は StartTime をデフォルトとする
            // (指定されている場合はその時刻を基準とする)
            if (epoch < 0) Epoch = start;
            else Epoch = epoch;

             if (body != null)
            {
                double G = PhysicsConstants.GameGravitationalConstant;
                if (body.Mass.Kilograms > 0) 
                    _mu = G * body.Mass.Kilograms;
                else
                    _mu = G * 5.972e24; 
            }
        }

        public OrbitalState Evaluate(double time)
        {
            // Epochからの経過時間を計算
            double dt = time - Epoch; 
            
            double a = Parameters.SemiMajorAxis;
            double e = Parameters.Eccentricity;
            double i = Parameters.Inclination * Mathf.Deg2Rad;
            double omega = Parameters.ArgumentOfPeriapsis * Mathf.Deg2Rad;
            double Omega = Parameters.LongitudeOfAscendingNode * Mathf.Deg2Rad; // 必要なら追加
            double n = Parameters.MeanMotion;
            double M0 = Parameters.MeanAnomalyAtEpoch * Mathf.Deg2Rad;

            double M = M0 + n * dt;

            // 2. Solve Kepler/Hyperbolic
            double E_or_H = 0;
            double nu = 0;
            double r = 0;

            if (e < 1.0)
            {
                E_or_H = OrbitalMath.SolveKepler(M, e);
                nu = OrbitalMath.EccentricToTrueAnomaly(E_or_H, e);
                r = a * (1.0 - e * e) / (1.0 + e * Math.Cos(nu));
            }
            else
            {
                // Hyperbolic logic included directly or via OrbitalMath helper if exists
                // Simplified inline for now as OrbitalMath didn't show hyperbolic helper
                E_or_H = SolveHyperbolic(M, e);
                nu = 2.0 * Math.Atan(Math.Sqrt((e + 1.0) / (e - 1.0)) * Math.Tanh(E_or_H / 2.0));
                r = a * (e * e - 1.0) / (1.0 + e * Math.Cos(nu));
            }

            // 3. Perifocal Coordinates (Orbit Plane)
            // x = r * cos(nu), y = r * sin(nu)
            double px = r * Math.Cos(nu);
            double py = r * Math.Sin(nu);
            
            // Velocity in Perifocal
            // p = a(1-e^2) or a(e^2-1)
            double p = (e < 1.0) ? a * (1.0 - e * e) : a * (e * e - 1.0);
            double sqrtMuP = Math.Sqrt(_mu / p);
            
            double vx = -sqrtMuP * Math.Sin(nu);
            double vy = sqrtMuP * (e + Math.Cos(nu));

            // 4. 3D Rotation to ECI/Reference Frame
            // Order: Omega (LAN) -> i (Inc) -> omega (ArgPeri)
            // But we start from Perifocal, so rotate by -omega, -i, -Omega? No, forward transform.
            // Pos_perifocal = [px, py, 0]
            
            // Rotation Matrix Elements
            double cO = Math.Cos(Omega);
            double sO = Math.Sin(Omega);
            double co = Math.Cos(omega);
            double so = Math.Sin(omega);
            double ci = Math.Cos(i);
            double si = Math.Sin(i);
            
            // Using standard orbital element transformation
            // X = px (cO co - sO so ci) - py (cO so + sO co ci)
            // Y = px (sO co + cO so ci) - py (sO so - cO co ci)
            // Z = px (so si) + py (co si)
            
            // Apply to Position
            Vector3 pos3D = new Vector3(
                (float)(px * (cO * co - sO * so * ci) - py * (cO * so + sO * co * ci)),
                (float)(px * (sO * co + cO * so * ci) - py * (sO * so - cO * co * ci)),
                (float)(px * (so * si) + py * (co * si))
            );

            // Apply to Velocity (same rotation)
            Vector3 vel3D = new Vector3(
                (float)(vx * (cO * co - sO * so * ci) - vy * (cO * so + sO * co * ci)),
                (float)(vx * (sO * co + cO * so * ci) - vy * (sO * so - cO * co * ci)),
                (float)(vx * (so * si) + vy * (co * si))
            );

            return new OrbitalState(pos3D, vel3D, time);
        }

        public Vector3[] GetPathPoints(int resolution)
        {
            Vector3[] points = new Vector3[resolution];
            double step = (EndTime - StartTime) / (resolution - 1);
            if (resolution == 1) step = 0;
            
            for (int i = 0; i < resolution; i++)
            {
                double t = StartTime + step * i;
                points[i] = Evaluate(t).Position;
            }
            return points;
        }

        private double SolveHyperbolic(double M, double e, int maxIter=50, double eps=1e-6)
        {
            double H = M;
            if (e > 1.6) H = (M < 0) ? M - e : M + e;
            else 
            {
               if (Math.Abs(M) < 0.1) H = M / (e - 1.0);
               else if (M > 0) H = Math.Log(2 * M / e + 1.8);
               else H = -Math.Log(2 * Math.Abs(M) / e + 1.8);
            }

            for(int i=0; i<maxIter; i++)
            {
                double f = e * Math.Sinh(H) - H - M;
                double df = e * Math.Cosh(H) - 1.0;
                double H_next = H - f / df;
                if(Math.Abs(H_next - H) < eps) return H_next;
                H = H_next;
            }
            return H;
        }
    }
}
