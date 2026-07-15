using System;

namespace Tier9.Sim
{
    public static class XpCurve
    {
        /// <summary>XP needed to go from `level` to `level + 1`.</summary>
        public static double ToNext(int level)
        {
            return Math.Floor(30.0 * Math.Pow(level, 1.9) + 20.0 * level);
        }

        /// <summary>XP needed to go from skill `level` to `level + 1` (slightly cheaper than class levels).</summary>
        public static double SkillToNext(int level)
        {
            return Math.Floor(20.0 * Math.Pow(level, 1.85) + 15.0 * level);
        }
    }
}
