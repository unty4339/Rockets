using System;
using UnityEngine;

namespace SpaceLogistics.Core
{
    /// <summary>
    /// 距離を表す物理量。基本単位はメートル(m)。
    /// </summary>
    [Serializable]
    public struct Distance : IComparable<Distance>, IEquatable<Distance>
    {
        public double Meters;

        public Distance(double meters) { Meters = meters; }

        public static Distance FromMeters(double meters) => new Distance(meters);
        public static Distance FromKilometers(double km) => new Distance(km * 1000.0);
        public static Distance FromAU(double au) => new Distance(au * 1.495978707e11);

        public double ToMeters() => Meters;
        public double ToKilometers() => Meters / 1000.0;
        public double ToAU() => Meters / 1.495978707e11;

        public static Distance operator +(Distance a, Distance b) => new Distance(a.Meters + b.Meters);
        public static Distance operator -(Distance a, Distance b) => new Distance(a.Meters - b.Meters);
        public static Distance operator *(Distance a, double b) => new Distance(a.Meters * b);
        public static Distance operator /(Distance a, double b) => new Distance(a.Meters / b);
        public static bool operator >(Distance a, Distance b) => a.Meters > b.Meters;
        public static bool operator <(Distance a, Distance b) => a.Meters < b.Meters;
        public static bool operator >=(Distance a, Distance b) => a.Meters >= b.Meters;
        public static bool operator <=(Distance a, Distance b) => a.Meters <= b.Meters;

        public int CompareTo(Distance other) => Meters.CompareTo(other.Meters);
        public bool Equals(Distance other) => Meters.Equals(other.Meters);
        public override string ToString()
        {
            if (Math.Abs(Meters) >= 1000.0) return $"{ToKilometers():F2} km";
            return $"{Meters:F2} m";
        }
    }

    /// <summary>
    /// 質量を表す物理量。基本単位はキログラム(kg)。
    /// </summary>
    [Serializable]
    public struct Mass : IComparable<Mass>, IEquatable<Mass>
    {
        public double Kilograms;

        public Mass(double kg) { Kilograms = kg; }

        public static Mass FromKilograms(double kg) => new Mass(kg);
        public static Mass FromTonnes(double t) => new Mass(t * 1000.0);

        public double ToKilograms() => Kilograms;
        public double ToTonnes() => Kilograms / 1000.0;

        public static Mass operator +(Mass a, Mass b) => new Mass(a.Kilograms + b.Kilograms);
        public static Mass operator -(Mass a, Mass b) => new Mass(a.Kilograms - b.Kilograms);
        public static Mass operator *(Mass a, double b) => new Mass(a.Kilograms * b);
        
        public int CompareTo(Mass other) => Kilograms.CompareTo(other.Kilograms);
        public bool Equals(Mass other) => Kilograms.Equals(other.Kilograms);
        public override string ToString() => $"{ToTonnes():F2} t";
    }

    /// <summary>
    /// 速度を表す物理量。基本単位はメートル毎秒(m/s)。
    /// </summary>
    [Serializable]
    public struct Speed : IComparable<Speed>, IEquatable<Speed>
    {
        public double MetersPerSecond;

        public Speed(double mps) { MetersPerSecond = mps; }
        
        public override string ToString() => $"{MetersPerSecond:F1} m/s";

        public int CompareTo(Speed other) => MetersPerSecond.CompareTo(other.MetersPerSecond);
        public bool Equals(Speed other) => MetersPerSecond.Equals(other.MetersPerSecond);
        
        public static Speed operator +(Speed a, Speed b) => new Speed(a.MetersPerSecond + b.MetersPerSecond);
        public static Speed operator -(Speed a, Speed b) => new Speed(a.MetersPerSecond - b.MetersPerSecond);
    }
    public static class PhysicsConstants
    {
        // 物理ベースの速度調整用定数 (G)
        // Physics-based speed tuning:
        // Real G = 6.674e-11
        // Adjusted to make Moon period (distance 30) around 60-120 seconds.
        public const double GameGravitationalConstant = 6.674e-11;
    }
}
