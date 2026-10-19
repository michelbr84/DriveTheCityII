using System;
using System.Collections.Generic;

namespace DriveTheCityII.Core.Career
{
    /// <summary>Unlocks of SPEC-ECON section 5.3 (plus taxi-car purchase, a documented addition).</summary>
    public enum Unlock
    {
        TaxiCareer = 0,
        DeliveryTier1 = 1,
        BasicRental = 2,
        CarPurchase = 3,
        VanRental = 4,
        VanPurchase = 5,
        FragileExpressContracts = 6,
        Freight = 7,
        TruckRentalPurchase = 8,
        BusShifts = 9,
        CompanyFounding = 10,
        TaxiPurchase = 11,
        CompanyOnly = 12,
    }

    /// <summary>
    /// XP per career (levels 1-10), the global reliability reputation (0-100) and unlock rules (REQ-ECON-003).
    /// XP never decreases.
    /// </summary>
    public sealed class Progression
    {
        private readonly long[] m_Xp = new long[4];

        public int Reputation { get; private set; } = CareerBalance.ReputationStart;

        public long Xp(CareerKind kind) => m_Xp[(int)kind];

        public int Level(CareerKind kind) => LevelFor(m_Xp[(int)kind]);

        public int TotalLevel
        {
            get
            {
                int t = 0;
                for (int i = 0; i < m_Xp.Length; i++)
                {
                    t += LevelFor(m_Xp[i]);
                }
                return t;
            }
        }

        public void AddXp(CareerKind kind, long xp)
        {
            if (xp > 0)
            {
                m_Xp[(int)kind] += xp;
            }
        }

        public void AddReputation(int delta)
        {
            Reputation = CareerMath.Clamp(Reputation + delta, 0, 100);
        }

        internal void Restore(long[] xp, int reputation)
        {
            for (int i = 0; i < m_Xp.Length && i < xp.Length; i++)
            {
                m_Xp[i] = Math.Max(0, xp[i]);
            }
            Reputation = CareerMath.Clamp(reputation, 0, 100);
        }

        public static int LevelFor(long xp)
        {
            long[] t = CareerBalance.LevelThresholds;
            int level = 1;
            for (int i = 1; i < t.Length; i++)
            {
                if (xp >= t[i])
                {
                    level = i + 1;
                }
            }
            return level;
        }

        /// <summary>XP needed to reach the next level, or -1 at the cap.</summary>
        public static long NextThreshold(long xp)
        {
            int level = LevelFor(xp);
            return level >= CareerBalance.LevelThresholds.Length ? -1 : CareerBalance.LevelThresholds[level];
        }

        public static long CurrentThreshold(long xp) => CareerBalance.LevelThresholds[LevelFor(xp) - 1];

        /// <summary>Career that receives XP for a vehicle class (challenges, SPEC-ECON section 5.1).</summary>
        public static CareerKind CareerForClass(VehicleClass cls)
        {
            switch (cls)
            {
                case VehicleClass.Van:
                    return CareerKind.Delivery;
                case VehicleClass.RigidTruck:
                    return CareerKind.Freight;
                case VehicleClass.Bus:
                    return CareerKind.Bus;
                default:
                    return CareerKind.Taxi;
            }
        }

        /// <summary>Mission XP = (base + per-km of PLANNED distance) x quality 0.5-1.25 (SPEC-ECON section 5.1).</summary>
        public static long MissionXp(double plannedMeters, double evaluation0To100, double share = 1.0)
        {
            double q = CareerBalance.XpQualityMin + (CareerBalance.XpQualityMax - CareerBalance.XpQualityMin) * CareerMath.Clamp(evaluation0To100, 0, 100) / 100.0;
            double xp = (CareerBalance.XpBase + CareerBalance.XpPerKm * Math.Max(0, plannedMeters) / 1000.0) * q * CareerMath.Clamp(share, 0, 1);
            return (long)Math.Round(xp, MidpointRounding.AwayFromZero);
        }

        /// <summary>Reputation for a completed mission from its evaluation (SPEC-ECON section 5.2).</summary>
        public static int CompletionReputation(double evaluation)
        {
            if (evaluation >= 80)
            {
                return 2;
            }
            return evaluation >= 50 ? 1 : 0;
        }

        /// <summary>Pay multiplier from an evaluation (SPEC-DELIVERY section 7, shared by freight and bus).</summary>
        public static double EvaluationMultiplier(double evaluation)
        {
            if (evaluation >= 80)
            {
                return CareerBalance.EvalHighMultiplier;
            }
            return evaluation >= 50 ? 1.0 : CareerBalance.EvalLowMultiplier;
        }

        public bool IsUnlocked(Unlock unlock, bool companyExists = false)
        {
            switch (unlock)
            {
                case Unlock.TaxiCareer:
                case Unlock.DeliveryTier1:
                case Unlock.BasicRental:
                case Unlock.CarPurchase:
                    return true;
                case Unlock.TaxiPurchase:
                    return Level(CareerKind.Taxi) >= 2;
                case Unlock.VanRental:
                    return Level(CareerKind.Delivery) >= 2;
                case Unlock.VanPurchase:
                case Unlock.FragileExpressContracts:
                    return Level(CareerKind.Delivery) >= 3;
                case Unlock.Freight:
                    return Level(CareerKind.Delivery) >= 3 || TotalLevel >= 6;
                case Unlock.TruckRentalPurchase:
                    return Level(CareerKind.Freight) >= 2;
                case Unlock.BusShifts:
                    return Level(CareerKind.Taxi) >= 3 || Reputation >= 60;
                case Unlock.CompanyFounding:
                    return TotalLevel >= 10 && Reputation >= 60;
                case Unlock.CompanyOnly:
                    return companyExists;
                default:
                    return false;
            }
        }

        /// <summary>Localization key and English text describing an unlock requirement (REQ-UI-003 next step).</summary>
        public static string RequirementText(Unlock unlock)
        {
            switch (unlock)
            {
                case Unlock.TaxiPurchase:
                    return "Requires Taxi level 2";
                case Unlock.VanRental:
                    return "Requires Deliveries level 2";
                case Unlock.VanPurchase:
                case Unlock.FragileExpressContracts:
                    return "Requires Deliveries level 3";
                case Unlock.Freight:
                    return "Requires Deliveries level 3 or a total level of 6";
                case Unlock.TruckRentalPurchase:
                    return "Requires Freight level 2";
                case Unlock.BusShifts:
                    return "Requires Taxi level 3 or reputation 60";
                case Unlock.CompanyFounding:
                    return "Requires a total level of 10 and reputation 60";
                case Unlock.CompanyOnly:
                    return "Available to companies only";
                default:
                    return "Available";
            }
        }

        public static IEnumerable<Unlock> AllUnlocks()
        {
            foreach (Unlock u in Enum.GetValues(typeof(Unlock)))
            {
                yield return u;
            }
        }
    }
}
