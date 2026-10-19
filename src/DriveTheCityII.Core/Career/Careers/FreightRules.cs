using System;
using System.Collections.Generic;

namespace DriveTheCityII.Core.Career
{
    /// <summary>
    /// Rigid-truck freight (SPEC-FREIGHT; REQ-FREIGHT-001). Requires <see cref="VehicleClass.RigidTruck"/>; articulated
    /// trucks are out of scope. Pay = per load + per tonne-km of planned distance, heavy x1.2, service truck x0.6.
    /// Freight loads are mod-owned career cargo and never change native goods flows (REQ-CITY-003).
    /// </summary>
    internal sealed class FreightRules : CareerRules
    {
        private static readonly PlaceKind[] s_PickupKinds = { PlaceKind.Industrial, PlaceKind.Warehouse };
        private static readonly PlaceKind[] s_DropKinds = { PlaceKind.Commercial, PlaceKind.Industrial, PlaceKind.Warehouse };

        public override CareerKind Kind => CareerKind.Freight;

        public override Unlock RequiredUnlock => Unlock.Freight;

        public override List<MissionOffer> Generate(CareerService svc, MapPoint near, double now, int count)
        {
            var offers = new List<MissionOffer>();
            DeterministicRandom rng = svc.State.Rng;
            bool ownTruck = svc.State.Progression.IsUnlocked(Unlock.TruckRentalPurchase) && svc.PlayerHasClass(VehicleClass.RigidTruck);
            double payload = ownTruck ? svc.BestPayloadTonnes() : CareerBalance.TruckPayloadTonnes;
            List<PlaceInfo> pickups = svc.QueryPlacesSorted(s_PickupKinds, near, 4000, 24);
            List<PlaceInfo> drops = svc.QueryPlacesSorted(s_DropKinds, near, 4000 + 15000, 64);
            rng.Shuffle(pickups);
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
                RouteEstimate approach = svc.EstimateRoute(near, pickup.Id, VehicleClass.RigidTruck);
                budget--;
                if (!approach.IsReachable)
                {
                    continue;
                }
                double c = rng.NextDouble();
                CargoType cargo = c < 0.5 ? CargoType.General : (c < 0.8 ? CargoType.Bulk : CargoType.Heavy);
                int dropCount = cargo == CargoType.General ? rng.NextRange(1, 3) : 1;
                double mass;
                switch (cargo)
                {
                    case CargoType.Bulk:
                        mass = rng.NextRange(10, 16) * 0.5;
                        break;
                    case CargoType.Heavy:
                        mass = rng.NextRange(8, 16) * 0.5;
                        break;
                    default:
                        mass = rng.NextRange(4, 16) * 0.5;
                        break;
                }
                mass = Math.Min(mass, payload);
                var chosen = new List<PlaceInfo>();
                var est = new List<RouteEstimate>();
                var shuffled = new List<PlaceInfo>(drops);
                rng.Shuffle(shuffled);
                MapPoint prev = pickup.Access;
                foreach (PlaceInfo d in shuffled)
                {
                    if (chosen.Count >= dropCount || budget <= 0)
                    {
                        break;
                    }
                    if (d.Id == pickup.Id || Contains(chosen, d.Id))
                    {
                        continue;
                    }
                    double straight = MapPoint.DistanceXZ(prev, d.Access);
                    if (straight < 300 || straight > 12000)
                    {
                        continue;
                    }
                    RouteEstimate e = svc.EstimateRoute(prev, d.Id, VehicleClass.RigidTruck);
                    budget--;
                    if (!e.IsReachable)
                    {
                        continue;
                    }
                    chosen.Add(d);
                    est.Add(e);
                    prev = d.Access;
                }
                if (chosen.Count < dropCount)
                {
                    continue;
                }
                double planned = 0;
                double plannedSeconds = 0;
                foreach (RouteEstimate e in est)
                {
                    planned += e.DistanceMeters;
                    plannedSeconds += e.TimeSeconds;
                }
                if (planned < 2000 || planned > 15000)
                {
                    continue;
                }
                offers.Add(Build(svc, near, now, cargo, mass, pickup, chosen, est, approach, planned, plannedSeconds, !ownTruck));
            }
            return offers;
        }

        private static bool Contains(List<PlaceInfo> list, string id)
        {
            foreach (PlaceInfo p in list)
            {
                if (p.Id == id)
                {
                    return true;
                }
            }
            return false;
        }

        private MissionOffer Build(CareerService svc, MapPoint near, double now, CargoType cargo, double mass, PlaceInfo pickup, List<PlaceInfo> drops, List<RouteEstimate> est, RouteEstimate approach, double planned, double plannedSeconds, bool serviceTruck)
        {
            var terms = new FreightTerms { Cargo = cargo, MassTonnes = mass, ServiceTruck = serviceTruck };
            MissionOffer o = svc.NewOffer(CareerKind.Freight, near, now);
            o.LegTemplates.Add(Leg(0, 0, LegKind.Load, pickup, CareerBalance.FreightDockRadius, approach.DistanceMeters, approach.TimeSeconds, 1));
            double remaining = mass;
            for (int i = 0; i < drops.Count; i++)
            {
                double part = i == drops.Count - 1 ? remaining : Math.Round(mass / drops.Count * 2) / 2;
                part = Math.Max(0.5, Math.Min(part, remaining));
                remaining -= part;
                terms.Loads.Add(new FreightLoad { LegIndex = i + 1, MassTonnes = part });
                o.LegTemplates.Add(Leg(i + 1, 1, LegKind.Unload, drops[i], CareerBalance.FreightDockRadius, est[i].DistanceMeters, est[i].TimeSeconds, 1));
            }
            double handling = HandlingSeconds(cargo, mass) * 2;
            terms.EstimateSeconds = approach.TimeSeconds + plannedSeconds + handling;
            long value = ContractValue(terms, o.LegTemplates);
            o.TitleKey = "DTC.Freight.Offer." + cargo;
            o.Title = cargo + " freight, " + mass.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + " t, " + drops.Count + " drop(s)";
            o.ApproachMeters = approach.DistanceMeters;
            o.ApproachSeconds = approach.TimeSeconds;
            o.PlannedMeters = planned;
            o.PlannedSeconds = plannedSeconds;
            o.ContractValueCents = value;
            o.PayMinCents = CareerMath.RoundCents(value * CareerBalance.EvalLowMultiplier);
            o.PayMaxCents = CareerMath.RoundCents(value * CareerBalance.EvalHighMultiplier);
            o.XpPreview = Progression.MissionXp(planned, 100);
            o.DeadlineSeconds = terms.EstimateSeconds * CareerBalance.FreightDeadlineSlack;
            o.Cargo = cargo;
            o.MassTonnes = mass;
            o.AllowedClasses.Add(VehicleClass.RigidTruck);
            o.ProvidesServiceVehicle = serviceTruck;
            o.ServiceModelId = serviceTruck ? CareerCatalog.ServiceTruckModelId : null;
            o.ServiceSpawnPlaceId = serviceTruck ? pickup.Id : null;
            o.FreightTemplate = terms;
            return o;
        }

        /// <summary>40 cr per load + 3 cr per tonne-km carried on each planned leg; heavy x1.2; service truck x0.6.</summary>
        public static long ContractValue(FreightTerms t, IReadOnlyList<MissionLeg> legs)
        {
            double carried = t.MassTonnes;
            double tonneKm = 0;
            foreach (FreightLoad l in t.Loads)
            {
                MissionLeg leg = legs[l.LegIndex];
                tonneKm += carried * leg.PlannedMeters / 1000.0;
                carried -= l.MassTonnes;
            }
            double v = CareerBalance.FreightPerLoadCents * t.Loads.Count + CareerBalance.FreightPerTonneKmCents * tonneKm;
            if (t.Cargo == CargoType.Heavy)
            {
                v *= CareerBalance.FreightHeavyFactor;
            }
            if (t.ServiceTruck)
            {
                v *= CareerBalance.FreightServiceTruckFactor;
            }
            return CareerMath.RoundCents(v);
        }

        /// <summary>General 3 s per 0.5 t pallet up to 20 s; bulk 15 s; heavy 20 s (SPEC-FREIGHT section 6).</summary>
        private static double HandlingSeconds(CargoType cargo, double mass)
        {
            switch (cargo)
            {
                case CargoType.Bulk:
                    return 15;
                case CargoType.Heavy:
                    return 20;
                default:
                    return Math.Min(20, 3 * Math.Ceiling(mass / 0.5));
            }
        }

        public override void OnAccepted(CareerService svc, Mission m, double now)
        {
            FreightTerms t = m.Freight;
            t.SoftDeadline = now + t.EstimateSeconds * CareerBalance.FreightDeadlineSlack;
            t.HardDeadline = now + t.EstimateSeconds * CareerBalance.FreightDeadlineSlack * 1.5;
        }

        public override bool CheckCapacity(Mission m, CareerVehicle v, out string reasonText)
        {
            if (m.Freight.MassTonnes > v.PayloadTonnes + 1e-9)
            {
                reasonText = "Needs " + m.Freight.MassTonnes.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + " t payload: install payload reinforcement or rent a truck";
                return false;
            }
            reasonText = null;
            return true;
        }

        public override double ServiceSeconds(CareerService svc, Mission m, MissionLeg leg)
        {
            double mass = m.Freight.MassTonnes;
            if (leg.Kind == LegKind.Unload)
            {
                foreach (FreightLoad l in m.Freight.Loads)
                {
                    if (l.LegIndex == leg.Index)
                    {
                        mass = l.MassTonnes;
                    }
                }
            }
            return HandlingSeconds(m.Freight.Cargo, mass);
        }

        public override void OnLegDone(CareerService svc, Mission m, MissionLeg leg, PlayerSnapshot s, double now)
        {
            FreightTerms t = m.Freight;
            if (leg.Kind == LegKind.Load)
            {
                foreach (FreightLoad l in t.Loads)
                {
                    if (l.State == LoadState.AtPickup)
                    {
                        l.State = LoadState.Loaded;
                    }
                }
                leg.Outcome = LegOutcome.DoneOk;
                svc.Notify(NotificationLevel.Info, "DTC.Freight.Loaded", "Cargo loaded. Payload affects braking and acceleration.");
                return;
            }
            leg.Outcome = LegOutcome.Failed;
            foreach (FreightLoad l in t.Loads)
            {
                if (l.LegIndex != leg.Index || l.State != LoadState.Loaded)
                {
                    continue;
                }
                if (t.HardDeadline >= 0 && now > t.HardDeadline)
                {
                    l.State = LoadState.Failed;
                    continue;
                }
                l.State = LoadState.Delivered;
                l.DeliveredAt = now;
                l.DeadlineFactor = DeadlineFactor(now, t.SoftDeadline, t.HardDeadline);
                l.IntegrityAtDelivery = t.Integrity;
                l.DockPrecision = CareerMath.Clamp(1.0 - MapPoint.Distance(s.Position, leg.Place.Access) / leg.Radius, 0, 1);
                leg.Outcome = l.DeadlineFactor >= 1 && t.Integrity >= 80 ? LegOutcome.DoneOk : LegOutcome.DonePartial;
            }
        }

        public override void OnContact(CareerService svc, Mission m, ContactSeverity severity, double now)
        {
            double loss = severity == ContactSeverity.Light ? 3 : severity == ContactSeverity.Medium ? 15 : 40;
            Damage(svc, m, loss, now);
        }

        private static double CargoThresholdG(CargoType c) => c == CargoType.Heavy ? 0.35 : c == CargoType.Bulk ? 0.6 : 0.45;

        public override double HarshThreshold(Mission m, bool lateral)
        {
            double g = lateral ? 0.45 : 0.4;
            if (m.Freight.LoadedTonnes > 0)
            {
                g = Math.Min(g, CargoThresholdG(m.Freight.Cargo));
            }
            return g * CareerBalance.Gravity;
        }

        public override void OnHarsh(CareerService svc, Mission m, bool lateral, double magnitudeMps2, double penaltyMultiplier, double now)
        {
            if (magnitudeMps2 / CareerBalance.Gravity >= CargoThresholdG(m.Freight.Cargo))
            {
                Damage(svc, m, 4 * penaltyMultiplier, now);
            }
        }

        private static void Damage(CareerService svc, Mission m, double loss, double now)
        {
            FreightTerms t = m.Freight;
            if (t.LoadedTonnes <= 0)
            {
                return;
            }
            t.Integrity = Math.Max(0, t.Integrity - loss);
            if (t.Integrity > 0)
            {
                return;
            }
            foreach (FreightLoad l in t.Loads)
            {
                if (l.State == LoadState.Loaded)
                {
                    l.State = LoadState.Lost;
                }
            }
            foreach (MissionLeg leg in m.Legs)
            {
                if (!leg.IsResolved && leg.Kind == LegKind.Unload)
                {
                    ResolveLeg(leg, LegOutcome.Failed, now);
                }
            }
            svc.Notify(NotificationLevel.Warning, "DTC.Freight.LoadLost", "The load was lost.");
        }

        public override bool HasObjectsAboard(Mission m) => m.Freight.LoadedTonnes > 0;

        public override void CheckTimers(CareerService svc, Mission m, PlayerSnapshot s, double now)
        {
            FreightTerms t = m.Freight;
            if (t.HardDeadline < 0 || now <= t.HardDeadline)
            {
                return;
            }
            bool changed = false;
            foreach (FreightLoad l in t.Loads)
            {
                if (l.State == LoadState.AtPickup || l.State == LoadState.Loaded)
                {
                    l.State = LoadState.Failed;
                    changed = true;
                }
            }
            if (changed)
            {
                foreach (MissionLeg leg in m.Legs)
                {
                    if (!leg.IsResolved)
                    {
                        ResolveLeg(leg, LegOutcome.Failed, now);
                    }
                }
                svc.Notify(NotificationLevel.Warning, "DTC.Freight.HardDeadline", "The hard deadline passed. The cargo is returned to the shipper.");
            }
        }

        public override void ExtendTimers(Mission m, double seconds)
        {
            if (m.Freight.SoftDeadline >= 0)
            {
                m.Freight.SoftDeadline += seconds;
                m.Freight.HardDeadline += seconds;
            }
        }

        public override void OnLegInvalid(CareerService svc, Mission m, MissionLeg leg, double now)
        {
            if (leg.Kind == LegKind.Load)
            {
                svc.InvalidateMission(m, "DTC.Freight.Invalidated.Pickup", "The loading bay was removed from the city. The contract was cancelled with no fault.");
                return;
            }
            // Substitute drop-off of the same category within 1 km (accepted automatically; pay recalculated).
            MapPoint from = m.HasVehiclePosition ? m.LastVehiclePosition : m.Legs[0].Place.Access;
            var kinds = new[] { leg.Place.Kind };
            foreach (PlaceInfo c in svc.QueryPlacesByDistance(kinds, leg.Place.Access, CareerBalance.FreightSubstituteRadiusMeters, 16))
            {
                if (c.Id == leg.Place.Id || c.Id == m.Legs[0].Place.Id)
                {
                    continue;
                }
                RouteEstimate e = svc.EstimateRoute(from, c.Id, VehicleClass.RigidTruck);
                if (!e.IsReachable)
                {
                    continue;
                }
                string oldName = leg.Place.Name;
                leg.Place = PlaceRef.From(c);
                leg.PlannedMeters = e.DistanceMeters;
                leg.PlannedSeconds = e.TimeSeconds;
                leg.State = LegState.Approaching;
                leg.ServicingStartedAt = -1;
                m.ContractValueCents = ContractValue(m.Freight, m.Legs);
                leg.FailedChecks = 0;
                svc.Notify(NotificationLevel.Warning, "DTC.Freight.Substituted", "The drop-off '" + oldName + "' no longer exists. The cargo goes to '" + c.Name + "' instead; pay was recalculated.", oldName, c.Name);
                return;
            }
            foreach (FreightLoad l in m.Freight.Loads)
            {
                if (l.LegIndex == leg.Index && (l.State == LoadState.Loaded || l.State == LoadState.AtPickup))
                {
                    l.State = LoadState.Skipped;
                }
            }
            ResolveLeg(leg, LegOutcome.SkippedNoFault, now);
            svc.Notify(NotificationLevel.Warning, "DTC.Freight.Skipped", "The drop-off '" + leg.Place.Name + "' can no longer be reached. The cargo was returned with no fee.", leg.Place.Name);
            bool open = false;
            bool delivered = false;
            foreach (FreightLoad l in m.Freight.Loads)
            {
                open |= l.State == LoadState.Loaded || l.State == LoadState.AtPickup;
                delivered |= l.State == LoadState.Delivered;
            }
            if (!open && !delivered)
            {
                svc.InvalidateMission(m, "DTC.Freight.Invalidated.AllSkipped", "No drop-off of this contract can be reached any more. It was cancelled with no fault.");
            }
        }

        public override void OnEnded(Mission m)
        {
            foreach (FreightLoad l in m.Freight.Loads)
            {
                if (l.State == LoadState.Loaded)
                {
                    l.State = LoadState.Returned;
                }
            }
        }

        public override MissionState CompletionOutcome(Mission m)
        {
            foreach (FreightLoad l in m.Freight.Loads)
            {
                if (l.State == LoadState.Delivered)
                {
                    return MissionState.Completed;
                }
            }
            return MissionState.Failed;
        }

        /// <summary>Evaluation = 40% integrity + 30% punctuality + 20% cleanliness + 10% docking (SPEC-FREIGHT section 7).</summary>
        public static double Evaluation(Mission m)
        {
            FreightTerms t = m.Freight;
            int counted = 0;
            int onTime = 0;
            double dock = 0;
            int docked = 0;
            foreach (FreightLoad l in t.Loads)
            {
                if (l.State == LoadState.Skipped)
                {
                    continue;
                }
                counted++;
                if (l.State == LoadState.Delivered)
                {
                    dock += l.DockPrecision;
                    docked++;
                    if (t.SoftDeadline < 0 || l.DeliveredAt <= t.SoftDeadline)
                    {
                        onTime++;
                    }
                }
            }
            double punctuality = counted > 0 ? 100.0 * onTime / counted : 100;
            double docking = docked > 0 ? 100.0 * dock / docked : 0;
            return 0.4 * t.Integrity + 0.3 * punctuality + 0.2 * m.Cleanliness + 0.1 * docking;
        }

        private static double LoadPay(Mission m, FreightLoad l)
        {
            FreightTerms t = m.Freight;
            double share = t.MassTonnes > 0 ? l.MassTonnes / t.MassTonnes : 1.0 / t.Loads.Count;
            return m.ContractValueCents * share * IntegrityFactor(l.IntegrityAtDelivery) * l.DeadlineFactor;
        }

        public override SettlementResult Settle(Mission m)
        {
            SettlementResult r = NewResult(m);
            FreightTerms t = m.Freight;
            double eval = Evaluation(m);
            r.Evaluation = eval;
            double sum = 0;
            int delivered = 0;
            bool loadedEver = m.Legs.Count > 0 && m.Legs[0].IsResolved && m.Legs[0].Outcome == LegOutcome.DoneOk;
            foreach (FreightLoad l in t.Loads)
            {
                if (l.State == LoadState.Delivered)
                {
                    sum += LoadPay(m, l);
                    delivered++;
                }
                loadedEver |= l.State == LoadState.Loaded || l.State == LoadState.Delivered || l.State == LoadState.Lost || l.State == LoadState.Returned;
            }
            double share = t.Loads.Count > 0 ? (double)delivered / t.Loads.Count : 0;
            if (m.State == MissionState.Completed)
            {
                long pay = CareerMath.RoundCents(sum);
                r.AddLine("DTC.Settle.Freight.Loads", "Loads delivered (" + delivered + ")", pay);
                long bonus = CareerMath.RoundCents(sum * Progression.EvaluationMultiplier(eval)) - pay;
                if (bonus != 0)
                {
                    r.AddLine("DTC.Settle.Evaluation", "Driving evaluation", bonus);
                }
                r.Xp = Progression.MissionXp(m.PlannedMeters, eval);
                r.ReputationDelta = Progression.CompletionReputation(eval);
            }
            else if (m.State == MissionState.Cancelled && m.Fault == MissionFault.NoFault)
            {
                long pay = CareerMath.RoundCents(sum);
                if (pay > 0)
                {
                    r.AddLine("DTC.Settle.Freight.Loads", "Loads delivered (" + delivered + ")", pay);
                }
                r.Xp = Progression.MissionXp(m.PlannedMeters, eval, share);
            }
            else if (m.State == MissionState.Cancelled)
            {
                if (!loadedEver)
                {
                    r.ReputationDelta = -1;
                }
                else
                {
                    long pay = CareerMath.RoundCents(sum * 0.5);
                    if (pay > 0)
                    {
                        r.AddLine("DTC.Settle.Freight.PartialLoads", "Delivered loads at 50%", pay);
                    }
                    long fee = CancellationFee(m);
                    r.AddLine("DTC.Settle.ReturnFee", "Return fee", -fee);
                    r.FeeCents = fee;
                    r.ReputationDelta = -3;
                }
            }
            else if (m.State == MissionState.Failed)
            {
                r.ReputationDelta = CareerBalance.ReputationFailed;
            }
            return r;
        }
    }
}
