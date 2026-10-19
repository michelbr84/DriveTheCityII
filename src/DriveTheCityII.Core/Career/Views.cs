using System.Collections.Generic;

namespace DriveTheCityII.Core.Career
{
    // Read-only view models for the UI (SPEC-MODULES 4.8). They are copies: changing them never changes career state.

    public sealed class LedgerEntryView
    {
        public string TransactionId;
        public LedgerEntryType Type;
        public long AmountCents;
        public long BalanceAfterCents;
        public double Time;
        public string SourceRef;
        public string Key;
        public string Text;
        public List<LedgerLine> Lines = new List<LedgerLine>();
    }

    public sealed class WalletView
    {
        public long BalanceCents;
        public long DeferredCents;
        public int EntryCount;

        /// <summary>Most recent entries first.</summary>
        public List<LedgerEntryView> Recent = new List<LedgerEntryView>();
    }

    public sealed class CareerProgressView
    {
        public CareerKind Career;
        public long Xp;
        public int Level;
        public long LevelStartXp;

        /// <summary>XP of the next level, or -1 at level 10.</summary>
        public long NextLevelXp;

        public double Progress01;
    }

    public sealed class ProgressView
    {
        public List<CareerProgressView> Careers = new List<CareerProgressView>();
        public int Reputation;
        public int TotalLevel;
        public List<Unlock> Unlocked = new List<Unlock>();
        public List<Unlock> Locked = new List<Unlock>();
        public bool TestSeeded;
    }

    public sealed class MissionLegView
    {
        public int Index;
        public int Group;
        public LegKind Kind;
        public LegState State;
        public LegOutcome Outcome;
        public string PlaceId;
        public string PlaceName;
        public MapPoint Access;
        public double Radius;
        public int Count;
        public double TargetTime;
    }

    public sealed class ActiveMissionView
    {
        public string MissionId;
        public CareerKind Career;
        public MissionState State;
        public string VehicleId;
        public List<VehicleClass> AllowedClasses = new List<VehicleClass>();
        public int CurrentLegIndex = -1;
        public LegKind CurrentLegKind;
        public LegState CurrentLegState;
        public string TargetPlaceId;
        public string TargetPlaceName;
        public MapPoint TargetAccess;

        /// <summary>Straight-line distance hint from the last known vehicle (or player) position, -1 when unknown.</summary>
        public double DistanceHintMeters = -1;

        public double ServicingProgress01;
        public double PlannedMeters;
        public long ContractValueCents;

        /// <summary>Seconds left before the start window / patience / soft / hard deadline; -1 when not applicable.</summary>
        public double StartWindowRemaining = -1;

        public double PatienceRemaining = -1;
        public double SoftDeadlineRemaining = -1;
        public double HardDeadlineRemaining = -1;

        public PassengerState PassengerState;
        public int Passengers;
        public double RideQuality = -1;
        public int ParcelsTotal;
        public int ParcelsLoaded;
        public int ParcelsDelivered;
        public double WorstParcelIntegrity = -1;
        public double LoadIntegrity = -1;
        public double PayloadTonnes;
        public string LineName;
        public int BusOnboard;
        public int BusWaitingAtNextStop;
        public int StopsServed;
        public int StopsMissed;
        public double Evaluation;
        public bool SettlementPending;
        public List<MissionLegView> Legs = new List<MissionLegView>();
    }

    public sealed class VehicleView
    {
        public string Id;
        public string ModelId;
        public string Name;
        public string PrefabCategory;
        public VehicleClass Class;
        public VehicleKind Kind;
        public VehicleHolder Holder;
        public VehicleLocationState State;
        public ImmobilizedReason Immobilized;
        public double FuelPercent;
        public double ConditionPercent;
        public double OdometerKm;
        public List<UpgradeKind> Upgrades = new List<UpgradeKind>();
        public VehiclePerformance Performance;
        public FleetAssignment Assignment;

        /// <summary>"On managed contract (simulated)" badge text for managed company vehicles, else null.</summary>
        public string ManagedBadge;

        public bool InCurrentMission;
        public long SaleValueCents;
        public string StoredAtPlaceId;
        public MapPoint LastPosition;
        public bool HasPosition;
    }

    public sealed class EmployeeView
    {
        public string Id;
        public string Name;
        public int Skill;
        public double Reliability;
        public long SalaryCents;
        public string VehicleId;
        public string ContractId;
        public int UnpaidPeriods;
    }

    public sealed class CompanyContractView
    {
        public string Id;
        public CareerKind Career;
        public VehicleClass RequiredClass;
        public int QuotaPerPeriod;
        public long PayPerJobCents;
        public int RemainingPeriods;
        public double MinReliability;
        public long PenaltyPerMissingJobCents;
        public long SimulatedJobsDone;
        public long JobsExpected;
    }

    /// <summary>Company summary. Always presented as "Managed operations (simulated)" (REQ-COMPANY-002).</summary>
    public sealed class CompanyView
    {
        public bool Exists;
        public string Id;
        public string Name;
        public string HeadingKey = CareerService.CompanyHeadingKey;
        public string Heading = CareerService.CompanyHeadingText;
        public string ExplanationKey = CareerService.CompanyExplanationKey;
        public string Explanation = CareerService.CompanyExplanationText;
        public long CashCents;
        public long DebtCents;
        public bool InArrears;
        public int ArrearsPeriods;
        public long LastSettledPeriod;
        public double SettledUntil;
        public double NextSettlementAt;
        public long SimulatedJobsTotal;
        public bool AutoMaintenance;
        public string HeadquartersPlaceId;
        public bool HeadquartersMissing;
        public List<EmployeeView> Employees = new List<EmployeeView>();
        public List<CompanyContractView> Contracts = new List<CompanyContractView>();
        public List<string> FleetVehicleIds = new List<string>();
        public List<LedgerEntryView> RecentLedger = new List<LedgerEntryView>();
    }

    public sealed class ChallengeView
    {
        public string Id;
        public string Name;
        public ChallengeType Type;
        public VehicleClass Class;
        public bool Available;
        public string UnavailableKey;
        public int RouteVersion;
        public double ReferenceSeconds;
        public double BestSeconds = -1;
        public MedalTier Medal;
        public int Attempts;

        /// <summary>A record exists only for an older route version ("Route changed since this record").</summary>
        public bool HasOlderRecords;

        public List<PlaceRef> Points = new List<PlaceRef>();
        public bool Running;
        public int NextPoint;

        /// <summary>Running challenge only: true once the timer started (after the start point).</summary>
        public bool RunStarted;

        /// <summary>Running challenge only: career-clock seconds since the timer started, and penalty seconds so far.</summary>
        public double RunElapsedSeconds;
        public double RunPenaltySeconds;
    }

    public sealed class HomeView
    {
        public bool HasHome;
        public string HomePlaceId;
        public string HomeName;
        public MapPoint HomeAccess;
        public bool TemporaryBase;
        public bool HasGarage;
        public string GaragePlaceId;
        public string GarageName;
        public MapPoint GarageAccess;
        public int GarageCapacity;
        public int StoredVehicles;
    }

    public enum RecoveryOptionKind
    {
        TakeBasicRental = 0,
        StarterJob = 1,
        RoadsideFuel = 2,
        RoadsideQuickFix = 3,
        Tow = 4,
        MobileRefuel = 5,
        MobileRepair = 6,
        SellVehicle = 7,
        ReturnRental = 8,
    }

    public sealed class RecoveryOption
    {
        public RecoveryOptionKind Kind;
        public string VehicleId;

        /// <summary>Cost in cents (negative = income, e.g. a sale).</summary>
        public long CostCents;

        /// <summary>Unpaid cost becomes a deferred charge instead of a refusal.</summary>
        public bool CanDefer;

        public string Key;
        public string Text;
    }

    /// <summary>The "Get back to work" panel (SPEC-ECON section 7).</summary>
    public sealed class RecoveryView
    {
        public bool Insolvent;
        public bool HasUsableVehicle;
        public long BalanceCents;
        public long DeferredCents;
        public List<RecoveryOption> Options = new List<RecoveryOption>();
    }
}
