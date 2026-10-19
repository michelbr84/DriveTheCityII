using System.Collections.Generic;
using DriveTheCityII.Core.Persistence;

namespace DriveTheCityII.Core.Career
{
    /// <summary>Shared lifecycle of SPEC-MISSION section 3 (REQ-CAREER-001).</summary>
    public enum MissionState
    {
        Offered = 0,
        Accepted = 1,
        Active = 2,
        Completed = 3,
        Cancelled = 4,
        Failed = 5,

        /// <summary>Transient: resolves immediately to a no-fault <see cref="Cancelled"/>.</summary>
        Invalidated = 6,

        Expired = 7,
    }

    public enum MissionFault
    {
        None = 0,
        Player = 1,
        NoFault = 2,
    }

    public enum LegKind
    {
        Pickup = 0,
        Dropoff = 1,
        Load = 2,
        Unload = 3,
        Stop = 4,
        Checkpoint = 5,
        Park = 6,
    }

    public enum LegState
    {
        Approaching = 0,
        Servicing = 1,
        Done = 2,
    }

    public enum LegOutcome
    {
        Pending = 0,
        DoneOk = 1,
        DonePartial = 2,
        SkippedNoFault = 3,
        Missed = 4,
        Failed = 5,
    }

    /// <summary>Persisted copy of a place: stable id plus descriptive data, never an entity handle (REQ-SAVE-003).</summary>
    public sealed class PlaceRef
    {
        public string Id;
        public PlaceKind Kind;
        public string Name;
        public MapPoint Access;

        public static PlaceRef From(PlaceInfo p) => p == null ? null : new PlaceRef { Id = p.Id, Kind = p.Kind, Name = p.Name ?? p.Id, Access = p.Access };

        public PlaceRef Clone() => new PlaceRef { Id = Id, Kind = Kind, Name = Name, Access = Access };

        internal JsonValue ToJson() => JsonValue.Object().Set("id", Id).Set("kind", Kind.ToString()).Set("name", Name ?? string.Empty).Set("access", JsonRead.WritePoint(Access));

        internal static PlaceRef FromJson(JsonValue o, string path, JsonRead r)
        {
            if (o == null)
            {
                return null;
            }
            return new PlaceRef
            {
                Id = r.Str(o, "id", path),
                Kind = r.Enum<PlaceKind>(o, "kind", path),
                Name = r.Str(o, "name", path, false) ?? string.Empty,
                Access = r.Point(o, "access", path),
            };
        }
    }

    /// <summary>
    /// A leg (SPEC-MISSION section 4). Legs of the same <see cref="Group"/> may be done in any order; groups are done
    /// in order. A resolved leg is in <see cref="LegState.Done"/> with its <see cref="Outcome"/>.
    /// </summary>
    public sealed class MissionLeg
    {
        public int Index;
        public int Group;
        public LegKind Kind;
        public PlaceRef Place;
        public LegState State;
        public LegOutcome Outcome;
        public bool Required = true;
        public double Radius = CareerBalance.DefaultServiceRadius;
        public double ArrivedAt = -1;
        public double DoneAt = -1;

        /// <summary>Bus timetable target arrival (career time), or -1.</summary>
        public double TargetTime = -1;

        /// <summary>Planned route distance and time from the previous leg (or from the start for the first).</summary>
        public double PlannedMeters;

        public double PlannedSeconds;

        /// <summary>Parcels, pallets or passengers handled at this leg.</summary>
        public int Count;

        /// <summary>Bus: the vehicle came close to this stop (pass detection).</summary>
        public bool WasNear;

        /// <summary>Consecutive route checks that proved this leg unreachable (SPEC-MISSION 7.3 bound: 2).</summary>
        public int FailedChecks;

        // Transient servicing progress: partly finished servicing restarts after load (SPEC-MISSION section 9).
        internal double ServicingStartedAt = -1;
        internal double RequiredSeconds;
        internal int ProgressUnits;

        public bool IsResolved => State == LegState.Done;

        public MissionLeg Clone()
        {
            var l = (MissionLeg)MemberwiseClone();
            l.Place = Place?.Clone();
            return l;
        }

        internal JsonValue ToJson()
        {
            return JsonValue.Object()
                .Set("index", Index)
                .Set("group", Group)
                .Set("kind", Kind.ToString())
                .Set("place", Place.ToJson())
                .Set("state", State.ToString())
                .Set("outcome", Outcome.ToString())
                .Set("required", Required)
                .Set("radius", Radius)
                .Set("arrivedAt", ArrivedAt)
                .Set("doneAt", DoneAt)
                .Set("targetTime", TargetTime)
                .Set("plannedMeters", PlannedMeters)
                .Set("plannedSeconds", PlannedSeconds)
                .Set("count", Count)
                .Set("wasNear", WasNear)
                .Set("failedChecks", FailedChecks);
        }

        internal static MissionLeg FromJson(JsonValue o, string path, JsonRead r)
        {
            var l = new MissionLeg
            {
                Index = r.Int(o, "index", path, 0, 10000),
                Group = r.Int(o, "group", path, 0, 10000),
                Kind = r.Enum<LegKind>(o, "kind", path),
                Place = PlaceRef.FromJson(r.Obj(o, "place", path), path + ".place", r),
                State = r.Enum<LegState>(o, "state", path),
                Outcome = r.Enum<LegOutcome>(o, "outcome", path),
                Required = r.Bool(o, "required", path),
                Radius = r.Num(o, "radius", path, 0.1, 1000),
                ArrivedAt = r.Num(o, "arrivedAt", path, -1),
                DoneAt = r.Num(o, "doneAt", path, -1),
                TargetTime = r.Num(o, "targetTime", path, -1),
                PlannedMeters = r.Num(o, "plannedMeters", path, 0),
                PlannedSeconds = r.Num(o, "plannedSeconds", path, 0),
                Count = r.Int(o, "count", path, 0, 100000),
                WasNear = r.Bool(o, "wasNear", path),
                FailedChecks = r.Int(o, "failedChecks", path, 0, 1000),
            };
            if (l.Place == null || string.IsNullOrEmpty(l.Place.Id))
            {
                r.Error(path + ".place", "leg without a valid place");
            }
            else if (!l.Place.Access.IsFinite)
            {
                r.Error(path + ".place.access", "non-finite access point");
            }
            return l;
        }
    }

    /// <summary>
    /// An accepted or active mission record (SPEC-MISSION section 9: persisted). Exactly one career-specific terms
    /// object is non-null.
    /// </summary>
    public sealed class Mission
    {
        public string Id;
        public CareerKind Career;
        public MissionState State;
        public MissionFault Fault;
        public string EndReasonKey;
        public string VehicleId;
        public List<VehicleClass> AllowedClasses = new List<VehicleClass>();
        public bool UsesServiceVehicle;
        public string ServiceModelId;
        public string ServiceSpawnPlaceId;
        public double AcceptedAt;
        public double ActivatedAt = -1;
        public double StartWindowEnd;
        public double EndedAt = -1;
        public double PlannedMeters;
        public double PlannedSeconds;
        public double ApproachMeters;
        public long ContractValueCents;
        public List<MissionLeg> Legs = new List<MissionLeg>();

        // Shared evaluation accumulators.
        public double Cleanliness = 100;
        public int LightContacts;
        public int MediumContacts;
        public int SevereContacts;
        public int HarshEvents;
        public double DrivenMeters;
        public MapPoint LastVehiclePosition;
        public bool HasVehiclePosition;
        public double AwaySince = -1;
        public double ImmobileSince = -1;
        public double LastWatchdogAt = -1;
        public bool SettlementPending;
        public DeterministicRandom Rng = new DeterministicRandom(1);

        public TaxiTerms Taxi;
        public DeliveryTerms Delivery;
        public FreightTerms Freight;
        public BusTerms Bus;

        // Transient edge detectors for harsh events.
        internal bool HarshLongLatched;
        internal bool HarshLatLatched;

        public bool IsOpen => State == MissionState.Accepted || State == MissionState.Active;

        /// <summary>Index of the first group with an unresolved required leg, or -1 when all are resolved.</summary>
        public int CurrentGroup
        {
            get
            {
                int best = -1;
                foreach (MissionLeg l in Legs)
                {
                    if (!l.IsResolved && l.Required && (best < 0 || l.Group < best))
                    {
                        best = l.Group;
                    }
                }
                return best;
            }
        }

        /// <summary>The first unresolved leg of the current group (for guidance), or null.</summary>
        public MissionLeg CurrentLeg
        {
            get
            {
                int g = CurrentGroup;
                if (g < 0)
                {
                    return null;
                }
                foreach (MissionLeg l in Legs)
                {
                    if (l.Group == g && !l.IsResolved)
                    {
                        return l;
                    }
                }
                return null;
            }
        }

        public bool AllRequiredResolved => CurrentGroup < 0;

        public bool AnyLegDoneOk
        {
            get
            {
                foreach (MissionLeg l in Legs)
                {
                    if (l.IsResolved && (l.Outcome == LegOutcome.DoneOk || l.Outcome == LegOutcome.DonePartial))
                    {
                        return true;
                    }
                }
                return false;
            }
        }
    }

    /// <summary>Result of a career's pure SettlementCalculator (SPEC-MISSION section 8).</summary>
    public sealed class SettlementResult
    {
        public MissionState Outcome;
        public MissionFault Fault;
        public List<LedgerLine> Lines = new List<LedgerLine>();
        public long NetCents;

        /// <summary>Positive fee part included in <see cref="NetCents"/> (cancellation fee).</summary>
        public long FeeCents;

        public long Xp;
        public CareerKind XpCareer;
        public int ReputationDelta;
        public double Evaluation;

        public void AddLine(string key, string text, long cents)
        {
            Lines.Add(new LedgerLine(key, text, cents));
            NetCents += cents;
        }
    }

    /// <summary>Summary of a terminal mission kept in the capped history (SPEC-MISSION section 9).</summary>
    public sealed class MissionHistoryEntry
    {
        public string MissionId;
        public CareerKind Career;
        public MissionState State;
        public MissionFault Fault;
        public string TransactionId;
        public long NetCents;
        public double EndedAt;
        public string ReasonKey;

        internal JsonValue ToJson()
        {
            return JsonValue.Object()
                .Set("id", MissionId)
                .Set("career", Career.ToString())
                .Set("state", State.ToString())
                .Set("fault", Fault.ToString())
                .Set("tx", TransactionId)
                .Set("net", NetCents)
                .Set("endedAt", EndedAt)
                .Set("reason", ReasonKey ?? string.Empty);
        }

        internal static MissionHistoryEntry FromJson(JsonValue o, string path, JsonRead r)
        {
            return new MissionHistoryEntry
            {
                MissionId = r.Str(o, "id", path),
                Career = r.Enum<CareerKind>(o, "career", path),
                State = r.Enum<MissionState>(o, "state", path),
                Fault = r.Enum<MissionFault>(o, "fault", path),
                TransactionId = r.Str(o, "tx", path),
                NetCents = r.Long(o, "net", path),
                EndedAt = r.Num(o, "endedAt", path, 0),
                ReasonKey = r.Str(o, "reason", path, false) ?? string.Empty,
            };
        }
    }

    /// <summary>
    /// A validated offer (state <see cref="MissionState.Offered"/>). Offers are not persisted; they are regenerated
    /// after load (SPEC-MISSION section 9).
    /// </summary>
    public sealed class MissionOffer
    {
        public string Id;
        public CareerKind Career;
        public MissionState State = MissionState.Offered;
        public double CreatedAt;
        public double ExpiresAt;
        public string TitleKey;
        public string Title;
        public MapPoint Origin;
        public double ApproachMeters;
        public double ApproachSeconds;

        /// <summary>Planned (paid) route distance and time, excluding the approach.</summary>
        public double PlannedMeters;

        public double PlannedSeconds;
        public long PayMinCents;
        public long PayMaxCents;
        public long ContractValueCents;
        public long XpPreview;

        /// <summary>Soft deadline relative to acceptance, or -1 when none.</summary>
        public double DeadlineSeconds = -1;

        /// <summary>Taxi patience relative to acceptance, or -1.</summary>
        public double PatienceSeconds = -1;

        public List<VehicleClass> AllowedClasses = new List<VehicleClass>();
        public bool ProvidesServiceVehicle;
        public string ServiceModelId;
        public string ServiceSpawnPlaceId;
        public bool Eligible = true;
        public string IneligibleKey;
        public string IneligibleText;

        // Summary terms for the offer card.
        public int Passengers;
        public TaxiPreference Preference;
        public DeliveryContractType DeliveryType;
        public int ParcelCount;
        public CargoType Cargo;
        public double MassTonnes;
        public string LineId;
        public string LineName;
        public int StopCount;

        internal List<MissionLeg> LegTemplates = new List<MissionLeg>();
        internal TaxiTerms TaxiTemplate;
        internal DeliveryTerms DeliveryTemplate;
        internal FreightTerms FreightTemplate;
        internal BusTerms BusTemplate;

        public IReadOnlyList<MissionLeg> Legs => LegTemplates;
    }
}
