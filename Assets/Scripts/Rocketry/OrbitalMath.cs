using System;
using UnityEngine;

namespace SpaceLogistics.Rocketry
{
    public static class OrbitalMath
    {
        /// <summary>
        /// ケプラー方程式 M = E - e sin E を解いて、平均近点角 M から 離心近点角 E を求める。
        /// ニュートン・ラフソン法を使用。
        /// </summary>
        public static double SolveKepler(double meanAnomaly, double eccentricity, int maxIter = 100, double epsilon = 1e-6)
        {
            // M を 0 ~ 2PI に正規化
            meanAnomaly = meanAnomaly % (2.0 * Math.PI);
            if (meanAnomaly < 0) meanAnomaly += 2.0 * Math.PI;

            // 初期推定 (eが小さい前提で M に近い)
            double E = meanAnomaly;
            if (eccentricity > 0.8) E = Math.PI; // 高離心率用

            for (int i = 0; i < maxIter; i++)
            {
                double nextE = E - (E - eccentricity * Math.Sin(E) - meanAnomaly) / (1.0 - eccentricity * Math.Cos(E));
                if (Math.Abs(nextE - E) < epsilon)
                {
                    return nextE;
                }
                E = nextE;
            }
            return E;
        }

        /// <summary>
        /// 離心近点角 E と 離心率 e から 真近点角 nu を計算する。
        /// </summary>
        public static double EccentricToTrueAnomaly(double E, double e)
        {
            // tan(nu/2) = sqrt((1+e)/(1-e)) * tan(E/2)
            double sqrtTerm = Math.Sqrt((1 + e) / (1 - e));
            double tanNu2 = sqrtTerm * Math.Tan(E / 2.0);
            return 2.0 * Math.Atan(tanNu2);
        }
    }
}
