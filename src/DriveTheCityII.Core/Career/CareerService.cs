using System;
using System.Collections.Generic;
using DriveTheCityII.Core.Persistence;

namespace DriveTheCityII.Core.Career
{
    /// <summary>Options for a new career profile. Any non-default value marks the profile "test-seeded" (SPEC-ECON 5.3).</summary>
    public sealed class CareerOptions
    {
        public long InitialBalanceCents = CareerBalance.InitialBalanceCents;
    }

    /// <summary>
    /// Single entry point and single writer of career state (SPEC-MODULES 4.6). Pure logic: no engine types, no
    /// wall-clock, no ambient randomness. The caller supplies career-active seconds to every call (frozen in Building,
    /// pause and menus; SPEC-TIME) and must restore its clock from <see cref="ClockSeconds"/> after loading. Every call
    /// returns its <see cref="CareerEvent"/>s; player-facing text is a localization key plus an English fallback.
    /// </summary>
    public sealed partial class CareerService
    {
        /// <summary>Persisted schema version (SPEC-SAVE section 10). Bump with a migration step on any shape change.</summary>
        public const int SchemaVersion = 1;

        private readonly CareerState m_State;
        private readonly CareerRules[] m_Rules = { new TaxiRules(), new DeliveryRules(), new FreightRules(), new BusRules() };
        private readonly List<MissionOffer> m_Offers = new List<MissionOffer>();
        private ICareerWorld m_World;
        private long m_OffersRevision = long.MinValue;
        private ChallengeRun m_Run;
        private bool m_NeedsRevalidation = true;
        private PlayerMode m_LastMode = PlayerMode.Building;
        private PlayerSnapshot m_LastSnapshot;
        private bool m_HasSnapshot;
        private double m_LastContactAt = double.MinValue;
        private ContactSeverity m_LastContactSeverity;
        private bool m_ClockRegressionReported;
        private EventSink m_Sink = new EventSink();
        private double m_Now;

        private CareerService(CareerState state, ICareerWorld world)
        {
            m_State = state;
            m_World = world;
            m_Now = state.Clock;
        }

        internal CareerState State => m_State;

        internal ICareerWorld World => m_World;

        /// <summary>Career-active clock checkpoint (last time seen). Restore the mod's career clock from it after load.</summary>
        public double ClockSeconds => m_State.Clock;

        public string ProfileId => m_State.ProfileId;

        /// <summary>True when the profile was seeded by a diagnostics-only option (permanent flag).</summary>
        public bool IsTestSeeded => m_State.TestSeeded;

        public bool HasWorld => m_World != null;

        // ------------------------------------------------------------------ construction and persistence

        /// <summary>Creates a new profile with the initial balance (SPEC-ECON section 8) and a seeded generator.</summary>
        public static CareerService CreateNew(string profileId, ulong seed, ICareerWorld world, double nowSeconds = 0, CareerOptions options = null)
        {
            if (string.IsNullOrEmpty(profileId))
            {
                throw new ArgumentException("profileId is required", nameof(profileId));
            }
            var state = new CareerState
            {
                ProfileId = profileId,
                Rng = new DeterministicRandom(seed),
                Clock = CareerMath.IsFinite(nowSeconds) && nowSeconds > 0 ? nowSeconds : 0,
            };
            long initial = CareerBalance.InitialBalanceCents;
            if (options != null && options.InitialBalanceCents != CareerBalance.InitialBalanceCents)
            {
                initial = Math.Max(0, options.InitialBalanceCents);
                state.TestSeeded = true;
            }
            var svc = new CareerService(state, world);
            state.CityRevision = svc.SafeRevision();
            state.Wallet.Append("INIT-" + profileId, LedgerEntryType.InitialBalance, initial, state.Clock, profileId, "DTC.Ledger.InitialBalance", "Initial balance");
            return svc;
        }

        public static CareerService FromJson(JsonValue json, out CareerLoadResult result) => FromJson(json, null, out result);

        /// <summary>
        /// Restores a profile. Invalid data (wallet not equal to the ledger sum, duplicate TransactionIds, an Active
        /// mission without a valid leg, non-finite or out-of-range numbers, ...) is refused as
        /// <see cref="CareerLoadStatus.Quarantined"/> and nothing is loaded. References are revalidated through the
        /// world port on the first non-Building tick.
        /// </summary>
        public static CareerService FromJson(JsonValue json, ICareerWorld world, out CareerLoadResult result)
        {
            CareerState state;
            try
            {
                state = CareerSerializer.Read(json, SchemaVersion, out result);
            }
            catch (Exception ex)
            {
                result = new CareerLoadResult { Status = CareerLoadStatus.Quarantined };
                result.Reasons.Add("unexpected error: " + ex.Message);
                return null;
            }
            if (state == null)
            {
                return null;
            }
            var svc = new CareerService(state, world);
            svc.m_NeedsRevalidation = true;
            return svc;
        }

        /// <summary>Parses JSON text and restores; malformed text is quarantined.</summary>
        public static CareerService FromJsonText(string text, ICareerWorld world, out CareerLoadResult result)
        {
            if (!JsonValue.TryParse(text, out JsonValue json, out string error))
            {
                result = new CareerLoadResult { Status = CareerLoadStatus.Quarantined };
                result.Reasons.Add("parse error: " + error);
                return null;
            }
            return FromJson(json, world, out result);
        }

        /// <summary>Immutable snapshot for Persistence: the whole profile as a JSON DOM (deterministic key order).</summary>
        public JsonValue ToJson() => CareerSerializer.Write(m_State, SchemaVersion);

        public string ToJsonText(bool indented = false) => ToJson().ToJson(indented);

        public void AttachWorld(ICareerWorld world)
        {
            m_World = world;
            m_NeedsRevalidation = true;
        }

        // ------------------------------------------------------------------ frame update

        /// <summary>
        /// Per-frame update: revalidation after city edits or load, fuel and wear of the vehicle in use, mission legs
        /// (arrival = inside the leg radius and below the speed threshold for the dwell time), failure rules and
        /// deadlines, the challenge run, company settlement up to now (settled-until), and offer expiry.
        /// </summary>
        public IReadOnlyList<CareerEvent> Tick(PlayerSnapshot snap, double nowSeconds)
        {
            m_Sink = new EventSink();
            if (!CareerMath.IsFinite(nowSeconds))
            {
                return m_Sink.Events;
            }
            if (nowSeconds < m_State.Clock - 1e-6)
            {
                // Clock regression (SPEC-TIME section 9): no time-based processing until the clock catches up.
                if (!m_ClockRegressionReported)
                {
                    m_ClockRegressionReported = true;
                    m_Sink.Notify(nowSeconds, NotificationLevel.Error, "DTC.Career.ClockRegression", "The career clock went backwards; career timers are paused until it catches up.");
                }
                return m_Sink.Events;
            }
            m_ClockRegressionReported = false;
            m_Now = nowSeconds;
            m_State.Clock = nowSeconds;
            try
            {
                bool returning = m_LastMode == PlayerMode.Building && snap.Mode != PlayerMode.Building;
                if (m_World != null && snap.Mode != PlayerMode.Building && (m_NeedsRevalidation || returning || SafeRevision() != m_State.CityRevision))
                {
                    Revalidate(snap);
                }
                ContactSeverity contact = ClassifyContact(snap, nowSeconds);
                UpdateVehicles(snap, contact, nowSeconds);
                TickMissionContained(snap, contact, nowSeconds);
                TickChallenge(snap, contact, nowSeconds);
                SettleCompany(nowSeconds, CareerBalance.CompanyMaxPeriodsPerCheck);
                ExpireOffers(nowSeconds);
            }
            finally
            {
                m_LastMode = snap.Mode;
                m_LastSnapshot = snap;
                m_HasSnapshot = true;
            }
            return m_Sink.Events;
        }

        /// <summary>Starts a command: fresh event sink, clock never moves backwards.</summary>
        private double Enter(double now)
        {
            m_Sink = new EventSink();
            if (CareerMath.IsFinite(now) && now > m_State.Clock)
            {
                m_State.Clock = now;
            }
            m_Now = m_State.Clock;
            return m_Now;
        }

        private CareerResult Done(CareerResult r)
        {
            r.Events = m_Sink.Events;
            return r;
        }

        private CareerResult Fail(CareerResultCode code, string key, string text) => Done(CareerResult.Fail(code, key, text));

        private ContactSeverity ClassifyContact(PlayerSnapshot s, double now)
        {
            if (s.Mode != PlayerMode.Driving || !CareerMath.IsFinite(s.ImpactSpeed) || s.ImpactSpeed < CareerBalance.MinContactSpeedMps)
            {
                return ContactSeverity.None;
            }
            ContactSeverity sev = s.ImpactSpeed > CareerBalance.SevereContactSpeedMps ? ContactSeverity.Severe : s.ImpactSpeed >= CareerBalance.MediumContactSpeedMps ? ContactSeverity.Medium : ContactSeverity.Light;
            if (now - m_LastContactAt < CareerBalance.ContactDebounceSeconds && sev <= m_LastContactSeverity)
            {
                return ContactSeverity.None;
            }
            m_LastContactAt = now;
            m_LastContactSeverity = sev;
            return sev;
        }

        /// <summary>Fuel, wear and location state of career vehicles from the snapshot (SPEC-GARAGE section 6).</summary>
        private void UpdateVehicles(PlayerSnapshot s, ContactSeverity contact, double now)
        {
            string driving = s.Mode == PlayerMode.Driving ? s.VehicleId : null;
            foreach (CareerVehicle v in m_State.Vehicles)
            {
                if (v.State == VehicleLocationState.InUse && v.Id != driving)
                {
                    v.State = VehicleLocationState.Parked;
                }
            }
            CareerVehicle veh = m_State.FindVehicle(driving);
            if (veh == null || veh.IsRemoved || veh.Assignment == FleetAssignment.Managed)
            {
                return;
            }
            if (!veh.IsImmobilized)
            {
                veh.State = VehicleLocationState.InUse;
            }
            veh.StoredAtPlaceId = null;
            veh.LastPosition = s.Position;
            veh.HasPosition = true;
            double fuelBefore = veh.FuelFraction;
            bool justImmobilized = false;
            double d = s.DistanceDrivenMeters;
            if (CareerMath.IsFinite(d) && d > 0 && d <= CareerBalance.MaxDistancePerSnapshotMeters)
            {
                justImmobilized = veh.ApplyDistance(d, PayloadFraction(veh));
            }
            if (contact != ContactSeverity.None)
            {
                justImmobilized |= veh.ApplyContact(contact);
            }
            if (!veh.FuelIncluded)
            {
                double after = veh.FuelFraction;
                if (fuelBefore > 0.2 && after <= 0.2 && after > 0.1)
                {
                    Notify(NotificationLevel.Warning, "DTC.Garage.FuelLow", "Fuel below 20%.");
                }
                else if (fuelBefore > 0.1 && after <= 0.1 && after > 0)
                {
                    Notify(NotificationLevel.Warning, "DTC.Garage.FuelCritical", "Fuel below 10%. Find a fuel point.");
                }
                else if (fuelBefore > 0 && after <= 0 && !veh.IsImmobilized)
                {
                    Notify(NotificationLevel.Warning, "DTC.Garage.LimpReserve", "Out of fuel: limp reserve active (2 km at 30 km/h).");
                }
            }
            if (justImmobilized)
            {
                m_Sink.Add(new CareerEvent
                {
                    Kind = CareerEventKind.VehicleImmobilized,
                    Time = now,
                    VehicleId = veh.Id,
                    Key = veh.Immobilized == ImmobilizedReason.OutOfFuel ? "DTC.Garage.Immobilized.OutOfFuel" : "DTC.Garage.Immobilized.Breakdown",
                    Text = veh.Immobilized == ImmobilizedReason.OutOfFuel ? "The vehicle ran out of fuel. Call roadside fuel, a tow, or take the free basic rental." : "The vehicle broke down. Call a tow, a roadside quick fix, or take the free basic rental.",
                    Level = NotificationLevel.Warning,
                });
                Hook("Immobilized", "Your vehicle cannot move. The 'Get back to work' panel always offers a free basic rental.");
            }
        }

        private double PayloadFraction(CareerVehicle v)
        {
            Mission m = m_State.Mission;
            if (m == null || m.VehicleId != v.Id || !m.IsOpen)
            {
                return 0;
            }
            if (m.Delivery != null && v.ParcelSlots > 0)
            {
                int slots = 0;
                foreach (Parcel p in m.Delivery.Parcels)
                {
                    if (p.State == ParcelState.Loaded)
                    {
                        slots += p.Size;
                    }
                }
                return (double)slots / v.ParcelSlots;
            }
            if (m.Freight != null && v.PayloadTonnes > 0)
            {
                return m.Freight.LoadedTonnes / v.PayloadTonnes;
            }
            if (m.Bus != null && m.Bus.Capacity > 0)
            {
                return (double)m.Bus.OnboardCount / m.Bus.Capacity;
            }
            return 0;
        }

        // ------------------------------------------------------------------ revalidation (REQ-CITY-002, REQ-CAREER-006)

        private void Revalidate(PlayerSnapshot snap)
        {
            m_NeedsRevalidation = false;
            m_State.CityRevision = SafeRevision();
            foreach (MissionOffer o in m_Offers)
            {
                o.State = MissionState.Expired;
            }
            m_Offers.Clear();
            Mission m = m_State.Mission;
            if (m != null && m.IsOpen && !m.SettlementPending)
            {
                try
                {
                    MapPoint pos = m.HasVehiclePosition ? m.LastVehiclePosition : snap.Position;
                    RulesFor(m.Career).Revalidate(this, m, pos, m_Now);
                    if (IsMissionOpen(m) && m.State == MissionState.Active)
                    {
                        // Force a route check of the current leg on this update (counts as one of the two checks).
                        m.LastWatchdogAt = m_Now - CareerBalance.WatchdogSeconds;
                    }
                }
                catch (Exception ex)
                {
                    ContainMissionFault(ex);
                }
            }
            RevalidateHome();
            RevalidateChallenges();
            RevalidateCompanyHeadquarters();
        }

        // ------------------------------------------------------------------ port wrappers (exceptions contained)

        private long SafeRevision()
        {
            if (m_World == null)
            {
                return 0;
            }
            try
            {
                return m_World.CityRevision;
            }
            catch (Exception)
            {
                return m_State.CityRevision;
            }
        }

        internal RouteEstimate EstimateRoute(MapPoint from, string placeId, VehicleClass cls)
        {
            if (m_World == null || string.IsNullOrEmpty(placeId))
            {
                return RouteEstimate.Unknown();
            }
            try
            {
                return m_World.Estimate(from, placeId, cls);
            }
            catch (Exception)
            {
                return RouteEstimate.Unknown();
            }
        }

        /// <summary>Describe through the port. <paramref name="unknown"/> is true when the port could not answer.</summary>
        internal PlaceInfo DescribePlace(string placeId, out bool unknown)
        {
            unknown = false;
            if (m_World == null || string.IsNullOrEmpty(placeId))
            {
                unknown = true;
                return null;
            }
            try
            {
                PlaceInfo p = m_World.Describe(placeId);
                if (p != null && (string.IsNullOrEmpty(p.Id) || !p.Access.IsFinite))
                {
                    unknown = true;
                    return null;
                }
                return p;
            }
            catch (Exception)
            {
                unknown = true;
                return null;
            }
        }

        private List<PlaceInfo> QueryPlacesRaw(PlaceKind[] kinds, MapPoint near, double radius, int max)
        {
            var result = new List<PlaceInfo>();
            if (m_World == null)
            {
                return result;
            }
            var q = new PlaceQuery(kinds, near, radius, max);
            IReadOnlyList<PlaceInfo> list;
            try
            {
                list = m_World.QueryPlaces(q);
            }
            catch (Exception)
            {
                return result;
            }
            if (list == null)
            {
                return result;
            }
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (PlaceInfo p in list)
            {
                if (p != null && !string.IsNullOrEmpty(p.Id) && p.Access.IsFinite && q.Accepts(p.Kind) && MapPoint.DistanceXZ(p.Access, near) <= radius && seen.Add(p.Id))
                {
                    result.Add(p);
                }
            }
            return result;
        }

        /// <summary>Places sorted by id (ordinal), so generation does not depend on the port's ordering.</summary>
        internal List<PlaceInfo> QueryPlacesSorted(PlaceKind[] kinds, MapPoint near, double radius, int max)
        {
            List<PlaceInfo> list = QueryPlacesRaw(kinds, near, radius, int.MaxValue);
            list.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));
            if (list.Count > max)
            {
                list.RemoveRange(max, list.Count - max);
            }
            return list;
        }

        /// <summary>Places sorted by distance, then id.</summary>
        internal List<PlaceInfo> QueryPlacesByDistance(PlaceKind[] kinds, MapPoint near, double radius, int max)
        {
            List<PlaceInfo> list = QueryPlacesRaw(kinds, near, radius, int.MaxValue);
            list.Sort((a, b) =>
            {
                int c = MapPoint.DistanceXZ(a.Access, near).CompareTo(MapPoint.DistanceXZ(b.Access, near));
                return c != 0 ? c : string.CompareOrdinal(a.Id, b.Id);
            });
            if (list.Count > max)
            {
                list.RemoveRange(max, list.Count - max);
            }
            return list;
        }

        internal List<TransitLineInfo> BusLinesSorted()
        {
            var result = new List<TransitLineInfo>();
            if (m_World == null)
            {
                return result;
            }
            try
            {
                IReadOnlyList<TransitLineInfo> lines = m_World.BusLines();
                if (lines != null)
                {
                    foreach (TransitLineInfo l in lines)
                    {
                        if (l != null && !string.IsNullOrEmpty(l.Id))
                        {
                            result.Add(l);
                        }
                    }
                }
            }
            catch (Exception)
            {
                return new List<TransitLineInfo>();
            }
            result.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));
            return result;
        }

        internal TransitLineInfo FindBusLine(string lineId, out bool unknown)
        {
            unknown = false;
            if (m_World == null)
            {
                unknown = true;
                return null;
            }
            try
            {
                IReadOnlyList<TransitLineInfo> lines = m_World.BusLines();
                if (lines == null)
                {
                    unknown = true;
                    return null;
                }
                foreach (TransitLineInfo l in lines)
                {
                    if (l != null && l.Id == lineId)
                    {
                        return l;
                    }
                }
                return null;
            }
            catch (Exception)
            {
                unknown = true;
                return null;
            }
        }

        // ------------------------------------------------------------------ shared helpers

        internal void Notify(NotificationLevel level, string key, string text, params string[] args)
        {
            m_Sink.Notify(m_Now, level, key, text, args);
        }

        /// <summary>Economic tutorial hooks fire once per trigger per profile (REQ-ECON-005).</summary>
        private void Hook(string name, string text)
        {
            if (m_State.TutorialHooks.Add(name))
            {
                m_Sink.Notify(m_Now, NotificationLevel.Tutorial, "DTC.Tutorial." + name, text);
            }
        }

        internal string NewOperationIdInternal() => "op" + CareerMath.Inv(++m_State.Seq.Operation);

        /// <summary>A fresh operation ID. The UI generates it once per confirmed action so a replay cannot charge twice.</summary>
        public string NewOperationId() => NewOperationIdInternal();

        private void AddDeferred(long cents)
        {
            if (cents > 0)
            {
                m_State.DeferredCents = Math.Min(CareerBalance.DeferredChargeCapCents, m_State.DeferredCents + cents);
            }
        }

        /// <summary>
        /// A mandatory charge (recovery services, return fees): pays what the wallet can and turns the rest into a
        /// capped deferred charge (SPEC-ECON section 7). Never refused. Returns false when the TransactionId already exists.
        /// </summary>
        private bool ChargeMandatory(string tx, LedgerEntryType type, long cost, string source, string key, string text, out long paid, out long deferred)
        {
            paid = 0;
            deferred = 0;
            if (m_State.Wallet.Contains(tx))
            {
                return false;
            }
            cost = Math.Max(0, cost);
            paid = Math.Min(cost, m_State.Wallet.Balance);
            deferred = cost - paid;
            var lines = new List<LedgerLine> { new LedgerLine(key, text, -cost) };
            if (deferred > 0)
            {
                lines.Add(new LedgerLine("DTC.Ledger.Deferred", "Deferred charge", deferred));
            }
            m_State.Wallet.Append(tx, type, -paid, m_Now, source, key, text, lines);
            AddDeferred(deferred);
            if (deferred > 0)
            {
                Hook("DeferredCharge", "You could not pay the full amount; the rest is a deferred charge repaid from future earnings.");
            }
            return true;
        }

        /// <summary>A voluntary purchase: refused when the wallet cannot pay.</summary>
        private LedgerAppendStatus Spend(string tx, LedgerEntryType type, long cost, string source, string key, string text)
        {
            if (m_State.Wallet.Contains(tx))
            {
                return LedgerAppendStatus.AlreadyApplied;
            }
            if (cost > m_State.Wallet.Balance)
            {
                return LedgerAppendStatus.InsufficientFunds;
            }
            LedgerAppendStatus st = m_State.Wallet.Append(tx, type, -cost, m_Now, source, key, text);
            if (st == LedgerAppendStatus.Appended && cost > 0)
            {
                Hook("FirstCost", "Costs such as fuel and repairs come out of your career wallet, never the city budget.");
            }
            return st;
        }

        // ------------------------------------------------------------------ diagnostics-only seeding

        /// <summary>Diagnostics-only: grants career XP. Marks the profile test-seeded permanently.</summary>
        public void DebugSeedXp(CareerKind kind, long xp)
        {
            m_State.TestSeeded = true;
            m_State.Progression.AddXp(kind, xp);
        }

        /// <summary>Diagnostics-only: sets reputation. Marks the profile test-seeded permanently.</summary>
        public void DebugSetReputation(int reputation)
        {
            m_State.TestSeeded = true;
            m_State.Progression.AddReputation(reputation - m_State.Progression.Reputation);
        }

        /// <summary>Diagnostics-only: a Correction ledger entry (+/-). Marks the profile test-seeded permanently.</summary>
        public CareerResult DebugGrant(long cents, double now)
        {
            Enter(now);
            m_State.TestSeeded = true;
            string tx = "CX-" + CareerMath.Inv(SchemaVersion) + "-" + CareerMath.Inv(++m_State.Seq.Correction);
            LedgerAppendStatus st = m_State.Wallet.Append(tx, LedgerEntryType.Correction, cents, m_Now, "diagnostics", "DTC.Ledger.Correction", "Diagnostics correction");
            if (st != LedgerAppendStatus.Appended)
            {
                return Fail(CareerResultCode.InsufficientFunds, "DTC.Result.InsufficientFunds", "The correction would make the wallet negative.");
            }
            CareerResult r = CareerResult.Success();
            r.TransactionId = tx;
            r.AmountCents = cents;
            return Done(r);
        }
    }
}
