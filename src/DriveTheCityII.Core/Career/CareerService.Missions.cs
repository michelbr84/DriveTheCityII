using System;
using System.Collections.Generic;

namespace DriveTheCityII.Core.Career
{
    public sealed partial class CareerService
    {
        internal CareerRules RulesFor(CareerKind kind) => m_Rules[(int)kind];

        internal bool IsMissionOpen(Mission m) => m != null && m_State.Mission == m && m.IsOpen && !m.SettlementPending;

        internal MissionOffer NewOffer(CareerKind career, MapPoint near, double now)
        {
            return new MissionOffer
            {
                Id = "O" + CareerMath.Inv(++m_State.Seq.Offer),
                Career = career,
                CreatedAt = now,
                ExpiresAt = now + CareerBalance.OfferTtlSeconds,
                Origin = near,
            };
        }

        internal bool PlayerHasClass(VehicleClass cls)
        {
            foreach (CareerVehicle v in m_State.Vehicles)
            {
                if (!v.IsRemoved && v.Class == cls && v.Kind != VehicleKind.Service && v.Assignment != FleetAssignment.Managed)
                {
                    return true;
                }
            }
            return false;
        }

        internal double BestPayloadTonnes()
        {
            double best = CareerBalance.TruckPayloadTonnes;
            foreach (CareerVehicle v in m_State.Vehicles)
            {
                if (!v.IsRemoved && v.Class == VehicleClass.RigidTruck && v.Kind != VehicleKind.Service)
                {
                    best = Math.Max(best, v.PayloadTonnes);
                }
            }
            return best;
        }

        private static int OfferCount(CareerKind kind) => kind == CareerKind.Freight ? 3 : 4;

        // ------------------------------------------------------------------ offers

        /// <summary>
        /// Validated offers near a point (the eligible vehicle's position, or the road point nearest the player on foot).
        /// Offers whose route estimate is Unknown or Unreachable are never published (REQ-CAREER-002); every leg uses a
        /// place access point (REQ-CAREER-003). Offers are cached until they expire or the city changes.
        /// </summary>
        public IReadOnlyList<MissionOffer> GetOffers(CareerKind career, MapPoint near, double nowSeconds)
        {
            double now = Enter(nowSeconds);
            var result = new List<MissionOffer>();
            if (m_World == null || !near.IsFinite)
            {
                return result;
            }
            CareerRules rules = RulesFor(career);
            if (!m_State.Progression.IsUnlocked(rules.RequiredUnlock))
            {
                return result;
            }
            ExpireOffers(now);
            long rev = SafeRevision();
            if (rev != m_OffersRevision)
            {
                foreach (MissionOffer o in m_Offers)
                {
                    o.State = MissionState.Expired;
                }
                m_Offers.Clear();
                m_OffersRevision = rev;
            }
            foreach (MissionOffer o in m_Offers)
            {
                if (o.Career == career && o.State == MissionState.Offered)
                {
                    result.Add(o);
                }
            }
            if (result.Count == 0)
            {
                List<MissionOffer> generated;
                try
                {
                    generated = rules.Generate(this, near, now, OfferCount(career));
                }
                catch (Exception ex)
                {
                    Notify(NotificationLevel.Error, "DTC.Career.OfferError", "Offers could not be generated: " + ex.Message);
                    generated = new List<MissionOffer>();
                }
                m_Offers.AddRange(generated);
                result.AddRange(generated);
            }
            return result;
        }

        /// <summary>Discards the career's current offers (board refresh) and generates new ones.</summary>
        public IReadOnlyList<MissionOffer> RefreshOffers(CareerKind career, MapPoint near, double nowSeconds)
        {
            for (int i = m_Offers.Count - 1; i >= 0; i--)
            {
                if (m_Offers[i].Career == career)
                {
                    m_Offers[i].State = MissionState.Expired;
                    m_Offers.RemoveAt(i);
                }
            }
            return GetOffers(career, near, nowSeconds);
        }

        private void ExpireOffers(double now)
        {
            for (int i = m_Offers.Count - 1; i >= 0; i--)
            {
                if (now > m_Offers[i].ExpiresAt)
                {
                    m_Offers[i].State = MissionState.Expired;
                    m_Offers.RemoveAt(i);
                }
            }
        }

        private MissionOffer FindOffer(string id)
        {
            foreach (MissionOffer o in m_Offers)
            {
                if (o.Id == id)
                {
                    return o;
                }
            }
            return null;
        }

        /// <summary>Taxi duty toggle (persisted, SPEC-TAXI section 9).</summary>
        public void SetTaxiDuty(bool on)
        {
            m_State.TaxiDuty = on;
        }

        public bool TaxiDuty => m_State.TaxiDuty;

        // ------------------------------------------------------------------ accept

        /// <summary>
        /// Accepts an offer (Offered to Accepted). Access points and connectivity are revalidated; a failure withdraws the
        /// offer with no consequence (SPEC-MISSION section 5). <paramref name="vehicleId"/> may be null: the mission then
        /// becomes Active when the player drives any eligible vehicle. Service vehicles (bus, starter freight) are
        /// provided here and announced with <see cref="CareerEventKind.ServiceVehicleProvided"/>.
        /// </summary>
        public CareerResult Accept(string offerId, string vehicleId, double nowSeconds)
        {
            double now = Enter(nowSeconds);
            if (m_State.Mission != null)
            {
                return Fail(CareerResultCode.MissionAlreadyActive, "DTC.Mission.AlreadyActive", "Finish or cancel your current job first.");
            }
            if (m_Run != null)
            {
                return Fail(CareerResultCode.InvalidState, "DTC.Challenge.RunActive", "Finish or abort the challenge first.");
            }
            if (m_World == null)
            {
                return Fail(CareerResultCode.NoWorld, "DTC.Career.NoWorld", "City data is not available.");
            }
            ExpireOffers(now);
            MissionOffer o = FindOffer(offerId);
            if (o == null || o.State != MissionState.Offered)
            {
                return Fail(CareerResultCode.OfferExpired, "DTC.Mission.OfferExpired", "This offer is no longer available.");
            }
            if (!o.Eligible)
            {
                return Fail(CareerResultCode.Locked, o.IneligibleKey ?? "DTC.Mission.Ineligible", o.IneligibleText ?? "You cannot take this job yet.");
            }
            CareerRules rules = RulesFor(o.Career);
            if (!m_State.Progression.IsUnlocked(rules.RequiredUnlock))
            {
                return Fail(CareerResultCode.Locked, "DTC.Mission.Locked", Progression.RequirementText(rules.RequiredUnlock));
            }

            var mission = new Mission
            {
                Id = "M" + CareerMath.Inv(m_State.Seq.Mission + 1) + "-" + StableHash.Hex(m_State.Rng.NextULong()).Substring(0, 6),
                Career = o.Career,
                State = MissionState.Accepted,
                AcceptedAt = now,
                StartWindowEnd = now + CareerBalance.StartWindowSeconds,
                PlannedMeters = o.PlannedMeters,
                PlannedSeconds = o.PlannedSeconds,
                ApproachMeters = o.ApproachMeters,
                ContractValueCents = o.ContractValueCents,
                AllowedClasses = new List<VehicleClass>(o.AllowedClasses),
                Rng = new DeterministicRandom(m_State.Rng.NextULong()),
                Taxi = o.TaxiTemplate?.Clone(),
                Delivery = o.DeliveryTemplate?.Clone(),
                Freight = o.FreightTemplate?.Clone(),
                Bus = o.BusTemplate?.Clone(),
            };
            foreach (MissionLeg l in o.LegTemplates)
            {
                mission.Legs.Add(l.Clone());
            }

            // Quick revalidation of access points and connectivity (SPEC-MISSION section 5, on acceptance).
            if (!RevalidateForAccept(mission, o))
            {
                o.State = MissionState.Expired;
                m_Offers.Remove(o);
                return Fail(CareerResultCode.OfferWithdrawn, "DTC.Mission.OfferWithdrawn", "Offer no longer available.");
            }

            CareerVehicle chosen = null;
            if (!string.IsNullOrEmpty(vehicleId))
            {
                chosen = m_State.FindVehicle(vehicleId);
                if (chosen == null || chosen.IsRemoved)
                {
                    return Fail(CareerResultCode.NotFound, "DTC.Garage.VehicleNotFound", "That vehicle is not available.");
                }
                if (!CheckEligible(mission, chosen, out CareerResultCode code, out string text))
                {
                    return Fail(code, "DTC.Mission.VehicleIneligible", text);
                }
            }

            m_State.Seq.Mission++;
            rules.OnAccepted(this, mission, now);
            m_State.Mission = mission;
            m_Offers.Remove(o);
            o.State = MissionState.Accepted;

            m_Sink.Add(new CareerEvent { Kind = CareerEventKind.MissionStateChanged, Time = now, MissionId = mission.Id, Career = mission.Career, FromState = MissionState.Offered, ToState = MissionState.Accepted, Key = "DTC.Mission.Accepted", Text = "Job accepted." });

            if (o.ProvidesServiceVehicle && (chosen == null || chosen.Kind == VehicleKind.Service))
            {
                CareerVehicle sv = CreateServiceVehicle(o.ServiceModelId, now);
                mission.UsesServiceVehicle = true;
                mission.ServiceModelId = o.ServiceModelId;
                mission.ServiceSpawnPlaceId = o.ServiceSpawnPlaceId;
                mission.VehicleId = sv.Id;
                m_Sink.Add(new CareerEvent { Kind = CareerEventKind.ServiceVehicleProvided, Time = now, MissionId = mission.Id, VehicleId = sv.Id, PlaceId = o.ServiceSpawnPlaceId, Key = "DTC.Garage.ServiceVehicle", Text = "A service vehicle is waiting for you." });
            }
            else
            {
                mission.VehicleId = chosen?.Id;
            }
            CareerResult r = CareerResult.Success(mission.Id);
            r.PlaceId = mission.ServiceSpawnPlaceId;
            return Done(r);
        }

        private bool RevalidateForAccept(Mission m, MissionOffer o)
        {
            MapPoint from = m_HasSnapshot && m_LastSnapshot.Mode != PlayerMode.Building ? m_LastSnapshot.Position : o.Origin;
            VehicleClass cls = m.AllowedClasses.Count > 0 ? m.AllowedClasses[0] : VehicleClass.Car;
            MapPoint prev = from;
            int prevGroup = -1;
            MapPoint groupStart = from;
            foreach (MissionLeg leg in m.Legs)
            {
                PlaceInfo p = DescribePlace(leg.Place.Id, out bool unknown);
                if (unknown || p == null)
                {
                    return false;
                }
                leg.Place.Access = p.Access;
                if (leg.Group != prevGroup)
                {
                    groupStart = prev;
                    prevGroup = leg.Group;
                }
                // Unordered groups: each leg must be reachable from the end of the previous group.
                RouteEstimate e = EstimateRoute(m.Career == CareerKind.Bus ? prev : groupStart, leg.Place.Id, cls);
                if (!e.IsReachable)
                {
                    return false;
                }
                prev = leg.Place.Access;
            }
            return true;
        }

        private bool CheckEligible(Mission m, CareerVehicle v, out CareerResultCode code, out string text)
        {
            code = CareerResultCode.Ok;
            text = null;
            if (v.IsRemoved)
            {
                code = CareerResultCode.NotFound;
                text = "That vehicle is not available.";
                return false;
            }
            if (v.Assignment == FleetAssignment.Managed)
            {
                code = CareerResultCode.InvalidState;
                text = "This vehicle is on a managed contract (simulated). Unassign it first.";
                return false;
            }
            if (v.Kind == VehicleKind.Service && m.VehicleId != v.Id)
            {
                code = CareerResultCode.InvalidState;
                text = "Service vehicles belong to their own shift.";
                return false;
            }
            if (!m.AllowedClasses.Contains(v.Class))
            {
                code = CareerResultCode.WrongVehicleClass;
                text = "This job needs a " + string.Join(" or ", m.AllowedClasses.ConvertAll(c => c.ToString()).ToArray()) + ".";
                return false;
            }
            if (v.IsImmobilized)
            {
                code = CareerResultCode.VehicleImmobilized;
                text = "This vehicle cannot move. Refuel, repair or take the basic rental.";
                return false;
            }
            if (!RulesFor(m.Career).CheckCapacity(m, v, out string reason))
            {
                code = CareerResultCode.CapacityExceeded;
                text = reason;
                return false;
            }
            return true;
        }

        private CareerVehicle CreateServiceVehicle(string modelId, double now)
        {
            VehicleModel model = CareerCatalog.Find(modelId) ?? CareerCatalog.Find(CareerCatalog.ServiceBusModelId);
            var v = new CareerVehicle
            {
                Id = "SV" + CareerMath.Inv(++m_State.Seq.Vehicle),
                ModelId = model.Id,
                Class = model.Class,
                Kind = VehicleKind.Service,
                Holder = VehicleHolder.Player,
                State = VehicleLocationState.Parked,
                FuelLiters = model.TankLiters,
                Condition = 100,
                AcquiredAt = now,
            };
            m_State.Vehicles.Add(v);
            return v;
        }

        /// <summary>A service vehicle that became immobilized is replaced by its operator; no fault (SPEC-GARAGE 8).</summary>
        private CareerVehicle ReplaceServiceVehicle(Mission m, CareerVehicle old, double now)
        {
            CareerVehicle sv = CreateServiceVehicle(old.ModelId, now);
            sv.LastPosition = old.LastPosition;
            sv.HasPosition = old.HasPosition;
            old.State = VehicleLocationState.Removed;
            m.VehicleId = sv.Id;
            m_Sink.Add(new CareerEvent { Kind = CareerEventKind.VehicleReleased, Time = now, VehicleId = old.Id, Key = "DTC.Garage.ServiceVehicleCollected", Text = "The operator collected the disabled vehicle." });
            m_Sink.Add(new CareerEvent { Kind = CareerEventKind.ServiceVehicleProvided, Time = now, MissionId = m.Id, VehicleId = sv.Id, Key = "DTC.Garage.ServiceVehicleReplaced", Text = "The operator delivered a replacement vehicle. Cargo and passengers were transferred." });
            return sv;
        }

        // ------------------------------------------------------------------ cancel, abandon, recovery

        /// <summary>
        /// Player cancellation of an Accepted or Active mission (REQ-CAREER-005). Consequences follow the career spec
        /// (fees never push the wallet below zero; any excess becomes a deferred charge).
        /// </summary>
        public CareerResult Cancel(string reason, double nowSeconds)
        {
            Enter(nowSeconds);
            Mission m = m_State.Mission;
            if (m == null || !m.IsOpen || m.SettlementPending)
            {
                return Fail(CareerResultCode.NotFound, "DTC.Mission.None", "There is no job to cancel.");
            }
            string tx = SettlementTxId(m.Id, MissionState.Cancelled);
            FinishMission(m, MissionState.Cancelled, MissionFault.Player, "DTC.Mission.Cancelled.Player", "You cancelled the job." + (string.IsNullOrEmpty(reason) ? string.Empty : " (" + reason + ")"));
            Hook("FirstCancellation", "Cancelling has consequences (reputation, sometimes a fee) but never makes your career unrecoverable.");
            CareerResult r = CareerResult.Success(m.Id);
            r.TransactionId = tx;
            return Done(r);
        }

        /// <summary>
        /// The mission cannot continue for a system-side reason (mission vehicle or objects lost, internal error): it is
        /// Invalidated and resolves to a no-fault cancellation that pays Done legs (SPEC-MISSION 7.3).
        /// </summary>
        public CareerResult Abandon(string reason, double nowSeconds)
        {
            Enter(nowSeconds);
            Mission m = m_State.Mission;
            if (m == null || !m.IsOpen || m.SettlementPending)
            {
                return Fail(CareerResultCode.NotFound, "DTC.Mission.None", "There is no job to end.");
            }
            InvalidateMission(m, "DTC.Mission.Invalidated.System", "The job could not continue" + (string.IsNullOrEmpty(reason) ? "." : ": " + reason + ".") + " It ended with no fault.");
            return Done(CareerResult.Success(m.Id));
        }

        /// <summary>
        /// Recovery notification from the Player module. A system-triggered recovery extends deadlines by its duration;
        /// any recovery voids a challenge run (SPEC-CHALLENGE 3.3).
        /// </summary>
        public CareerResult NotifyRecovery(bool systemTriggered, double durationSeconds, double nowSeconds)
        {
            Enter(nowSeconds);
            VoidRun("DTC.Challenge.Void.Recovery");
            Mission m = m_State.Mission;
            if (systemTriggered && IsMissionOpen(m) && CareerMath.IsFinite(durationSeconds) && durationSeconds > 0)
            {
                RulesFor(m.Career).ExtendTimers(m, durationSeconds);
                m.StartWindowEnd += durationSeconds;
            }
            return Done(CareerResult.Success());
        }

        /// <summary>Retries a pending settlement, or reports AlreadyApplied for a mission that is already settled.</summary>
        public CareerResult Settle(string missionId, double nowSeconds)
        {
            Enter(nowSeconds);
            Mission m = m_State.Mission;
            if (m != null && m.Id == missionId && m.SettlementPending)
            {
                CommitSettlement(m, RulesFor(m.Career));
                return Done(CareerResult.Success(missionId));
            }
            if (m_State.Wallet.HasSettlement(missionId) || m_State.FindHistory(missionId) != null)
            {
                return Fail(CareerResultCode.AlreadyApplied, "DTC.Mission.AlreadySettled", "This job was already settled.");
            }
            return Fail(CareerResultCode.NotFound, "DTC.Mission.NotTerminal", "This job has not ended yet.");
        }

        // ------------------------------------------------------------------ tick

        private void TickMissionContained(PlayerSnapshot s, ContactSeverity contact, double now)
        {
            try
            {
                TickMission(s, contact, now);
            }
            catch (Exception ex)
            {
                ContainMissionFault(ex);
            }
        }

        /// <summary>REQ-RECOV-002: an internal error invalidates the mission (no-fault) instead of crashing.</summary>
        private void ContainMissionFault(Exception ex)
        {
            Notify(NotificationLevel.Error, "DTC.Career.InternalError", "Internal career error: " + ex.Message);
            Mission m = m_State.Mission;
            if (IsMissionOpen(m))
            {
                try
                {
                    InvalidateMission(m, "DTC.Mission.Invalidated.InternalError", "The job hit an internal error and ended with no fault.");
                }
                catch (Exception)
                {
                    m.SettlementPending = true;
                }
            }
        }

        private void TickMission(PlayerSnapshot s, ContactSeverity contact, double now)
        {
            Mission m = m_State.Mission;
            if (m == null)
            {
                return;
            }
            CareerRules rules = RulesFor(m.Career);
            if (m.SettlementPending)
            {
                CommitSettlement(m, rules);
                return;
            }
            if (s.Mode == PlayerMode.Building)
            {
                return; // Suspended: the career clock is frozen in Building (SPEC-MISSION section 9).
            }
            CareerVehicle driven = s.Mode == PlayerMode.Driving ? m_State.FindVehicle(s.VehicleId) : null;

            if (m.State == MissionState.Accepted)
            {
                if (driven != null && (m.VehicleId == null || m.VehicleId == driven.Id) && CheckEligible(m, driven, out _, out _))
                {
                    Activate(m, rules, driven, now);
                }
                else
                {
                    rules.CheckTimers(this, m, s, now);
                    if (IsMissionOpen(m) && m.AllRequiredResolved)
                    {
                        // Timers resolved every leg before the job started (for example a hard deadline).
                        FinishMission(m, rules.CompletionOutcome(m) == MissionState.Completed ? MissionState.Completed : MissionState.Failed, MissionFault.Player, "DTC.Mission.Failed.Deadline", "The deadline passed before the job started.");
                    }
                    else if (IsMissionOpen(m) && m.State == MissionState.Accepted && now > m.StartWindowEnd)
                    {
                        FinishMission(m, MissionState.Cancelled, MissionFault.Player, "DTC.Mission.Cancelled.StartWindow", "The job was cancelled because it was not started in time.");
                    }
                    return;
                }
            }
            if (!IsMissionOpen(m) || m.State != MissionState.Active)
            {
                return;
            }

            CareerVehicle mv = m_State.FindVehicle(m.VehicleId);
            if (mv != null && mv.Kind == VehicleKind.Service && mv.IsImmobilized)
            {
                mv = ReplaceServiceVehicle(m, mv, now);
            }
            bool inVehicle = driven != null && driven.Id == m.VehicleId;
            if (inVehicle)
            {
                m.LastVehiclePosition = s.Position;
                m.HasVehiclePosition = true;
                m.AwaySince = -1;
                double d = s.DistanceDrivenMeters;
                if (CareerMath.IsFinite(d) && d > 0 && d <= CareerBalance.MaxDistancePerSnapshotMeters)
                {
                    m.DrivenMeters += d;
                }
                if (contact != ContactSeverity.None)
                {
                    switch (contact)
                    {
                        case ContactSeverity.Light:
                            m.LightContacts++;
                            break;
                        case ContactSeverity.Medium:
                            m.MediumContacts++;
                            break;
                        default:
                            m.SevereContacts++;
                            break;
                    }
                    m.Cleanliness = CareerMath.Clamp(m.Cleanliness - CareerRulesPenalty(contact), 0, 100);
                    rules.OnContact(this, m, contact, now);
                    if (!IsMissionOpen(m))
                    {
                        return;
                    }
                }
                DetectHarsh(m, rules, s, driven, now);
                if (!IsMissionOpen(m))
                {
                    return;
                }
            }
            else
            {
                CheckAbandonment(m, rules, s, now);
                if (!IsMissionOpen(m))
                {
                    return;
                }
            }
            if (now - m.LastWatchdogAt >= CareerBalance.WatchdogSeconds)
            {
                Watchdog(m, rules, now);
                if (!IsMissionOpen(m))
                {
                    return;
                }
            }
            if (inVehicle)
            {
                rules.BeforeLegs(this, m, s, now);
                if (!IsMissionOpen(m))
                {
                    return;
                }
                ProcessLegs(m, rules, s, now);
                if (!IsMissionOpen(m))
                {
                    return;
                }
            }
            rules.CheckTimers(this, m, s, now);
            if (!IsMissionOpen(m))
            {
                return;
            }
            if (m.AllRequiredResolved)
            {
                MissionState outcome = rules.CompletionOutcome(m);
                if (outcome == MissionState.Completed)
                {
                    FinishMission(m, MissionState.Completed, MissionFault.None, "DTC.Mission.Completed", "Job completed.");
                }
                else
                {
                    FinishMission(m, MissionState.Failed, MissionFault.Player, "DTC.Mission.Failed.NothingDelivered", "Nothing could be delivered. The job failed.");
                }
            }
        }

        private static double CareerRulesPenalty(ContactSeverity s)
        {
            return s == ContactSeverity.Light ? CareerBalance.ContactPenaltyLight : s == ContactSeverity.Medium ? CareerBalance.ContactPenaltyMedium : CareerBalance.ContactPenaltySevere;
        }

        private void Activate(Mission m, CareerRules rules, CareerVehicle v, double now)
        {
            m.State = MissionState.Active;
            m.ActivatedAt = now;
            m.VehicleId = v.Id;
            m.LastWatchdogAt = now;
            m.LastVehiclePosition = v.LastPosition;
            m.HasVehiclePosition = v.HasPosition;
            rules.OnActivated(this, m, now);
            m_Sink.Add(new CareerEvent { Kind = CareerEventKind.MissionStateChanged, Time = now, MissionId = m.Id, Career = m.Career, FromState = MissionState.Accepted, ToState = MissionState.Active, VehicleId = v.Id, Key = "DTC.Mission.Active", Text = "Job started." });
        }

        /// <summary>Rising-edge harsh-event detection per axis (taxi table thresholds and career-specific ones).</summary>
        private void DetectHarsh(Mission m, CareerRules rules, PlayerSnapshot s, CareerVehicle v, double now)
        {
            double mult = v != null ? v.GetPerformance().HarshPenaltyMultiplier : 1;
            for (int axis = 0; axis < 2; axis++)
            {
                bool lateral = axis == 1;
                double a = Math.Abs(lateral ? s.LateralAccel : s.LongitudinalAccel);
                if (!CareerMath.IsFinite(a))
                {
                    continue;
                }
                double standard = (lateral ? 0.45 : 0.4) * CareerBalance.Gravity;
                double threshold = Math.Min(standard, rules.HarshThreshold(m, lateral));
                bool above = a >= threshold;
                bool wasAbove = lateral ? m.HarshLatLatched : m.HarshLongLatched;
                if (lateral)
                {
                    m.HarshLatLatched = above;
                }
                else
                {
                    m.HarshLongLatched = above;
                }
                if (!above || wasAbove)
                {
                    continue;
                }
                if (a >= standard)
                {
                    m.HarshEvents++;
                    m.Cleanliness = CareerMath.Clamp(m.Cleanliness - CareerBalance.HarshPenalty * mult, 0, 100);
                }
                rules.OnHarsh(this, m, lateral, a, mult, now);
                if (!IsMissionOpen(m))
                {
                    return;
                }
            }
        }

        private void CheckAbandonment(Mission m, CareerRules rules, PlayerSnapshot s, double now)
        {
            if (!rules.HasObjectsAboard(m) || s.Mode == PlayerMode.Transition)
            {
                if (!rules.HasObjectsAboard(m))
                {
                    m.AwaySince = -1;
                }
                return;
            }
            rules.AbandonLimits(m, out double meters, out double seconds);
            bool away = meters <= 0 || !m.HasVehiclePosition || MapPoint.Distance(s.Position, m.LastVehiclePosition) > meters;
            if (!away)
            {
                m.AwaySince = -1;
                return;
            }
            if (m.AwaySince < 0)
            {
                m.AwaySince = now;
                Notify(NotificationLevel.Warning, "DTC.Mission.WaitingForYou", "Your passengers or cargo are waiting in the vehicle. Return soon.");
                return;
            }
            if (now - m.AwaySince > seconds)
            {
                FinishMission(m, MissionState.Failed, MissionFault.Player, "DTC.Mission.Failed.Abandoned", "You left the vehicle for too long. The job failed.");
            }
        }

        private VehicleClass MissionClass(Mission m)
        {
            CareerVehicle v = m_State.FindVehicle(m.VehicleId);
            if (v != null)
            {
                return v.Class;
            }
            return m.AllowedClasses.Count > 0 ? m.AllowedClasses[0] : VehicleClass.Car;
        }

        /// <summary>
        /// Route watchdog (SPEC-MISSION 7.3): every pending leg of the current group (drop-offs may be unordered) is
        /// re-checked; two consecutive proven-unreachable checks resolve the leg through the career policy. An Unknown
        /// answer is never treated as reachable for offers, but it is not evidence of impossibility either, so it
        /// leaves the counter unchanged.
        /// </summary>
        private void Watchdog(Mission m, CareerRules rules, double now)
        {
            m.LastWatchdogAt = now;
            int group = m.CurrentGroup;
            if (group < 0)
            {
                return;
            }
            foreach (MissionLeg leg in m.Legs.ToArray())
            {
                if (!IsMissionOpen(m))
                {
                    return;
                }
                if (leg.Group != group || leg.IsResolved || !m.Legs.Contains(leg))
                {
                    continue;
                }
                PlaceInfo p = DescribePlace(leg.Place.Id, out bool unknown);
                if (!unknown && p == null)
                {
                    rules.OnLegInvalid(this, m, leg, now);
                    continue;
                }
                if (!m.HasVehiclePosition)
                {
                    continue;
                }
                RouteEstimate e = EstimateRoute(m.LastVehiclePosition, leg.Place.Id, MissionClass(m));
                if (e.IsReachable)
                {
                    leg.FailedChecks = 0;
                }
                else if (e.Status == RouteStatus.Unreachable && ++leg.FailedChecks >= CareerBalance.MaxFailedChecks)
                {
                    leg.FailedChecks = 0;
                    rules.OnLegUnreachable(this, m, leg, now);
                }
            }
        }

        /// <summary>Servicing rule of SPEC-MISSION section 4: inside R, below v, for t seconds.</summary>
        private void ProcessLegs(Mission m, CareerRules rules, PlayerSnapshot s, double now)
        {
            int group = m.CurrentGroup;
            if (group < 0)
            {
                return;
            }
            bool slow = s.Stationary || s.SpeedMps <= CareerBalance.ServiceSpeedMps;
            foreach (MissionLeg leg in m.Legs.ToArray())
            {
                if (leg.Group != group || leg.IsResolved)
                {
                    continue;
                }
                bool inZone = MapPoint.Distance(s.Position, leg.Place.Access) <= leg.Radius;
                if (inZone && leg.ArrivedAt < 0)
                {
                    leg.ArrivedAt = now;
                }
                bool ok = inZone && (slow || !rules.RequiresStop(leg));
                if (ok)
                {
                    if (leg.ServicingStartedAt < 0)
                    {
                        leg.ServicingStartedAt = now;
                        leg.RequiredSeconds = rules.ServiceSeconds(this, m, leg);
                        leg.ProgressUnits = 0;
                        leg.State = LegState.Servicing;
                        EmitLeg(m, leg, "DTC.Mission.Leg.Servicing", "Servicing " + leg.Place.Name + ".");
                    }
                    double elapsed = now - leg.ServicingStartedAt;
                    rules.OnServicingProgress(this, m, leg, elapsed);
                    if (elapsed >= leg.RequiredSeconds - 1e-9)
                    {
                        leg.State = LegState.Done;
                        leg.DoneAt = now;
                        leg.ServicingStartedAt = -1;
                        rules.OnLegDone(this, m, leg, s, now);
                        if (leg.Outcome == LegOutcome.Pending)
                        {
                            leg.Outcome = LegOutcome.DoneOk;
                        }
                        EmitLeg(m, leg, "DTC.Mission.Leg.Done", "Stop done: " + leg.Place.Name + ".");
                    }
                    return; // One leg is serviced at a time.
                }
                if (leg.State == LegState.Servicing)
                {
                    leg.State = LegState.Approaching;
                    leg.ServicingStartedAt = -1;
                    rules.OnServicingAborted(this, m, leg);
                    EmitLeg(m, leg, "DTC.Mission.Leg.Interrupted", "Moving interrupted the stop. Stop again to continue.");
                }
            }
        }

        private void EmitLeg(Mission m, MissionLeg leg, string key, string text)
        {
            m_Sink.Add(new CareerEvent { Kind = CareerEventKind.LegStateChanged, Time = m_Now, MissionId = m.Id, Career = m.Career, LegIndex = leg.Index, LegState = leg.State, PlaceId = leg.Place.Id, Key = key, Text = text, Args = new[] { leg.Place.Name } });
        }

        // ------------------------------------------------------------------ ending and settlement

        internal void InvalidateMission(Mission m, string key, string text, params string[] args)
        {
            FinishMission(m, MissionState.Invalidated, MissionFault.NoFault, key, text, args);
        }

        /// <summary>
        /// Moves an open mission to a terminal state and settles it exactly once, in the same in-memory commit as the
        /// ledger entry, XP and reputation (SPEC-MISSION section 8). Invalidated resolves to a no-fault Cancelled.
        /// </summary>
        internal void FinishMission(Mission m, MissionState outcome, MissionFault fault, string key, string text, params string[] args)
        {
            if (!IsMissionOpen(m))
            {
                return;
            }
            MissionState from = m.State;
            if (outcome == MissionState.Invalidated)
            {
                m_Sink.Add(new CareerEvent { Kind = CareerEventKind.MissionStateChanged, Time = m_Now, MissionId = m.Id, Career = m.Career, FromState = from, ToState = MissionState.Invalidated, Fault = MissionFault.NoFault, Key = key, Text = text, Args = args });
                from = MissionState.Invalidated;
                outcome = MissionState.Cancelled;
                fault = MissionFault.NoFault;
            }
            CareerRules rules = RulesFor(m.Career);
            m.State = outcome;
            m.Fault = fault;
            m.EndReasonKey = key;
            m.EndedAt = m_Now;
            rules.OnEnded(m);
            m_Sink.Add(new CareerEvent { Kind = CareerEventKind.MissionStateChanged, Time = m_Now, MissionId = m.Id, Career = m.Career, FromState = from, ToState = outcome, Fault = fault, Key = key, Text = text, Args = args });
            Notify(outcome == MissionState.Completed ? NotificationLevel.Info : NotificationLevel.Warning, key, text, args);
            CommitSettlement(m, rules);
        }

        internal static string SettlementTxId(string missionId, MissionState outcome)
        {
            string tag = outcome == MissionState.Completed ? "COMPLETED" : outcome == MissionState.Failed ? "FAILED" : "CANCELLED";
            return "MS-" + missionId + "-" + tag;
        }

        private void CommitSettlement(Mission m, CareerRules rules)
        {
            SettlementResult r;
            try
            {
                r = rules.Settle(m);
            }
            catch (Exception ex)
            {
                // SPEC-MISSION 12: stays flagged "settlement pending" and retries next update; the deterministic
                // TransactionId prevents a double payment.
                m.SettlementPending = true;
                Notify(NotificationLevel.Error, "DTC.Mission.SettlementPending", "Settlement failed and will be retried: " + ex.Message);
                return;
            }
            string tx = SettlementTxId(m.Id, m.State);
            long net = ApplySettlement(m, r, tx);
            m.SettlementPending = false;
            m_State.History.Add(new MissionHistoryEntry { MissionId = m.Id, Career = m.Career, State = m.State, Fault = m.Fault, TransactionId = tx, NetCents = net, EndedAt = m_Now, ReasonKey = m.EndReasonKey });
            while (m_State.History.Count > CareerBalance.MissionHistoryCap)
            {
                m_State.History.RemoveAt(0);
            }
            CareerVehicle v = m_State.FindVehicle(m.VehicleId);
            if (m.UsesServiceVehicle && v != null && v.Kind == VehicleKind.Service && !v.IsRemoved)
            {
                v.State = VehicleLocationState.Removed;
                m_Sink.Add(new CareerEvent { Kind = CareerEventKind.VehicleReleased, Time = m_Now, VehicleId = v.Id, Key = "DTC.Garage.ServiceVehicleReturned", Text = "The operator collects the service vehicle when you leave it." });
            }
            m_State.Mission = null;
            m_Sink.Add(new CareerEvent
            {
                Kind = CareerEventKind.Settled,
                Time = m_Now,
                MissionId = m.Id,
                Career = m.Career,
                ToState = m.State,
                Fault = m.Fault,
                TransactionId = tx,
                AmountCents = net,
                Key = "DTC.Mission.Settled",
                Text = "Settled: " + CareerMath.Money(net) + ", +" + CareerMath.Inv(r.Xp) + " XP, reputation " + (r.ReputationDelta >= 0 ? "+" : string.Empty) + CareerMath.Inv(r.ReputationDelta) + ".",
                Args = new[] { CareerMath.Inv(net), CareerMath.Inv(r.Xp), CareerMath.Inv(r.ReputationDelta) },
            });
            if (net > 0)
            {
                Hook("FirstSettlement", "You were paid. The breakdown shows pay, tip, XP and reputation; your wallet is separate from the city budget.");
            }
        }

        /// <summary>Applies a settlement once. Returns the net wallet amount of the settlement entry.</summary>
        private long ApplySettlement(Mission m, SettlementResult r, string tx)
        {
            Ledger w = m_State.Wallet;
            if (w.HasSettlement(m.Id) || w.Contains(tx))
            {
                LedgerEntry existing = w.Find(tx);
                return existing != null ? existing.AmountCents : 0;
            }
            long net = r.NetCents;
            long deferred = 0;
            var lines = new List<LedgerLine>(r.Lines);
            if (net < 0)
            {
                long payable = Math.Min(-net, w.Balance);
                deferred = -net - payable;
                if (deferred > 0)
                {
                    lines.Add(new LedgerLine("DTC.Ledger.Deferred", "Deferred charge", deferred));
                }
                net = -payable;
            }
            w.Append(tx, LedgerEntryType.MissionSettlement, net, m_Now, m.Id, "DTC.Ledger.MissionSettlement", m.Career + " job " + m.State.ToString().ToLowerInvariant(), lines);
            AddDeferred(deferred);
            m_State.Progression.AddXp(r.XpCareer, r.Xp);
            m_State.Progression.AddReputation(r.ReputationDelta);
            if (net > 0)
            {
                CareerVehicle v = m_State.FindVehicle(m.VehicleId);
                if (v != null && v.Kind == VehicleKind.BasicRental)
                {
                    long share = CareerMath.RoundCents(net * CareerBalance.BasicRentalShare);
                    if (share > 0)
                    {
                        w.Append("RN-" + v.RentalId + "-" + m.Id, LedgerEntryType.RentalCharge, -share, m_Now, v.RentalId, "DTC.Ledger.RentalShare", "Basic rental share (25%)");
                    }
                }
                if (m_State.DeferredCents > 0)
                {
                    long repay = Math.Min(m_State.DeferredCents, Math.Min(CareerMath.RoundCents(net * CareerBalance.DeferredRepaymentShare), w.Balance));
                    if (repay > 0 && w.Append("DR-" + tx, LedgerEntryType.DeferredChargeRepayment, -repay, m_Now, tx, "DTC.Ledger.DeferredRepayment", "Deferred charge repayment") == LedgerAppendStatus.Appended)
                    {
                        m_State.DeferredCents -= repay;
                    }
                }
            }
            return net;
        }
    }
}
