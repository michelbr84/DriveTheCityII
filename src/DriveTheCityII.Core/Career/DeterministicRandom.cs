using System;
using System.Collections.Generic;
using System.Globalization;

namespace DriveTheCityII.Core.Career
{
    /// <summary>
    /// Seeded SplitMix64 generator whose whole state is one 64-bit word, persisted as hex text (JSON numbers are doubles
    /// and cannot hold 64 bits). Career never uses <see cref="System.Random"/> or wall-clock time (SPEC-MODULES 4.6).
    /// </summary>
    public sealed class DeterministicRandom
    {
        private ulong m_State;

        public DeterministicRandom(ulong seed)
        {
            m_State = seed;
        }

        public ulong State => m_State;

        public string StateHex => m_State.ToString("x16", CultureInfo.InvariantCulture);

        public static bool TryParseState(string hex, out ulong state)
        {
            state = 0;
            if (string.IsNullOrEmpty(hex) || hex.Length > 16)
            {
                return false;
            }
            return ulong.TryParse(hex, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out state);
        }

        public ulong NextULong()
        {
            unchecked
            {
                m_State += 0x9E3779B97F4A7C15UL;
                ulong z = m_State;
                z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
                z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
                return z ^ (z >> 31);
            }
        }

        /// <summary>Uniform integer in [0, maxExclusive). Returns 0 when maxExclusive is 1 or less.</summary>
        public int NextInt(int maxExclusive)
        {
            if (maxExclusive <= 1)
            {
                return 0;
            }
            return (int)(NextULong() % (ulong)maxExclusive);
        }

        /// <summary>Uniform integer in [minInclusive, maxInclusive].</summary>
        public int NextRange(int minInclusive, int maxInclusive)
        {
            if (maxInclusive <= minInclusive)
            {
                return minInclusive;
            }
            return minInclusive + NextInt(maxInclusive - minInclusive + 1);
        }

        /// <summary>Uniform double in [0, 1).</summary>
        public double NextDouble() => (NextULong() >> 11) * (1.0 / 9007199254740992.0);

        public double NextRange(double min, double max) => min + (max - min) * NextDouble();

        /// <summary>Fisher-Yates shuffle (deterministic for a given state).</summary>
        public void Shuffle<T>(IList<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = NextInt(i + 1);
                T t = list[i];
                list[i] = list[j];
                list[j] = t;
            }
        }

        /// <summary>A generator seeded from several stable parts, for example (CompanyId, PeriodIndex, EmployeeId).</summary>
        public static DeterministicRandom FromParts(params string[] parts)
        {
            ulong h = StableHash.Fnv1a64(string.Empty);
            for (int i = 0; i < parts.Length; i++)
            {
                h = StableHash.Combine(h, StableHash.Fnv1a64(parts[i] ?? string.Empty));
            }
            return new DeterministicRandom(h);
        }
    }

    /// <summary>Stable 64-bit hashing (FNV-1a). Never use string.GetHashCode for persisted or seeded values.</summary>
    public static class StableHash
    {
        public static ulong Fnv1a64(string s)
        {
            unchecked
            {
                ulong h = 14695981039346656037UL;
                if (s != null)
                {
                    for (int i = 0; i < s.Length; i++)
                    {
                        char c = s[i];
                        h ^= (byte)(c & 0xFF);
                        h *= 1099511628211UL;
                        h ^= (byte)(c >> 8);
                        h *= 1099511628211UL;
                    }
                }
                return h;
            }
        }

        public static ulong Combine(ulong a, ulong b)
        {
            unchecked
            {
                ulong z = a ^ (b + 0x9E3779B97F4A7C15UL + (a << 6) + (a >> 2));
                z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
                z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
                return z ^ (z >> 31);
            }
        }

        public static string Hex(ulong h) => h.ToString("x16", CultureInfo.InvariantCulture);
    }
}
