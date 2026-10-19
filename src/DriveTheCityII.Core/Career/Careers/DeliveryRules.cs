using System;
using System.Collections.Generic;

namespace DriveTheCityII.Core.Career
{
    /// <summary>
    /// Urban deliveries (SPEC-DELIVERY; REQ-DELIV-001): one pickup, 1..N drop-offs in any order (one unordered leg
    /// group), soft and hard deadlines, parcel fragility and integrity, and a driving evaluation that changes pay.
    /// Parcels are mod-owned career cargo; deliveries never fulfil native demand (REQ-CITY-003).
    /// </summary>
    internal sealed class DeliveryRules : CareerRules
    {
        private static readonly PlaceKind[] s_PickupKinds = { PlaceKind.Commercial, PlaceKind.Warehouse, PlaceKind.Industrial };
        private static readonly PlaceKind[] s_DropKinds = { PlaceKind.Residential, PlaceKind.Commercial, PlaceKind.Office };

        // Integrity loss by fragility (rows) and severity (light, medium, severe), SPEC-DELIVERY section 5.
        private static readonly double[,] s_ContactDamage = { { 5, 20, 50 }, { 10, 35, 80 }, { 20, 60, 100 } };

        public override CareerKind Kind => CareerKind.Delivery;

        public override Unlock RequiredUnlock => Unlock.DeliveryTier1;

        public override List<MissionOffer> Generate(CareerService svc, MapPoint near, double now, int count)
        {
            List<MissionOffer> offers = TryGenerate(svc, near, now, count, false);
            if (offers.Count == 0)
            {
                offers = TryGenerate(svc, near, now, 1, true);
            }
            return offers;
        }

        private List<MissionOffer> TryGenerate(CareerService svc, MapPoint near, double now, int count, bool starter)
        {
            var offers = new List<MissionOffer>();
            Progression prog = svc.State.Progression;
            bool vanOk = prog.IsUnlocked(Unlock.VanRental) || svc.PlayerHasClass(VehicleClass.Van);
            bool level3 = prog.IsUnlocked(Unlock.FragileExpressContracts);
            List<PlaceInfo> pickups = svc.QueryPlacesSorted(s_PickupKinds, near, 2500, 32);
            List<PlaceInfo> drops = svc.QueryPlacesSorted(s_DropKinds, near, 2500 + 6000, 96);
            DeterministicRandom rng = svc.State.Rng;
            rng.Shuffle(pickups);
            int budget = 80;
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
                DeliveryContractType type = DeliveryContractType.Courier;
                if (!starter)
                {
                    var types = new List<DeliveryContractType> { DeliveryContractType.Courier, DeliveryContractType.Route };
                    if (level3)
                    {
                        types.Add(DeliveryContractType.Express);
                        if (vanOk)
                        {
                            types.Add(DeliveryContractType.Fragile);
                        }
                    }
                    type = types[rng.NextInt(types.Count)];
                }
                int dropCount;
                switch (type)
                {
                    case DeliveryContractType.Route:
                        dropCount = vanOk ? rng.NextRange(2, 6) : 2;
                        break;
                    case DeliveryContractType.Express:
                        dropCount = rng.NextRange(1, vanOk ? 3 : 2);
                        break;
                    case DeliveryContractType.Fragile:
                        dropCount = rng.NextRange(1, 4);
                        break;
                    default:
                        dropCount = 1;
                        break;
                }
                // Parcels per drop: one parcel each for car-sized work; vans may carry 1-2 parcels of size 1-2.
                bool vanWork = type == DeliveryContractType.Fragile || (vanOk && dropCount > 2);
                VehicleClass cls = vanWork ? VehicleClass.Van : VehicleClass.Car;
                RouteEstimate approach = svc.EstimateRoute(near, pickup.Id, cls);
                budget--;
                if (!approach.IsReachable || approach.DistanceMeters > 4000)
                {
                    continue;
                }
                double minLeg = starter ? 200 : (type == DeliveryContractType.Courier ? 800 : 300);
                double maxLeg = type == DeliveryContractType.Courier ? 5000 : 6000;
                var chosen = new List<PlaceInfo>();
                var legEst = new List<RouteEstimate>();
                var shuffled = new List<PlaceInfo>(drops);
                rng.Shuffle(shuffled);
                MapPoint prev = pickup.Access;
                foreach (PlaceInfo d in shuffled)
                {
                    if (chosen.Count >= dropCount || budget <= 0)
                    {
                        break;
                    }
                    if (d.Id == pickup.Id || ContainsId(chosen, d.Id))
                    {
                        continue;
                    }
                    double straight = MapPoint.DistanceXZ(prev, d.Access);
                    if (straight > maxLeg || straight < minLeg * 0.3)
                    {
                        continue;
                    }
                    RouteEstimate fromPickup = chosen.Count == 0 ? default : svc.EstimateRoute(pickup.Access, d.Id, cls);
                    RouteEstimate e = svc.EstimateRoute(prev, d.Id, cls);
                    budget -= chosen.Count == 0 ? 1 : 2;
                    if (!e.IsReachable || e.DistanceMeters < minLeg || e.DistanceMeters > maxLeg)
                    {
                        continue;
                    }
                    if (chosen.Count > 0 && !fromPickup.IsReachable)
                    {
                        continue;
                    }
                    chosen.Add(d);
                    legEst.Add(e);
                    prev = d.Access;
                }
                if (chosen.Count < dropCount)
                {
                    continue;
                }
                double planned = 0;
                double plannedSeconds = 0;
                foreach (RouteEstimate e in legEst)
                {
                    planned += e.DistanceMeters;
                    plannedSeconds += e.TimeSeconds;
                }
                if (!starter && type == DeliveryContractType.Route && (planned < 3000 || planned > 12000))
                {
                    continue;
                }
                offers.Add(Build(svc, near, now, type, cls, pickup, chosen, legEst, approach, planned, plannedSeconds, vanOk));
            }
            return offers;
        }

        private static bool ContainsId(List<PlaceInfo> list, string id)
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

        private MissionOffer Build(CareerService svc, MapPoint near, double now, DeliveryContractType type, VehicleClass cls, PlaceInfo pickup, List<PlaceInfo> drops, List<RouteEstimate> legEst, RouteEstimate approach, double planned, double plannedSeconds, bool vanOk)
        {
            DeterministicRandom rng = svc.State.Rng;
            var terms = new DeliveryTerms { Type = type, Slack = type == DeliveryContractType.Express ? 1.25 : 1.5 };
            MissionOffer o = svc.NewOffer(CareerKind.Delivery, near, now);
            int parcelIndex = 0;
            int slots = 0;
            o.LegTemplates.Add(Leg(0, 0, LegKind.Load, pickup, CareerBalance.DefaultServiceRadius, approach.DistanceMeters, approach.TimeSeconds, 0));
            for (int i = 0; i < drops.Count; i++)
            {
                int perDrop = cls == VehicleClass.Van ? rng.NextRange(1, 2) : 1;
                for (int k = 0; k < perDrop; k++)
                {
                    int size = cls == VehicleClass.Van ? rng.NextRange(1, 2) : (drops.Count == 1 ? rng.NextRange(1, 2) : 1);
                    if (slots + size > CareerBalance.VanParcelSlots)
                    {
                        break;
                    }
                    Fragility f = Fragility.Standard;
                    if (type == DeliveryContractType.Fragile)
                    {
                        f = rng.NextDouble() < 0.3 ? Fragility.VeryFragile : Fragility.Fragile;
                    }
                    terms.Parcels.Add(new Parcel { Id = "p" + CareerMath.Inv(++parcelIndex), Size = size, Fragility = f, LegIndex = i + 1 });
                    slots += size;
                }
                int countForDrop = 0;
                foreach (Parcel p in terms.Parcels)
                {
                    if (p.LegIndex == i + 1)
                    {
                        countForDrop++;
                    }
                }
                o.LegTemplates.Add(Leg(i + 1, 1, LegKind.Dropoff, drops[i], CareerBalance.DeliveryDropRadius, legEst[i].DistanceMeters, legEst[i].TimeSeconds, countForDrop));
            }
            o.LegTemplates[0].Count = terms.Parcels.Count;
            double handling = terms.Parcels.Count * (CareerBalance.DeliveryLoadSecondsPerParcel + CareerBalance.DeliveryUnloadSecondsPerParcel);
            terms.EstimateSeconds = approach.TimeSeconds + plannedSeconds + handling;
            long value = ContractValue(type, terms.Parcels.Count, planned);

            o.TitleKey = "DTC.Delivery.Offer." + type;
            o.Title = type + " delivery: " + terms.Parcels.Count + " parcel(s), " + drops.Count + " stop(s)";
            o.ApproachMeters = approach.DistanceMeters;
            o.ApproachSeconds = approach.TimeSeconds;
            o.PlannedMeters = planned;
            o.PlannedSeconds = plannedSeconds;
            o.ContractValueCents = value;
            o.PayMinCents = CareerMath.RoundCents(value * CareerBalance.EvalLowMultiplier);
            o.PayMaxCents = CareerMath.RoundCents(value * CareerBalance.EvalHighMultiplier * (type == DeliveryContractType.Express ? 1.1 : 1.0));
            o.XpPreview = Progression.MissionXp(planned, 100);
            o.DeadlineSeconds = terms.EstimateSeconds * terms.Slack;
            o.DeliveryType = type;
            o.ParcelCount = terms.Parcels.Count;
            if (slots <= CareerBalance.CarParcelSlots && type != DeliveryContractType.Fragile)
            {
                o.AllowedClasses.Add(VehicleClass.Car);
                o.AllowedClasses.Add(VehicleClass.Taxi);
            }
            o.AllowedClasses.Add(VehicleClass.Van);
            if (!o.AllowedClasses.Contains(VehicleClass.Car) && !vanOk)
            {
                o.Eligible = false;
                o.IneligibleKey = "DTC.Delivery.Ineligible.Van";
                o.IneligibleText = Progression.RequirementText(Unlock.VanRental) + " (needs a van)";
            }
            o.DeliveryTemplate = terms;
            return o;
        }

        /// <summary>Contract value: 12 cr per parcel + 4 cr per planned km, x 1.3 fragile, x 1.4 express (SPEC-ECON 8).</summary>
        public static long ContractValue(DeliveryContractType type, int parcels, double plannedMeters)
        {
            double v = CareerBalance.DeliveryPerParcelCents * parcels + CareerBalance.DeliveryPerKmCents * plannedMeters / 1000.0;
            return CareerMath.RoundCents(v * TypeFactor(type));
        }

        private static double TypeFactor(DeliveryContractType type)
        {
            return type == DeliveryContractType.Fragile ? CareerBalance.DeliveryFragileFactor : type == DeliveryContractType.Express ? CareerBalance.DeliveryExpressFactor : 1.0;
        }

        public override void OnAccepted(CareerService svc, Mission m, double now)
        {
            DeliveryTerms t = m.Delivery;
            t.SoftDeadline = now + t.EstimateSeconds * t.Slack;
            t.HardDeadline = now + t.EstimateSeconds * t.Slack * 1.5;
        }

        public override bool CheckCapacity(Mission m, CareerVehicle v, out string reasonText)
        {
            int slots = 0;
            foreach (Parcel p in m.Delivery.Parcels)
            {
                slots += p.Size;
            }
            if (slots > v.ParcelSlots)
            {
                reasonText = "Needs " + slots + " parcel slots: rent a van";
                return false;
            }
            reasonText = null;
            return true;
        }

        public override double ServiceSeconds(CareerService svc, Mission m, MissionLeg leg)
        {
            int n = 0;
            foreach (Parcel p in m.Delivery.Parcels)
            {
                if (leg.Kind == LegKind.Load ? p.State == ParcelState.AtPickup : (p.LegIndex == leg.Index && p.State == ParcelState.Loaded))
                {
                    n++;
                }
            }
            return leg.Kind == LegKind.Load ? n * CareerBalance.DeliveryLoadSecondsPerParcel : Math.Max(0.5, n * CareerBalance.DeliveryUnloadSecondsPerParcel);
        }

        public override void OnServicingProgress(CareerService svc, Mission m, MissionLeg leg, double elapsed)
        {
            if (leg.Kind != LegKind.Load)
            {
                return;
            }
            // Parcels load one by one; moving aborts the rest, loaded parcels stay loaded (SPEC-DELIVERY section 6).
            int units = (int)Math.Floor(elapsed / CareerBalance.DeliveryLoadSecondsPerParcel);
            while (leg.ProgressUnits < units)
            {
                Parcel next = null;
                foreach (Parcel p in m.Delivery.Parcels)
                {
                    if (p.State == ParcelState.AtPickup)
                    {
                        next = p;
                        break;
                    }
                }
                if (next == null)
                {
                    break;
                }
                next.State = ParcelState.Loaded;
                leg.ProgressUnits++;
            }
        }

        public override void OnLegDone(CareerService svc, Mission m, MissionLeg leg, PlayerSnapshot s, double now)
        {
            DeliveryTerms t = m.Delivery;
            if (leg.Kind == LegKind.Load)
            {
                foreach (Parcel p in t.Parcels)
                {
                    if (p.State == ParcelState.AtPickup)
                    {
                        p.State = ParcelState.Loaded;
                    }
                }
                leg.Outcome = LegOutcome.DoneOk;
                svc.Notify(NotificationLevel.Info, "DTC.Delivery.Loaded", "Parcels loaded. Deliver them in any order.");
                return;
            }
            int delivered = 0;
            bool perfect = true;
            foreach (Parcel p in t.Parcels)
            {
                if (p.LegIndex != leg.Index || p.State != ParcelState.Loaded)
                {
                    continue;
                }
                if (t.HardDeadline >= 0 && now > t.HardDeadline)
                {
                    p.State = ParcelState.Failed;
                    perfect = false;
                    continue;
                }
                p.State = ParcelState.Delivered;
                p.DeliveredAt = now;
                double f = DeadlineFactor(now, t.SoftDeadline, t.HardDeadline);
                if (t.Type == DeliveryContractType.Express && f >= 1.0)
                {
                    f = 1.1;
                }
                p.DeadlineFactor = f;
                delivered++;
                if (f < 1.0 || p.Integrity < 80)
                {
                    perfect = false;
                }
            }
            leg.Outcome = delivered == 0 ? LegOutcome.Failed : (perfect ? LegOutcome.DoneOk : LegOutcome.DonePartial);
        }

        public override void OnContact(CareerService svc, Mission m, ContactSeverity severity, double now)
        {
            int col = severity == ContactSeverity.Light ? 0 : severity == ContactSeverity.Medium ? 1 : 2;
            bool destroyed = false;
            foreach (Parcel p in m.Delivery.Parcels)
            {
                if (p.State != ParcelState.Loaded)
                {
                    continue;
                }
                p.Integrity = Math.Max(0, p.Integrity - s_ContactDamage[(int)p.Fragility, col]);
                if (p.Integrity <= 0)
                {
                    p.State = ParcelState.Destroyed;
                    destroyed = true;
                }
            }
            if (destroyed)
            {
                svc.Notify(NotificationLevel.Warning, "DTC.Delivery.ParcelDestroyed", "A parcel was destroyed by the impact.");
                ResolveDeadLegs(m, now);
            }
        }

        public override double HarshThreshold(Mission m, bool lateral)
        {
            double g = lateral ? 0.45 : 0.4;
            foreach (Parcel p in m.Delivery.Parcels)
            {
                if (p.State == ParcelState.Loaded)
                {
                    g = Math.Min(g, FragilityThreshold(p.Fragility));
                }
            }
            return g * CareerBalance.Gravity;
        }

        private static double FragilityThreshold(Fragility f) => f == Fragility.VeryFragile ? 0.35 : f == Fragility.Fragile ? 0.45 : 0.6;

        public override void OnHarsh(CareerService svc, Mission m, bool lateral, double magnitudeMps2, double penaltyMultiplier, double now)
        {
            double g = magnitudeMps2 / CareerBalance.Gravity;
            bool destroyed = false;
            foreach (Parcel p in m.Delivery.Parcels)
            {
                if (p.State != ParcelState.Loaded || p.Fragility == Fragility.Standard || g < FragilityThreshold(p.Fragility))
                {
                    continue;
                }
                double damage = (p.Fragility == Fragility.VeryFragile ? 8 : 5) * penaltyMultiplier;
                p.Integrity = Math.Max(0, p.Integrity - damage);
                if (p.Integrity <= 0)
                {
                    p.State = ParcelState.Destroyed;
                    destroyed = true;
                }
            }
            if (destroyed)
            {
                ResolveDeadLegs(m, now);
            }
        }

        /// <summary>Resolves legs that have nothing left to deliver (destroyed, failed or skipped parcels).</summary>
        private static void ResolveDeadLegs(Mission m, double now)
        {
            foreach (MissionLeg leg in m.Legs)
            {
                if (leg.IsResolved)
                {
                    continue;
                }
                bool pending = false;
                bool delivered = false;
                foreach (Parcel p in m.Delivery.Parcels)
                {
                    bool mine = leg.Kind == LegKind.Load || p.LegIndex == leg.Index;
                    if (!mine)
                    {
                        continue;
                    }
                    if (p.State == ParcelState.AtPickup || (leg.Kind != LegKind.Load && p.State == ParcelState.Loaded))
                    {
                        pending = true;
                    }
                    if (p.State == ParcelState.Delivered)
                    {
                        delivered = true;
                    }
                }
                if (!pending)
                {
                    ResolveLeg(leg, delivered ? LegOutcome.DonePartial : LegOutcome.Failed, now);
                }
            }
        }

        public override bool HasObjectsAboard(Mission m)
        {
            foreach (Parcel p in m.Delivery.Parcels)
            {
                if (p.State == ParcelState.Loaded)
                {
                    return true;
                }
            }
            return false;
        }

        public override void CheckTimers(CareerService svc, Mission m, PlayerSnapshot s, double now)
        {
            DeliveryTerms t = m.Delivery;
            if (t.HardDeadline < 0 || now <= t.HardDeadline)
            {
                return;
            }
            bool changed = false;
            foreach (Parcel p in t.Parcels)
            {
                if (p.State == ParcelState.AtPickup || p.State == ParcelState.Loaded)
                {
                    p.State = ParcelState.Failed;
                    changed = true;
                }
            }
            if (changed)
            {
                svc.Notify(NotificationLevel.Warning, "DTC.Delivery.HardDeadline", "The hard deadline passed. Undelivered parcels are returned to the shipper.");
                ResolveDeadLegs(m, now);
            }
        }

        public override void ExtendTimers(Mission m, double seconds)
        {
            if (m.Delivery.SoftDeadline >= 0)
            {
                m.Delivery.SoftDeadline += seconds;
                m.Delivery.HardDeadline += seconds;
            }
        }

        public override void OnLegInvalid(CareerService svc, Mission m, MissionLeg leg, double now)
        {
            if (leg.Kind == LegKind.Load)
            {
                svc.InvalidateMission(m, "DTC.Delivery.Invalidated.Pickup", "The pickup was removed from the city. The job was cancelled with no fault.");
                return;
            }
            foreach (Parcel p in m.Delivery.Parcels)
            {
                if (p.LegIndex == leg.Index && (p.State == ParcelState.AtPickup || p.State == ParcelState.Loaded))
                {
                    p.State = ParcelState.Skipped;
                }
            }
            ResolveLeg(leg, LegOutcome.SkippedNoFault, now);
            svc.Notify(NotificationLevel.Warning, "DTC.Delivery.Skipped", "The address '" + leg.Place.Name + "' can no longer be reached. Its parcels were returned with no penalty.", leg.Place.Name);
            bool anyOpen = false;
            bool anyDelivered = false;
            foreach (Parcel p in m.Delivery.Parcels)
            {
                anyOpen |= p.State == ParcelState.AtPickup || p.State == ParcelState.Loaded;
                anyDelivered |= p.State == ParcelState.Delivered;
            }
            if (!anyOpen && !anyDelivered)
            {
                svc.InvalidateMission(m, "DTC.Delivery.Invalidated.AllSkipped", "No address of this job can be reached any more. The job was cancelled with no fault.");
            }
        }

        public override void OnEnded(Mission m)
        {
            foreach (Parcel p in m.Delivery.Parcels)
            {
                if (p.State == ParcelState.Loaded)
                {
                    p.State = ParcelState.Returned;
                }
            }
        }

        public override MissionState CompletionOutcome(Mission m)
        {
            foreach (Parcel p in m.Delivery.Parcels)
            {
                if (p.State == ParcelState.Delivered)
                {
                    return MissionState.Completed;
                }
            }
            return MissionState.Failed;
        }

        /// <summary>Score 0-100 = 50% average integrity + 30% punctuality + 20% cleanliness (SPEC-DELIVERY section 7).</summary>
        public static double Evaluation(Mission m)
        {
            DeliveryTerms t = m.Delivery;
            double integrity = 0;
            int counted = 0;
            int onTime = 0;
            foreach (Parcel p in t.Parcels)
            {
                if (p.State == ParcelState.Skipped)
                {
                    continue;
                }
                counted++;
                integrity += p.Integrity;
                if (p.State == ParcelState.Delivered && (t.SoftDeadline < 0 || p.DeliveredAt <= t.SoftDeadline))
                {
                    onTime++;
                }
            }
            if (counted == 0)
            {
                return m.Cleanliness;
            }
            return 0.5 * (integrity / counted) + 0.3 * (100.0 * onTime / counted) + 0.2 * m.Cleanliness;
        }

        private static double ParcelPay(Mission m, Parcel p)
        {
            DeliveryTerms t = m.Delivery;
            double basePer = (CareerBalance.DeliveryPerParcelCents + CareerBalance.DeliveryPerKmCents * m.PlannedMeters / 1000.0 / t.Parcels.Count) * TypeFactor(t.Type);
            return basePer * IntegrityFactor(p.Integrity) * p.DeadlineFactor;
        }

        public override SettlementResult Settle(Mission m)
        {
            SettlementResult r = NewResult(m);
            DeliveryTerms t = m.Delivery;
            double eval = Evaluation(m);
            r.Evaluation = eval;
            double sum = 0;
            int delivered = 0;
            // "After loading": the load leg finished, or a parcel was loaded at some point (Returned = was Loaded).
            bool loadedEver = m.Legs.Count > 0 && m.Legs[0].IsResolved && m.Legs[0].Outcome == LegOutcome.DoneOk;
            foreach (Parcel p in t.Parcels)
            {
                if (p.State == ParcelState.Delivered)
                {
                    sum += ParcelPay(m, p);
                    delivered++;
                }
                loadedEver |= p.State == ParcelState.Loaded || p.State == ParcelState.Delivered || p.State == ParcelState.Destroyed || p.State == ParcelState.Returned;
            }
            double share = t.Parcels.Count > 0 ? (double)delivered / t.Parcels.Count : 0;
            if (m.State == MissionState.Completed)
            {
                long parcels = CareerMath.RoundCents(sum);
                r.AddLine("DTC.Settle.Delivery.Parcels", "Parcels delivered (" + delivered + ")", parcels);
                double mult = Progression.EvaluationMultiplier(eval);
                long bonus = CareerMath.RoundCents(sum * mult) - parcels;
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
                    r.AddLine("DTC.Settle.Delivery.Parcels", "Parcels delivered (" + delivered + ")", pay);
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
                        r.AddLine("DTC.Settle.Delivery.PartialParcels", "Delivered parcels at 50%", pay);
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
