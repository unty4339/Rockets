using UnityEngine;
using SpaceLogistics.Rocketry; // For OrbitalMath
using System;

namespace SpaceLogistics.Space
{
    [System.Serializable]
    public class KeplerOrbit : ITrajectory
    {
        public CelestialBody ReferenceBody { get; private set; }
        public OrbitParameters Parameters; // 既存のパラメータクラスを再利用（または新設）
        
        public double StartTime { get; private set; }
        public double EndTime { get; private set; }

        // キャッシュ用
        private double _mu;

        public KeplerOrbit(CelestialBody body, OrbitParameters paramsData, double start, double end)
        {
            ReferenceBody = body;
            Parameters = paramsData;
            StartTime = start;
            EndTime = end;

            // μ = GM (standard gravitational parameter)
            // Rocketry.PhysicsConstants はまだ見てないが、ActiveRocketで使っていた定数を確認
            // ActiveRocket: PhysicsConstants.GameGravitationalConstant * root.Mass.Kilograms
            if (body != null)
            {
                 // 注意: PhysicsConstantsへのアクセスが必要。とりあえず定数として持つか、参照するか。
                 // ここではActiveRocketと同じ定数値を使うと仮定。
                 // 本来はSimulationManager的なところから取るべきだが、一旦直接計算。
                 double G = 6.674e-11; // 物理定数クラスがあればそちらを使う
                 // Rocketry.PhysicsTypes.Mass があるので、それを使う
                 _mu = SpaceLogistics.Core.PhysicsConstants.GameGravitationalConstant * body.Mass.Kilograms;
            }
        }

        public OrbitalState Evaluate(double time)
        {
            // 時間のクランプ（範囲外でも計算はできるが、定義上はクランプする？）
            // 軌道予測などで未来を見る場合もあるので、Strictにはしないが、
            // FlightPlanなどでは範囲外アクセスを制御する。
            
            double dt = time - StartTime; 
            // Epochからの経過時間
            double tFromEpoch = time; // OrbitParameters.MeanAnomalyAtEpoch が t=0 定義か、Epoch時刻定義かによる
            // OrbitParametersの実装を見ると:
            // currentMeanAnomaly = MeanAnomalyAtEpoch + MeanMotion * time;
            // となっているので、time は UniverseTime そのものを渡せば良さそう。

            double a = Parameters.SemiMajorAxis;
            double e = Parameters.Eccentricity;
            double i = Parameters.Inclination;
            double w = Parameters.ArgumentOfPeriapsis;
            double n = Parameters.MeanMotion;
            double M0 = Parameters.MeanAnomalyAtEpoch;

            // 1. Mean Anomaly (M)
            double M = M0 + n * time;

            // 2. Eccentric Anomaly (E) or Hyperbolic Anomaly (H)
            double E_or_H = 0;
            double nu = 0;       // True Anomaly
            double r = 0;        // Radius

            if (e < 1.0)
            {
                // Elliptical
                E_or_H = OrbitalMath.SolveKepler(M, e);
                nu = OrbitalMath.EccentricToTrueAnomaly(E_or_H, e);
                r = a * (1.0 - e * e) / (1.0 + e * Math.Cos(nu));
            }
            else
            {
                // Hyperbolic
                // M = e sinh H - H
                // Solve similar to Kepler
                E_or_H = SolveKeplerHyperbolic(M, e);
                nu = HyperbolicToTrueAnomaly(E_or_H, e);
                r = a * (e * e - 1.0) / (1.0 + e * Math.Cos(nu));
            }

            // 3. Position & Velocity in Orbital Plane (Perifocal Frame)
            // p = a(1-e^2) for ellipse, a(e^2-1) for hyperbola
            // h = sqrt(mu * p) : specific angular momentum
            // しかし簡単のため、r と nu から直接計算し、速度はVis-vivaなどから求める
            // 位置: x = r cos(nu), y = r sin(nu)
            // 速度: vx = -sqrt(mu/p)*sin(nu), vy = sqrt(mu/p)*(e + cos(nu))
            
            double p = a * (1.0 - e * e);
            if (e >= 1.0) p = a * (e * e - 1.0);

            // 座標回転 (Argument of Periapsis w)
            // 2D平面 (XY) を前提とする
            double angle = nu + w; // True Anomaly + Arg of Periapsis

            // Position
            Vector3 pos = new Vector3(
                (float)(r * Math.Cos(angle)),
                (float)(r * Math.Sin(angle)),
                0
            );

            // Velocity
            // Radial and Tangential components? Or simply rotate the perifocal velocity.
            // Perifocal Velocity:
            // Vx_perifocal = -sqrt(mu/p) * sin(nu)
            // Vy_perifocal =  sqrt(mu/p) * (e + cos(nu))
            
            double sqrtMuP = Math.Sqrt(_mu / p);
            double vx_peri = -sqrtMuP * Math.Sin(nu);
            double vy_peri =  sqrtMuP * (e + Math.Cos(nu));

            // Rotate velocity by w
            double vx_rot = vx_peri * Math.Cos(w) - vy_peri * Math.Sin(w);
            double vy_rot = vx_peri * Math.Sin(w) + vy_peri * Math.Cos(w);

            Vector3 vel = new Vector3((float)vx_rot, (float)vy_rot, 0);

            return new OrbitalState(pos, vel, time);
        }

        public Vector3[] GetPathPoints(int resolution)
        {
            Vector3[] points = new Vector3[resolution];
            double step = (EndTime - StartTime) / (resolution - 1);
            for (int i = 0; i < resolution; i++)
            {
                double t = StartTime + step * i;
                points[i] = Evaluate(t).Position;
            }
            return points;
        }

        // --- Hyperbolic Math (Internal) ---
        // OrbitalMath に移動しても良いが、一旦ここに実装
        
        private static double SolveKeplerHyperbolic(double M, double e, int maxIter = 100, double epsilon = 1e-6)
        {
            // M = e sinh H - H
            // Initial guess
            double H = M;
            if (e > 1.6) // coarse guess
            {
                if (M < 0) H = M - e;
                else H = M + e;
            }

            for (int i = 0; i < maxIter; i++)
            {
                double f = e * Math.Sinh(H) - H - M;
                double df = e * Math.Cosh(H) - 1.0;
                double nextH = H - f / df;
                if (Math.Abs(nextH - H) < epsilon) return nextH;
                H = nextH;
            }
            return H;
        }

        private static double HyperbolicToTrueAnomaly(double H, double e)
        {
            // tan(nu/2) = sqrt((e+1)/(e-1)) * tanh(H/2)
            double sqrtTerm = Math.Sqrt((e + 1.0) / (e - 1.0));
            double tanhH2 = Math.Tanh(H / 2.0);
            return 2.0 * Math.Atan(sqrtTerm * tanhH2);
        }
    }
}
