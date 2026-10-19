using System;
using System.Collections.Generic;

namespace DriveTheCityII.Core.Career
{
    /// <summary>
    /// Taxi career (SPEC-TAXI; REQ-TAXI-001..003). Passengers are mod-owned career passengers, never native citizens.
    /// Fare = (flag + per-km x PLANNED distance) x (1 + tip by ride rating).
    /// </summary>
    internal sealed class TaxiRules : CareerRules
    {
        internal const string FailPatienceKey = "DTC.Taxi.Fail.Patience";
        internal const string FailSevereKey = "DTC.Taxi.Fail.SevereContacts";
        internal const string FailLeftKey = "DTC.Taxi.Fail.LeftVehicle";
        internal const string FailImmobileKey = "DTC.Taxi.Fail.Immobile";

        private static readonly PlaceKind[] s_Kinds = { PlaceKind.Residential, PlaceKind.Commercial, PlaceKind.Office, PlaceKind.Leisure, PlaceKind.TransitStop };

        public override CareerKind Kind => CareerKind.Taxi;

        public static double MaxTripMeters(int taxiLevel)
        {
            return Math.Min(CareerBalance.TaxiTripMaxMetersCap, CareerBalance.TaxiTripMaxMeters + (taxiLevel - 1) * CareerBalance.TaxiTripMaxPerLevelMeters);
        }

        public static long BaseFare(double tripMeters)
        {
            return CareerBalance.TaxiFlagCents + CareerMath.RoundCents(CareerBalance.TaxiPerKmCents * tripMeters / 1000.0);
        }

        public override List<MissionOffer> Generate(CareerService svc, MapPoint near, double now, int count)
        {
            double maxTrip = MaxTripMeters(svc.State.Progression.Level(CareerKind.Taxi));
            List<MissionOffer> offers = TryGenerate(svc, near, now, count, CareerBalance.TaxiTripMinMeters, maxTrip, CareerBalance.TaxiPickupMaxMeters);
            if (offers.Count == 0)
            {
                // Starter-job guarantee (SPEC-ECON section 7, RISK-204): relax the bands before giving up.
                offers = TryGenerate(svc, near, now, 1, 200, maxTrip, CareerBalance.TaxiPickupMaxMeters * 2);
            }
            return offers;
        }

        private List<MissionOffer> TryGenerate(CareerService svc, MapPoint near, double now, int count, double minTrip, double maxTrip, double pickupMax)
        {
            var offers = new List<MissionOffer>();
            List<PlaceInfo> pickups = svc.QueryPlacesSorted(s_Kinds, near, pickupMax, 48);
            List<PlaceInfo> dests = svc.QueryPlacesSorted(s_Kinds, near, pickupMax + maxTrip, 96);
            svc.State.Rng.Shuffle(pickups);
            svc.State.Rng.Shuffle(dests);
            int budget = 60;
            foreach (PlaceInfo pickup in pickups)
            {
                if (offers.Count >= count || budget <= 0)
                {
                    break;
                }
                if (NearOtherOffer(offers, pickup.Access, 50))
                {
                    continue;
                }
                RouteEstimate approach = svc.EstimateRoute(near, pickup.Id, VehicleClass.Car);
                budget--;
                if (!approach.IsReachable || approach.DistanceMeters > pickupMax)
                {
                    continue;
                }
                int tries = 0;
                foreach (PlaceInfo dest in dests)
                {
                    if (tries >= 6 || budget <= 0)
                    {
                        break;
                    }
                    if (dest.Id == pickup.Id)
                    {
                        continue;
                    }
                    double straight = MapPoint.DistanceXZ(pickup.Access, dest.Access);
                    if (straight > maxTrip || straight < minTrip * 0.3)
                    {
                        continue;
                    }
                    tries++;
                    RouteEstimate trip = svc.EstimateRoute(pickup.Access, dest.Id, VehicleClass.Car);
                    budget--;
                    if (!trip.IsReachable || trip.DistanceMeters < minTrip || trip.DistanceMeters > maxTrip)
                    {
                        continue;
                    }
                    offers.Add(Build(svc, near, now, pickup, dest, approach, trip));
                    break;
                }
            }
            return offers;
        }

        private MissionOffer Build(CareerService svc, MapPoint near, double now, PlaceInfo pickup, PlaceInfo dest, RouteEstimate approach, RouteEstimate trip)
        {
            DeterministicRandom rng = svc.State.Rng;
            int passengers = rng.NextRange(1, 3);
            double p = rng.NextDouble();
            TaxiPreference pref = p < 0.6 ? TaxiPreference.Standard : (p < 0.8 ? TaxiPreference.InAHurry : TaxiPreference.Comfort);
            long fare = BaseFare(trip.DistanceMeters);
            double patience = pref == TaxiPreference.InAHurry ? CareerBalance.TaxiPatienceHurrySeconds : CareerBalance.TaxiPatienceSeconds;
            MissionOffer o = svc.NewOffer(CareerKind.Taxi, near, now);
            o.TitleKey = "DTC.Taxi.Offer.Title";
            o.Title = "Fare to " + (dest.Name ?? dest.Id);
            o.ApproachMeters = approach.DistanceMeters;
            o.ApproachSeconds = approach.TimeSeconds;
            o.PlannedMeters = trip.DistanceMeters;
            o.PlannedSeconds = trip.TimeSeconds;
            o.ContractValueCents = fare;
            o.PayMinCents = fare;
            o.PayMaxCents = CareerMath.RoundCents(fare * 1.25);
            o.XpPreview = Progression.MissionXp(trip.DistanceMeters, 100);
            o.PatienceSeconds = patience;
            o.Passengers = passengers;
            o.Preference = pref;
            o.AllowedClasses.Add(VehicleClass.Car);
            o.AllowedClasses.Add(VehicleClass.Taxi);
            o.LegTemplates.Add(Leg(0, 0, LegKind.Pickup, pickup, CareerBalance.DefaultServiceRadius, approach.DistanceMeters, approach.TimeSeconds, passengers));
            o.LegTemplates.Add(Leg(1, 1, LegKind.Dropoff, dest, CareerBalance.DefaultServiceRadius, trip.DistanceMeters, trip.TimeSeconds, passengers));
            o.TaxiTemplate = new TaxiTerms
            {
                Passengers = passengers,
                Preference = pref,
                PatienceSeconds = patience,
                TripMeters = trip.DistanceMeters,
                TripEstimateSeconds = trip.TimeSeconds,
                BaseFareCents = fare,
            };
            return o;
        }

        public override void OnAccepted(CareerService svc, Mission m, double now)
        {
            m.Taxi.PatienceDeadline = now + m.Taxi.PatienceSeconds;
        }

        public override bool CheckCapacity(Mission m, CareerVehicle v, out string reasonText)
        {
            reasonText = null;
            if (v.PassengerCapacity < m.Taxi.Passengers)
            {
                reasonText = "Not enough seats for the passengers";
                return false;
            }
            return true;
        }

        public override double ServiceSeconds(CareerService svc, Mission m, MissionLeg leg)
        {
            return leg.Kind == LegKind.Pickup ? CareerBalance.TaxiBoardSeconds : CareerBalance.TaxiAlightSeconds;
        }

        public override void OnLegDone(CareerService svc, Mission m, MissionLeg leg, PlayerSnapshot s, double now)
        {
            TaxiTerms t = m.Taxi;
            if (leg.Kind == LegKind.Pickup)
            {
                t.PassengerState = PassengerState.Aboard;
                t.PickedUpAt = now;
                t.PickupPoint = leg.Place.Access;
                ResolveOk(leg);
                svc.Notify(NotificationLevel.Info, "DTC.Taxi.Boarded", "Your career passengers boarded.", CareerMath.Inv(t.Passengers));
            }
            else
            {
                t.PassengerState = PassengerState.Alighted;
                t.DroppedAt = now;
                ApplyTripTime(t);
                ResolveOk(leg);
            }
        }

        private static void ResolveOk(MissionLeg leg)
        {
            leg.Outcome = LegOutcome.DoneOk;
        }

        /// <summary>Trip-time adjustments of the ride-quality table (SPEC-TAXI section 6).</summary>
        private static void ApplyTripTime(TaxiTerms t)
        {
            if (t.PickedUpAt < 0 || t.TripEstimateSeconds <= 0)
            {
                return;
            }
            double trip = t.DroppedAt - t.PickedUpAt;
            double limit = t.Preference == TaxiPreference.InAHurry ? 1.1 : 1.3;
            if (trip > limit * t.TripEstimateSeconds)
            {
                t.Quality -= 10;
            }
            else if (t.Preference == TaxiPreference.InAHurry && trip < 0.9 * t.TripEstimateSeconds)
            {
                t.Quality += 5;
            }
            t.Quality = CareerMath.Clamp(t.Quality, 0, 100);
        }

        public override void OnContact(CareerService svc, Mission m, ContactSeverity severity, double now)
        {
            TaxiTerms t = m.Taxi;
            if (t.PassengerState != PassengerState.Aboard)
            {
                return;
            }
            t.Quality = CareerMath.Clamp(t.Quality - ContactPenalty(severity), 0, 100);
            if (severity == ContactSeverity.Severe)
            {
                t.SevereContacts++;
                if (t.SevereContacts >= CareerBalance.TaxiSevereLimit)
                {
                    svc.FinishMission(m, MissionState.Failed, MissionFault.Player, FailSevereKey, "Your passengers demanded to get out after three severe collisions.");
                }
            }
        }

        public override void OnHarsh(CareerService svc, Mission m, bool lateral, double magnitudeMps2, double penaltyMultiplier, double now)
        {
            TaxiTerms t = m.Taxi;
            if (t.PassengerState != PassengerState.Aboard)
            {
                return;
            }
            double k = t.Preference == TaxiPreference.Comfort ? 2 : 1;
            t.Quality = CareerMath.Clamp(t.Quality - CareerBalance.HarshPenalty * k * penaltyMultiplier, 0, 100);
        }

        public override bool HasObjectsAboard(Mission m) => m.Taxi.PassengerState == PassengerState.Aboard;

        public override void AbandonLimits(Mission m, out double meters, out double seconds)
        {
            // Passengers wait 2 career-clock minutes once the player leaves the vehicle (SPEC-TAXI section 8).
            meters = 0;
            seconds = CareerBalance.TaxiLeaveVehicleSeconds;
        }

        public override void CheckTimers(CareerService svc, Mission m, PlayerSnapshot s, double now)
        {
            TaxiTerms t = m.Taxi;
            MissionLeg pickup = m.Legs[0];
            if (t.PassengerState == PassengerState.Waiting && pickup.ArrivedAt < 0 && t.PatienceDeadline >= 0 && now > t.PatienceDeadline)
            {
                svc.FinishMission(m, MissionState.Failed, MissionFault.Player, FailPatienceKey, "The passenger ran out of patience and left.");
                return;
            }
            if (t.PassengerState == PassengerState.Aboard)
            {
                CareerVehicle v = svc.State.FindVehicle(m.VehicleId);
                if (v != null && v.IsImmobilized)
                {
                    if (m.ImmobileSince < 0)
                    {
                        m.ImmobileSince = now;
                    }
                    else if (now - m.ImmobileSince > CareerBalance.TaxiImmobileSeconds)
                    {
                        svc.FinishMission(m, MissionState.Failed, MissionFault.Player, FailImmobileKey, "The taxi could not move, so the passengers left.");
                    }
                }
                else
                {
                    m.ImmobileSince = -1;
                }
            }
        }

        public override void ExtendTimers(Mission m, double seconds)
        {
            if (m.Taxi.PatienceDeadline >= 0)
            {
                m.Taxi.PatienceDeadline += seconds;
            }
            m.Taxi.TripEstimateSeconds += seconds;
        }

        public override void OnLegInvalid(CareerService svc, Mission m, MissionLeg leg, double now)
        {
            TaxiTerms t = m.Taxi;
            if (leg.Kind == LegKind.Pickup)
            {
                svc.InvalidateMission(m, "DTC.Taxi.Invalidated.Pickup", "The pickup point was removed from the city. The fare was cancelled with no fault.");
                return;
            }
            // Drop-off: the passenger proposes the nearest eligible place within 400 m (accepted automatically).
            MapPoint from = t.PassengerState == PassengerState.Aboard && m.HasVehiclePosition ? m.LastVehiclePosition : m.Legs[0].Place.Access;
            List<PlaceInfo> candidates = svc.QueryPlacesByDistance(s_Kinds, leg.Place.Access, CareerBalance.TaxiSubstituteRadiusMeters, 16);
            foreach (PlaceInfo c in candidates)
            {
                if (c.Id == leg.Place.Id || c.Id == m.Legs[0].Place.Id)
                {
                    continue;
                }
                RouteEstimate toNew = svc.EstimateRoute(from, c.Id, VehicleClass.Car);
                RouteEstimate planned = svc.EstimateRoute(m.Legs[0].Place.Access, c.Id, VehicleClass.Car);
                if (!toNew.IsReachable || !planned.IsReachable)
                {
                    continue;
                }
                string oldName = leg.Place.Name;
                leg.Place = PlaceRef.From(c);
                leg.PlannedMeters = planned.DistanceMeters;
                leg.PlannedSeconds = planned.TimeSeconds;
                leg.State = LegState.Approaching;
                leg.ServicingStartedAt = -1;
                t.TripMeters = planned.DistanceMeters;
                t.TripEstimateSeconds = planned.TimeSeconds;
                t.BaseFareCents = BaseFare(planned.DistanceMeters);
                m.PlannedMeters = planned.DistanceMeters;
                m.ContractValueCents = t.BaseFareCents;
                leg.FailedChecks = 0;
                svc.Notify(NotificationLevel.Warning, "DTC.Taxi.Substituted", "The destination '" + oldName + "' no longer exists. The passenger asked to go to '" + c.Name + "' instead; the fare was recalculated.", oldName, c.Name);
                return;
            }
            if (t.PassengerState == PassengerState.Aboard)
            {
                t.PayShare = ProgressShare(m, leg);
                svc.InvalidateMission(m, "DTC.Taxi.Invalidated.DropoffPaid", "The destination was removed from the city. The passengers got out; you were paid for the distance covered.");
            }
            else
            {
                svc.InvalidateMission(m, "DTC.Taxi.Invalidated.Dropoff", "The destination was removed from the city. The fare was cancelled with no fault.");
            }
        }

        /// <summary>Planned-distance share covered, by straight-line progress toward the drop-off (detours do not count).</summary>
        private static double ProgressShare(Mission m, MissionLeg drop)
        {
            if (!m.HasVehiclePosition)
            {
                return 0;
            }
            double total = MapPoint.DistanceXZ(m.Taxi.PickupPoint, drop.Place.Access);
            if (total <= 1)
            {
                return 1;
            }
            double remaining = MapPoint.DistanceXZ(m.LastVehiclePosition, drop.Place.Access);
            return CareerMath.Clamp(1.0 - remaining / total, 0, 1);
        }

        public override void OnEnded(Mission m)
        {
            if (m.Taxi.PassengerState == PassengerState.Aboard)
            {
                m.Taxi.PassengerState = PassengerState.Alighted;
            }
        }

        public override SettlementResult Settle(Mission m)
        {
            SettlementResult r = NewResult(m);
            TaxiTerms t = m.Taxi;
            r.Evaluation = t.Quality;
            if (m.State == MissionState.Completed)
            {
                double tip = TipShare(t.Quality);
                r.AddLine("DTC.Settle.Taxi.Fare", "Fare", t.BaseFareCents);
                long tipCents = CareerMath.RoundCents(t.BaseFareCents * tip);
                if (tipCents > 0)
                {
                    r.AddLine("DTC.Settle.Taxi.Tip", "Tip (" + Stars(t.Quality) + " stars)", tipCents);
                }
                r.Xp = Progression.MissionXp(t.TripMeters, t.Quality);
                r.ReputationDelta = Progression.CompletionReputation(t.Quality);
            }
            else if (m.State == MissionState.Cancelled && m.Fault == MissionFault.NoFault)
            {
                // No-fault end with passengers picked up: paid for the planned distance covered (SPEC-TAXI section 8).
                double share = t.PickedUpAt < 0 ? 0 : (t.PayShare > 0 ? t.PayShare : ProgressShare(m, m.Legs[1]));
                long pay = CareerMath.RoundCents(t.BaseFareCents * CareerMath.Clamp(share, 0, 1));
                if (pay > 0)
                {
                    r.AddLine("DTC.Settle.Taxi.PartialFare", "Fare for the distance covered", pay);
                }
                r.Xp = Progression.MissionXp(t.TripMeters, t.Quality, share);
            }
            else if (m.State == MissionState.Cancelled)
            {
                // Player cancellation: no fee, no fare; -1 before pickup, -3 with passengers aboard (SPEC-TAXI section 8).
                r.ReputationDelta = t.PickedUpAt >= 0 ? -3 : -1;
            }
            else if (m.State == MissionState.Failed)
            {
                r.ReputationDelta = m.EndReasonKey == FailPatienceKey ? -1 : CareerBalance.ReputationFailed;
            }
            return r;
        }

        /// <summary>Tip by rating: 5 stars 25%, 4 stars 15%, 3 stars 5%, else 0 (SPEC-TAXI section 6).</summary>
        public static double TipShare(double quality)
        {
            int stars = Stars(quality);
            return stars == 5 ? 0.25 : stars == 4 ? 0.15 : stars == 3 ? 0.05 : 0;
        }

        public static int Stars(double quality)
        {
            if (quality >= 90)
            {
                return 5;
            }
            if (quality >= 75)
            {
                return 4;
            }
            if (quality >= 55)
            {
                return 3;
            }
            return quality >= 35 ? 2 : 1;
        }
    }
}
