namespace DriveTheCityII.Core.Career
{
    /// <summary>
    /// The single balancing table (SPEC-ECON section 6 principle 7). Every value is an illustrative [Proposal] from
    /// SPEC-ECON section 8 or the career specs, converted to integer cents (1 cr = 100 cents). Values marked
    /// "default" were left open by the specs and are chosen here.
    /// </summary>
    public static class CareerBalance
    {
        // ------------------------------------------------------------------ time (career-active seconds)

        /// <summary>Offer TTL (SPEC-MISSION section 3).</summary>
        public const double OfferTtlSeconds = 300;

        /// <summary>Start window of an Accepted mission (SPEC-MISSION section 3).</summary>
        public const double StartWindowSeconds = 600;

        /// <summary>Route watchdog period for active missions (SPEC-MISSION section 7.3).</summary>
        public const double WatchdogSeconds = 60;

        /// <summary>Consecutive failed checks before a leg/mission is resolved (SPEC-MISSION section 7.3 bound).</summary>
        public const int MaxFailedChecks = 2;

        /// <summary>Generic abandonment: distance and time (SPEC-MISSION section 7.2).</summary>
        public const double AbandonDistanceMeters = 300;

        public const double AbandonSeconds = 180;

        /// <summary>Default servicing speed threshold v (SPEC-MISSION section 4).</summary>
        public const double ServiceSpeedMps = 0.5;

        /// <summary>Default servicing radius R (SPEC-MISSION section 4).</summary>
        public const double DefaultServiceRadius = 15;

        /// <summary>History of terminal missions kept in the save (SPEC-MISSION section 9).</summary>
        public const int MissionHistoryCap = 200;

        /// <summary>Snapshot distance above which a tick is treated as a teleport and not charged (default).</summary>
        public const double MaxDistancePerSnapshotMeters = 1000;

        /// <summary>Contacts closer in time than this are merged unless more severe (default).</summary>
        public const double ContactDebounceSeconds = 0.5;

        /// <summary>Minimum closing speed that counts as a contact (default).</summary>
        public const double MinContactSpeedMps = 0.3;

        /// <summary>Light below 10 km/h, Medium 10-30 km/h, Severe above 30 km/h (SPEC-DRIVING).</summary>
        public const double MediumContactSpeedMps = 10.0 / 3.6;

        public const double SevereContactSpeedMps = 30.0 / 3.6;

        public const double Gravity = 9.81;

        // ------------------------------------------------------------------ wallet

        public const long InitialBalanceCents = 30000;
        public const double BasicRentalShare = 0.25;
        public const long CancellationFeeCapCents = 4000;
        public const double CancellationFeeShare = 0.10;
        public const long DeferredChargeCapCents = 100000;
        public const double DeferredRepaymentShare = 0.25;

        // ------------------------------------------------------------------ XP, reputation

        public const double XpBase = 10;
        public const double XpPerKm = 3;
        public const double XpQualityMin = 0.5;
        public const double XpQualityMax = 1.25;
        public const int ReputationStart = 50;
        public const int ReputationFailed = -5;

        /// <summary>XP needed for levels 1..10 (SPEC-ECON section 8).</summary>
        public static readonly long[] LevelThresholds = { 0, 100, 300, 650, 1150, 1800, 2600, 3600, 4800, 6200 };

        // ------------------------------------------------------------------ taxi (SPEC-TAXI, SPEC-ECON section 8)

        public const long TaxiFlagCents = 2000;
        public const long TaxiPerKmCents = 600;
        public const double TaxiPickupMaxMeters = 1500;
        public const double TaxiTripMinMeters = 800;
        public const double TaxiTripMaxMeters = 6000;
        public const double TaxiTripMaxMetersCap = 10000;
        public const double TaxiTripMaxPerLevelMeters = 500;
        public const double TaxiPatienceSeconds = 360;
        public const double TaxiPatienceHurrySeconds = 240;
        public const double TaxiBoardSeconds = 4;
        public const double TaxiAlightSeconds = 3;
        public const double TaxiLeaveVehicleSeconds = 120;
        public const double TaxiImmobileSeconds = 180;
        public const double TaxiSubstituteRadiusMeters = 400;
        public const int TaxiSevereLimit = 3;

        // ------------------------------------------------------------------ deliveries (SPEC-DELIVERY)

        public const long DeliveryPerParcelCents = 1200;
        public const long DeliveryPerKmCents = 400;
        public const double DeliveryFragileFactor = 1.3;
        public const double DeliveryExpressFactor = 1.4;
        public const double DeliveryLoadSecondsPerParcel = 2;
        public const double DeliveryUnloadSecondsPerParcel = 3;
        public const double DeliveryDropRadius = 20;
        public const int CarParcelSlots = 2;
        public const int VanParcelSlots = 10;
        public const int CargoRackSlots = 2;

        // ------------------------------------------------------------------ freight (SPEC-FREIGHT)

        public const long FreightPerLoadCents = 4000;
        public const long FreightPerTonneKmCents = 300;
        public const double FreightHeavyFactor = 1.2;
        public const double FreightServiceTruckFactor = 0.6;
        public const double FreightDeadlineSlack = 1.6;

        /// <summary>Default: "rear within 3 m of the bay" approximated as vehicle center within 3 m + half length.</summary>
        public const double FreightDockRadius = 8;

        public const double TruckPayloadTonnes = 8;
        public const double PayloadReinforcementTonnes = 1;
        public const double FreightSubstituteRadiusMeters = 1000;

        // ------------------------------------------------------------------ bus (SPEC-BUS)

        public const long BusPerStopCents = 800;
        public const long BusPerPassengerCents = 150;
        public const double BusPunctualityBonusMax = 0.20;
        public const int BusCapacity = 60;
        public const int BusMaxWaitingPerStop = 15;
        public const double BusSecondsPerPassenger = 1.2;
        public const double BusDwellCapSeconds = 40;

        /// <summary>Default minimum dwell so a stop with no passengers still needs a real stop.</summary>
        public const double BusMinDwellSeconds = 3;

        public const double BusStopRadius = 8;
        public const double BusDwellAllowanceSeconds = 15;
        public const double BusEarlyToleranceSeconds = 60;
        public const double BusLateToleranceSeconds = 180;

        /// <summary>Default: a stop approached within this distance and then left behind counts as passed (Missed).</summary>
        public const double BusPassNearMeters = 20;

        public const double BusPassLeaveMeters = 60;
        public const int BusMinStops = 3;

        // ------------------------------------------------------------------ evaluation shared tables

        public const double ContactPenaltyLight = 2;
        public const double ContactPenaltyMedium = 8;
        public const double ContactPenaltySevere = 20;
        public const double HarshPenalty = 1;
        public const double EvalHighMultiplier = 1.15;
        public const double EvalLowMultiplier = 0.85;

        // ------------------------------------------------------------------ garage (SPEC-GARAGE, SPEC-ECON section 8)

        public const double PremiumShiftSeconds = 7200;
        public const double PremiumExtraHourShare = 0.25;
        public const double LimpReserveMeters = 2000;
        public const double LimpSpeedCapMps = 30.0 / 3.6;
        public const double WornSpeedCapMps = 60.0 / 3.6;
        public const double ContactWearLight = 0.5;
        public const double ContactWearMedium = 3;
        public const double ContactWearSevere = 10;
        public const double SaleShare = 0.6;
        public const double SaleUpgradeShare = 0.3;
        public const long TowBaseCents = 4000;
        public const long TowPerKmCents = 200;
        public const long RoadsideFuelFeeCents = 2500;
        public const double RoadsideFuelFraction = 0.25;

        /// <summary>Default: quick fix call fee; the condition points up to 20 are charged at the class rate.</summary>
        public const long RoadsideQuickFixFeeCents = 3000;

        public const double RoadsideQuickFixCondition = 20;

        /// <summary>Default: mobile-service call fee when no fuel or repair point is reachable (SPEC-GARAGE section 9).</summary>
        public const long MobileServiceFeeCents = 1500;

        public static readonly long[] GarageExpansionCents = { 80000, 160000 };
        public static readonly int[] GarageCapacityByLevel = { 2, 4, 6 };

        // ------------------------------------------------------------------ home (SPEC-HOME)

        public const double HomeChangeCooldownSeconds = 600;
        public const double ReturnHomeCooldownSeconds = 60;

        // ------------------------------------------------------------------ company (SPEC-COMPANY)

        public const long CompanyFoundingCents = 300000;
        public const long HiringFeeCents = 15000;
        public const long BaseSalaryCents = 12000;

        /// <summary>Default period length in career-active seconds (OPEN-201: career clock chosen for determinism).</summary>
        public const double CompanyPeriodSeconds = 600;

        public const int CompanyMaxPeriodsPerCheck = 30;
        public const int CompanyCandidatesPerPeriod = 3;
        public const int CompanyContractOffersPerPeriod = 3;
        public const int CompanyArrearsResignPeriods = 3;
        public const double CompanyArrearsReliabilityLoss = 0.10;
        public const double CompanyShortfallThreshold = 0.8;
        public const int CompanyShortfallReputation = -2;
        public const int CompanyDissolveReputation = -10;

        // ------------------------------------------------------------------ challenges (SPEC-CHALLENGE)

        public const double ChallengeCheckpointRadius = 15;
        public const double MedalGold = 0.85;
        public const double MedalSilver = 1.0;
        public const double MedalBronze = 1.2;

        /// <summary>Default one-time cash rewards per tier (bronze, silver, gold).</summary>
        public static readonly long[] ChallengeRewardCents = { 2000, 4000, 8000 };

        /// <summary>Default one-time XP rewards per tier (bronze, silver, gold).</summary>
        public static readonly long[] ChallengeRewardXp = { 10, 20, 40 };

        /// <summary>Default contact time penalties in seconds (light, medium, severe).</summary>
        public static readonly double[] ChallengeContactPenaltySeconds = { 1, 3, 5 };

        public const double ParkingLateralTolerance = 0.4;
        public const double ParkingLongitudinalTolerance = 0.6;
        public const double ParkingHeadingTolerance = 8;
        public const double TruckParkingLateralTolerance = 0.6;
        public const double TruckParkingLongitudinalTolerance = 0.6;
        public const double TruckParkingHeadingTolerance = 10;

        /// <summary>Default reference time and time limit of a parking challenge.</summary>
        public const double ParkingReferenceSeconds = 45;

        public const double ParkingTimeLimitSeconds = 180;
    }
}
