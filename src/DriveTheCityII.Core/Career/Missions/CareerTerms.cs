using System.Collections.Generic;
using DriveTheCityII.Core.Persistence;

namespace DriveTheCityII.Core.Career
{
    public enum TaxiPreference
    {
        Standard = 0,
        InAHurry = 1,
        Comfort = 2,
    }

    /// <summary>State of the mod-owned career passenger group (never native citizens, REQ-TAXI-002).</summary>
    public enum PassengerState
    {
        Waiting = 0,
        Aboard = 1,
        Alighted = 2,
    }

    /// <summary>Taxi fare terms and ride-quality accumulators (SPEC-TAXI).</summary>
    public sealed class TaxiTerms
    {
        public int Passengers = 1;
        public TaxiPreference Preference;
        public double PatienceSeconds = CareerBalance.TaxiPatienceSeconds;
        public double PatienceDeadline = -1;
        public double TripMeters;
        public double TripEstimateSeconds;
        public long BaseFareCents;
        public PassengerState PassengerState;
        public double Quality = 100;
        public int SevereContacts;
        public double PickedUpAt = -1;
        public double DroppedAt = -1;
        public MapPoint PickupPoint;

        /// <summary>Share of the fare paid on a no-fault end with passengers aboard (planned distance covered).</summary>
        public double PayShare;

        public TaxiTerms Clone() => (TaxiTerms)MemberwiseClone();

        internal JsonValue ToJson()
        {
            return JsonValue.Object()
                .Set("passengers", Passengers)
                .Set("preference", Preference.ToString())
                .Set("patienceSeconds", PatienceSeconds)
                .Set("patienceDeadline", PatienceDeadline)
                .Set("tripMeters", TripMeters)
                .Set("tripSeconds", TripEstimateSeconds)
                .Set("baseFare", BaseFareCents)
                .Set("passengerState", PassengerState.ToString())
                .Set("quality", Quality)
                .Set("severe", SevereContacts)
                .Set("pickedUpAt", PickedUpAt)
                .Set("droppedAt", DroppedAt)
                .Set("pickupPoint", JsonRead.WritePoint(PickupPoint))
                .Set("payShare", PayShare);
        }

        internal static TaxiTerms FromJson(JsonValue o, string path, JsonRead r)
        {
            return new TaxiTerms
            {
                Passengers = r.Int(o, "passengers", path, 1, 3),
                Preference = r.Enum<TaxiPreference>(o, "preference", path),
                PatienceSeconds = r.Num(o, "patienceSeconds", path, 0),
                PatienceDeadline = r.Num(o, "patienceDeadline", path, -1),
                TripMeters = r.Num(o, "tripMeters", path, 0),
                TripEstimateSeconds = r.Num(o, "tripSeconds", path, 0),
                BaseFareCents = r.Long(o, "baseFare", path, 0),
                PassengerState = r.Enum<PassengerState>(o, "passengerState", path),
                Quality = r.Num(o, "quality", path, 0, 100),
                SevereContacts = r.Int(o, "severe", path, 0, 1000),
                PickedUpAt = r.Num(o, "pickedUpAt", path, -1),
                DroppedAt = r.Num(o, "droppedAt", path, -1),
                PickupPoint = r.Point(o, "pickupPoint", path),
                PayShare = r.Num(o, "payShare", path, 0, 1),
            };
        }
    }

    public enum DeliveryContractType
    {
        Courier = 0,
        Route = 1,
        Express = 2,
        Fragile = 3,
    }

    public enum Fragility
    {
        Standard = 0,
        Fragile = 1,
        VeryFragile = 2,
    }

    public enum ParcelState
    {
        AtPickup = 0,
        Loaded = 1,
        Delivered = 2,
        Destroyed = 3,
        Skipped = 4,
        Returned = 5,
        Failed = 6,
    }

    /// <summary>A mod-owned career parcel (never native goods, REQ-CITY-003).</summary>
    public sealed class Parcel
    {
        public string Id;
        public int Size = 1;
        public Fragility Fragility;
        public double Integrity = 100;
        public ParcelState State;
        public int LegIndex;
        public double DeliveredAt = -1;
        public double DeadlineFactor = 1;

        public Parcel Clone() => (Parcel)MemberwiseClone();

        internal JsonValue ToJson()
        {
            return JsonValue.Object()
                .Set("id", Id)
                .Set("size", Size)
                .Set("fragility", Fragility.ToString())
                .Set("integrity", Integrity)
                .Set("state", State.ToString())
                .Set("leg", LegIndex)
                .Set("deliveredAt", DeliveredAt)
                .Set("deadlineFactor", DeadlineFactor);
        }

        internal static Parcel FromJson(JsonValue o, string path, JsonRead r)
        {
            return new Parcel
            {
                Id = r.Str(o, "id", path),
                Size = r.Int(o, "size", path, 1, 2),
                Fragility = r.Enum<Fragility>(o, "fragility", path),
                Integrity = r.Num(o, "integrity", path, 0, 100),
                State = r.Enum<ParcelState>(o, "state", path),
                LegIndex = r.Int(o, "leg", path, 0, 10000),
                DeliveredAt = r.Num(o, "deliveredAt", path, -1),
                DeadlineFactor = r.Num(o, "deadlineFactor", path, 0, 2),
            };
        }
    }

    /// <summary>Delivery contract terms (SPEC-DELIVERY).</summary>
    public sealed class DeliveryTerms
    {
        public DeliveryContractType Type;
        public List<Parcel> Parcels = new List<Parcel>();
        public double Slack = 1.5;
        public double EstimateSeconds;
        public double SoftDeadline = -1;
        public double HardDeadline = -1;

        public DeliveryTerms Clone()
        {
            var t = (DeliveryTerms)MemberwiseClone();
            t.Parcels = new List<Parcel>();
            foreach (Parcel p in Parcels)
            {
                t.Parcels.Add(p.Clone());
            }
            return t;
        }

        internal JsonValue ToJson()
        {
            JsonValue parcels = JsonValue.Array();
            foreach (Parcel p in Parcels)
            {
                parcels.Add(p.ToJson());
            }
            return JsonValue.Object()
                .Set("type", Type.ToString())
                .Set("slack", Slack)
                .Set("estimateSeconds", EstimateSeconds)
                .Set("softDeadline", SoftDeadline)
                .Set("hardDeadline", HardDeadline)
                .Set("parcels", parcels);
        }

        internal static DeliveryTerms FromJson(JsonValue o, string path, JsonRead r)
        {
            var t = new DeliveryTerms
            {
                Type = r.Enum<DeliveryContractType>(o, "type", path),
                Slack = r.Num(o, "slack", path, 1, 10),
                EstimateSeconds = r.Num(o, "estimateSeconds", path, 0),
                SoftDeadline = r.Num(o, "softDeadline", path, -1),
                HardDeadline = r.Num(o, "hardDeadline", path, -1),
            };
            int i = 0;
            foreach (JsonValue p in r.Arr(o, "parcels", path).Items)
            {
                t.Parcels.Add(Parcel.FromJson(p, path + ".parcels[" + i++ + "]", r));
            }
            if (t.Parcels.Count == 0)
            {
                r.Error(path + ".parcels", "no parcels");
            }
            return t;
        }
    }

    public enum CargoType
    {
        General = 0,
        Bulk = 1,
        Heavy = 2,
    }

    public enum LoadState
    {
        AtPickup = 0,
        Loaded = 1,
        Delivered = 2,
        Lost = 3,
        Returned = 4,
        Skipped = 5,
        Failed = 6,
    }

    /// <summary>One freight drop: the mass delivered at a leg.</summary>
    public sealed class FreightLoad
    {
        public int LegIndex;
        public double MassTonnes;
        public LoadState State;
        public double DeliveredAt = -1;
        public double DeadlineFactor = 1;
        public double IntegrityAtDelivery = 100;
        public double DockPrecision;

        public FreightLoad Clone() => (FreightLoad)MemberwiseClone();

        internal JsonValue ToJson()
        {
            return JsonValue.Object()
                .Set("leg", LegIndex)
                .Set("mass", MassTonnes)
                .Set("state", State.ToString())
                .Set("deliveredAt", DeliveredAt)
                .Set("deadlineFactor", DeadlineFactor)
                .Set("integrity", IntegrityAtDelivery)
                .Set("dock", DockPrecision);
        }

        internal static FreightLoad FromJson(JsonValue o, string path, JsonRead r)
        {
            return new FreightLoad
            {
                LegIndex = r.Int(o, "leg", path, 0, 10000),
                MassTonnes = r.Num(o, "mass", path, 0, 100),
                State = r.Enum<LoadState>(o, "state", path),
                DeliveredAt = r.Num(o, "deliveredAt", path, -1),
                DeadlineFactor = r.Num(o, "deadlineFactor", path, 0, 2),
                IntegrityAtDelivery = r.Num(o, "integrity", path, 0, 100),
                DockPrecision = r.Num(o, "dock", path, 0, 1),
            };
        }
    }

    /// <summary>Freight contract terms (SPEC-FREIGHT). Rigid trucks only; articulated trucks are out of scope.</summary>
    public sealed class FreightTerms
    {
        public CargoType Cargo;
        public double MassTonnes;
        public double Integrity = 100;
        public bool ServiceTruck;
        public List<FreightLoad> Loads = new List<FreightLoad>();
        public double EstimateSeconds;
        public double SoftDeadline = -1;
        public double HardDeadline = -1;

        public FreightTerms Clone()
        {
            var t = (FreightTerms)MemberwiseClone();
            t.Loads = new List<FreightLoad>();
            foreach (FreightLoad l in Loads)
            {
                t.Loads.Add(l.Clone());
            }
            return t;
        }

        public double LoadedTonnes
        {
            get
            {
                double t = 0;
                foreach (FreightLoad l in Loads)
                {
                    if (l.State == LoadState.Loaded)
                    {
                        t += l.MassTonnes;
                    }
                }
                return t;
            }
        }

        internal JsonValue ToJson()
        {
            JsonValue loads = JsonValue.Array();
            foreach (FreightLoad l in Loads)
            {
                loads.Add(l.ToJson());
            }
            return JsonValue.Object()
                .Set("cargo", Cargo.ToString())
                .Set("mass", MassTonnes)
                .Set("integrity", Integrity)
                .Set("serviceTruck", ServiceTruck)
                .Set("estimateSeconds", EstimateSeconds)
                .Set("softDeadline", SoftDeadline)
                .Set("hardDeadline", HardDeadline)
                .Set("loads", loads);
        }

        internal static FreightTerms FromJson(JsonValue o, string path, JsonRead r)
        {
            var t = new FreightTerms
            {
                Cargo = r.Enum<CargoType>(o, "cargo", path),
                MassTonnes = r.Num(o, "mass", path, 0, 100),
                Integrity = r.Num(o, "integrity", path, 0, 100),
                ServiceTruck = r.Bool(o, "serviceTruck", path),
                EstimateSeconds = r.Num(o, "estimateSeconds", path, 0),
                SoftDeadline = r.Num(o, "softDeadline", path, -1),
                HardDeadline = r.Num(o, "hardDeadline", path, -1),
            };
            int i = 0;
            foreach (JsonValue l in r.Arr(o, "loads", path).Items)
            {
                t.Loads.Add(FreightLoad.FromJson(l, path + ".loads[" + i++ + "]", r));
            }
            if (t.Loads.Count == 0)
            {
                r.Error(path + ".loads", "no loads");
            }
            return t;
        }
    }

    /// <summary>Career passengers grouped by destination stop (counts, not individuals; SPEC-BUS section 5).</summary>
    public sealed class PassengerGroup
    {
        public string StopId;
        public int Count;

        public PassengerGroup Clone() => (PassengerGroup)MemberwiseClone();

        internal JsonValue ToJson() => JsonValue.Object().Set("stop", StopId).Set("count", Count);

        internal static PassengerGroup FromJson(JsonValue o, string path, JsonRead r)
        {
            return new PassengerGroup { StopId = r.Str(o, "stop", path), Count = r.Int(o, "count", path, 0, 10000) };
        }
    }

    /// <summary>
    /// Bus shift terms (SPEC-BUS). Passengers are mod-owned career passengers; native ridership is never read as a
    /// count nor changed (REQ-BUS-001, REQ-CITY-001).
    /// </summary>
    public sealed class BusTerms
    {
        public string LineId;
        public string LineName;
        public int LineNumber;

        /// <summary>Version signature of the line's stop sequence at acceptance (or last replanning).</summary>
        public string Signature;

        public int Capacity = CareerBalance.BusCapacity;
        public List<PassengerGroup> Onboard = new List<PassengerGroup>();
        public List<PassengerGroup> Waiting = new List<PassengerGroup>();
        public string WaitingStopId;
        public int Delivered;
        public int Boarded;
        public int Complaints;
        public int StopsServed;
        public int StopsMissed;
        public int OnTime;
        public double Comfort = 100;
        public int SevereContacts;

        public int OnboardCount
        {
            get
            {
                int n = 0;
                foreach (PassengerGroup g in Onboard)
                {
                    n += g.Count;
                }
                return n;
            }
        }

        public BusTerms Clone()
        {
            var t = (BusTerms)MemberwiseClone();
            t.Onboard = CloneGroups(Onboard);
            t.Waiting = CloneGroups(Waiting);
            return t;
        }

        private static List<PassengerGroup> CloneGroups(List<PassengerGroup> src)
        {
            var list = new List<PassengerGroup>();
            foreach (PassengerGroup g in src)
            {
                list.Add(g.Clone());
            }
            return list;
        }

        internal static void AddTo(List<PassengerGroup> groups, string stopId, int count)
        {
            if (count <= 0)
            {
                return;
            }
            foreach (PassengerGroup g in groups)
            {
                if (g.StopId == stopId)
                {
                    g.Count += count;
                    return;
                }
            }
            groups.Add(new PassengerGroup { StopId = stopId, Count = count });
        }

        internal static int TakeFrom(List<PassengerGroup> groups, string stopId)
        {
            for (int i = 0; i < groups.Count; i++)
            {
                if (groups[i].StopId == stopId)
                {
                    int n = groups[i].Count;
                    groups.RemoveAt(i);
                    return n;
                }
            }
            return 0;
        }

        internal JsonValue ToJson()
        {
            JsonValue onboard = JsonValue.Array();
            foreach (PassengerGroup g in Onboard)
            {
                onboard.Add(g.ToJson());
            }
            JsonValue waiting = JsonValue.Array();
            foreach (PassengerGroup g in Waiting)
            {
                waiting.Add(g.ToJson());
            }
            return JsonValue.Object()
                .Set("lineId", LineId)
                .Set("lineName", LineName ?? string.Empty)
                .Set("lineNumber", LineNumber)
                .Set("signature", Signature)
                .Set("capacity", Capacity)
                .Set("onboard", onboard)
                .Set("waiting", waiting)
                .Set("waitingStop", WaitingStopId)
                .Set("delivered", Delivered)
                .Set("boarded", Boarded)
                .Set("complaints", Complaints)
                .Set("served", StopsServed)
                .Set("missed", StopsMissed)
                .Set("onTime", OnTime)
                .Set("comfort", Comfort)
                .Set("severe", SevereContacts);
        }

        internal static BusTerms FromJson(JsonValue o, string path, JsonRead r)
        {
            var t = new BusTerms
            {
                LineId = r.Str(o, "lineId", path),
                LineName = r.Str(o, "lineName", path, false) ?? string.Empty,
                LineNumber = r.Int(o, "lineNumber", path),
                Signature = r.Str(o, "signature", path),
                Capacity = r.Int(o, "capacity", path, 1, 1000),
                WaitingStopId = r.Str(o, "waitingStop", path, false),
                Delivered = r.Int(o, "delivered", path, 0),
                Boarded = r.Int(o, "boarded", path, 0),
                Complaints = r.Int(o, "complaints", path, 0),
                StopsServed = r.Int(o, "served", path, 0),
                StopsMissed = r.Int(o, "missed", path, 0),
                OnTime = r.Int(o, "onTime", path, 0),
                Comfort = r.Num(o, "comfort", path, 0, 100),
                SevereContacts = r.Int(o, "severe", path, 0),
            };
            int i = 0;
            foreach (JsonValue g in r.Arr(o, "onboard", path).Items)
            {
                t.Onboard.Add(PassengerGroup.FromJson(g, path + ".onboard[" + i++ + "]", r));
            }
            i = 0;
            foreach (JsonValue g in r.Arr(o, "waiting", path).Items)
            {
                t.Waiting.Add(PassengerGroup.FromJson(g, path + ".waiting[" + i++ + "]", r));
            }
            if (t.OnboardCount > t.Capacity)
            {
                r.Error(path + ".onboard", "more passengers than capacity");
            }
            return t;
        }
    }
}
