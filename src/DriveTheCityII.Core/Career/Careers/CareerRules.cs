using System;
using System.Collections.Generic;

namespace DriveTheCityII.Core.Career
{
    /// <summary>
    /// Career extension points of SPEC-MISSION section 11: OfferGenerator, Eligibility, ServicingRule, EvaluationModel,
    /// FailureRules, SubstitutionPolicy and the pure SettlementCalculator. Rules are stateless; all state lives in the
    /// <see cref="Mission"/> record so it persists and replays deterministically.
    /// </summary>
    internal abstract class CareerRules
    {
        public abstract CareerKind Kind { get; }

        /// <summary>Unlock needed to see offers of this career at all.</summary>
        public virtual Unlock RequiredUnlock => Unlock.TaxiCareer;

        /// <summary>OfferGenerator: validated candidate contracts (REQ-CAREER-002, REQ-CAREER-003).</summary>
        public abstract List<MissionOffer> Generate(CareerService svc, MapPoint near, double now, int count);

        /// <summary>Called once at acceptance: sets deadlines and patience relative to the acceptance time.</summary>
        public virtual void OnAccepted(CareerService svc, Mission m, double now)
        {
        }

        /// <summary>Called when the mission becomes Active (player drives an eligible vehicle).</summary>
        public virtual void OnActivated(CareerService svc, Mission m, double now)
        {
        }

        /// <summary>Eligibility beyond the vehicle class (capacity, payload).</summary>
        public virtual bool CheckCapacity(Mission m, CareerVehicle v, out string reasonText)
        {
            reasonText = null;
            return true;
        }

        /// <summary>Servicing duration t of SPEC-MISSION section 4, computed when servicing starts.</summary>
        public virtual double ServiceSeconds(CareerService svc, Mission m, MissionLeg leg) => 0;

        /// <summary>Whether servicing needs the vehicle below the speed threshold (false for pass-through checkpoints).</summary>
        public virtual bool RequiresStop(MissionLeg leg) => true;

        public virtual void OnServicingProgress(CareerService svc, Mission m, MissionLeg leg, double elapsed)
        {
        }

        public virtual void OnServicingAborted(CareerService svc, Mission m, MissionLeg leg)
        {
        }

        public virtual void OnLegDone(CareerService svc, Mission m, MissionLeg leg, PlayerSnapshot s, double now)
        {
        }

        /// <summary>Runs before leg servicing while the player drives the mission vehicle (bus pass detection).</summary>
        public virtual void BeforeLegs(CareerService svc, Mission m, PlayerSnapshot s, double now)
        {
        }

        /// <summary>EvaluationModel input: a contact event in the mission vehicle.</summary>
        public virtual void OnContact(CareerService svc, Mission m, ContactSeverity severity, double now)
        {
        }

        /// <summary>EvaluationModel input: a harsh acceleration event (rising edge above <see cref="HarshThreshold"/>).</summary>
        public virtual void OnHarsh(CareerService svc, Mission m, bool lateral, double magnitudeMps2, double penaltyMultiplier, double now)
        {
        }

        /// <summary>Lowest acceleration (m/s^2) that matters to this career, per axis.</summary>
        public virtual double HarshThreshold(Mission m, bool lateral) => (lateral ? 0.45 : 0.4) * CareerBalance.Gravity;

        /// <summary>Mission objects (passengers or cargo) are aboard the mission vehicle.</summary>
        public virtual bool HasObjectsAboard(Mission m) => false;

        /// <summary>Abandonment rule: distance from the vehicle (0 = any exit) and time (SPEC-MISSION 7.2).</summary>
        public virtual void AbandonLimits(Mission m, out double meters, out double seconds)
        {
            meters = CareerBalance.AbandonDistanceMeters;
            seconds = CareerBalance.AbandonSeconds;
        }

        /// <summary>FailureRules on timers: deadlines, patience, immobile vehicle.</summary>
        public virtual void CheckTimers(CareerService svc, Mission m, PlayerSnapshot s, double now)
        {
        }

        /// <summary>System-triggered recovery extends deadlines by its duration (SPEC-MISSION 7.3).</summary>
        public virtual void ExtendTimers(Mission m, double seconds)
        {
        }

        /// <summary>
        /// Revalidation pass (REQ-CAREER-006). The default refreshes unresolved legs through Describe and applies
        /// <see cref="OnLegInvalid"/> to missing places.
        /// </summary>
        public virtual void Revalidate(CareerService svc, Mission m, MapPoint position, double now)
        {
            foreach (MissionLeg leg in new List<MissionLeg>(m.Legs))
            {
                if (leg.IsResolved || !svc.IsMissionOpen(m))
                {
                    continue;
                }
                PlaceInfo p = svc.DescribePlace(leg.Place.Id, out bool unknown);
                if (unknown)
                {
                    continue;
                }
                if (p == null)
                {
                    OnLegInvalid(svc, m, leg, now);
                }
                else
                {
                    // An alternate or moved access point of the same place is used (SPEC-MISSION 7.3 step 1).
                    leg.Place.Access = p.Access;
                    leg.Place.Name = p.Name ?? leg.Place.Name;
                }
            }
        }

        /// <summary>SubstitutionPolicy: the leg's place no longer exists.</summary>
        public virtual void OnLegInvalid(CareerService svc, Mission m, MissionLeg leg, double now)
        {
            svc.InvalidateMission(m, "DTC.Mission.Invalidated.PlaceRemoved", "A destination was removed from the city. The job was cancelled with no fault.");
        }

        /// <summary>The route to the current leg failed two consecutive checks.</summary>
        public virtual void OnLegUnreachable(CareerService svc, Mission m, MissionLeg leg, double now)
        {
            OnLegInvalid(svc, m, leg, now);
        }

        /// <summary>Releases mission objects when the mission ends (passengers alight, cargo is returned).</summary>
        public virtual void OnEnded(Mission m)
        {
        }

        /// <summary>Terminal state when every required leg is resolved (for example Failed when nothing was delivered).</summary>
        public virtual MissionState CompletionOutcome(Mission m) => MissionState.Completed;

        /// <summary>Pure SettlementCalculator (SPEC-MISSION section 8 step 1).</summary>
        public abstract SettlementResult Settle(Mission m);

        // ------------------------------------------------------------------ shared helpers

        protected static double ContactPenalty(ContactSeverity s)
        {
            switch (s)
            {
                case ContactSeverity.Light:
                    return CareerBalance.ContactPenaltyLight;
                case ContactSeverity.Medium:
                    return CareerBalance.ContactPenaltyMedium;
                case ContactSeverity.Severe:
                    return CareerBalance.ContactPenaltySevere;
                default:
                    return 0;
            }
        }

        /// <summary>Integrity pay factor shared by deliveries and freight (SPEC-DELIVERY section 7).</summary>
        protected static double IntegrityFactor(double integrity)
        {
            if (integrity >= 80)
            {
                return 1.0;
            }
            if (integrity >= 50)
            {
                return 0.7;
            }
            return integrity > 0 ? 0.3 : 0;
        }

        /// <summary>Deadline factor: 100% before soft, linear to 50% at hard, 0 after (SPEC-DELIVERY section 6).</summary>
        protected static double DeadlineFactor(double t, double soft, double hard)
        {
            if (soft < 0 || t <= soft)
            {
                return 1.0;
            }
            if (t > hard || hard <= soft)
            {
                return 0;
            }
            return 1.0 - 0.5 * (t - soft) / (hard - soft);
        }

        protected static SettlementResult NewResult(Mission m)
        {
            return new SettlementResult { Outcome = m.State, Fault = m.Fault, XpCareer = m.Career };
        }

        /// <summary>Cancellation fee: 10% of contract value, capped (SPEC-MISSION 7.1, SPEC-ECON section 8).</summary>
        protected static long CancellationFee(Mission m)
        {
            return Math.Min(CareerBalance.CancellationFeeCapCents, CareerMath.RoundCents(m.ContractValueCents * CareerBalance.CancellationFeeShare));
        }

        protected static MissionLeg Leg(int index, int group, LegKind kind, PlaceInfo place, double radius, double plannedMeters, double plannedSeconds, int count)
        {
            return new MissionLeg
            {
                Index = index,
                Group = group,
                Kind = kind,
                Place = PlaceRef.From(place),
                Radius = radius,
                PlannedMeters = plannedMeters,
                PlannedSeconds = plannedSeconds,
                Count = count,
            };
        }

        protected static bool NearOtherOffer(List<MissionOffer> offers, MapPoint p, double meters)
        {
            foreach (MissionOffer o in offers)
            {
                if (o.LegTemplates.Count > 0 && MapPoint.DistanceXZ(o.LegTemplates[0].Place.Access, p) < meters)
                {
                    return true;
                }
            }
            return false;
        }

        protected static void ResolveLeg(MissionLeg leg, LegOutcome outcome, double now)
        {
            if (leg.IsResolved)
            {
                return;
            }
            leg.State = LegState.Done;
            leg.Outcome = outcome;
            leg.DoneAt = now;
            leg.ServicingStartedAt = -1;
        }
    }
}
