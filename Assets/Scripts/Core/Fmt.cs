using System;

namespace Tier9.Core
{
    public static class Fmt
    {
        static readonly string[] Suffixes = { "", "K", "M", "B", "T" };

        public static string N(double v)
        {
            double abs = Math.Abs(v);
            if (abs < 1000) return abs < 10 && v % 1 != 0 ? v.ToString("0.#") : Math.Floor(v).ToString("0");
            int tier = 0;
            while (abs >= 1000 && tier < Suffixes.Length - 1)
            {
                abs /= 1000;
                tier++;
            }
            string num = abs >= 100 ? abs.ToString("0") : abs >= 10 ? abs.ToString("0.#") : abs.ToString("0.##");
            return (v < 0 ? "-" : "") + num + Suffixes[tier];
        }

        public static string Time(double seconds)
        {
            if (seconds < 60) return $"{Math.Floor(seconds)}s";
            if (seconds < 3600) return $"{Math.Floor(seconds / 60)}m {Math.Floor(seconds % 60)}s";
            if (seconds < 86400) return $"{Math.Floor(seconds / 3600)}h {Math.Floor(seconds % 3600 / 60)}m";
            return $"{Math.Floor(seconds / 86400)}d {Math.Floor(seconds % 86400 / 3600)}h";
        }

        public static string PerHour(double perSecond) => N(perSecond * 3600) + "/hr";
    }
}
