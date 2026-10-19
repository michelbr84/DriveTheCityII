using System;
using System.Collections.Generic;
using DriveTheCityII.Core.Persistence;

namespace DriveTheCityII.Core.Career
{
    /// <summary>The three CareerVehicle subtypes of SPEC-ENTITY (rental split into basic and premium).</summary>
    public enum VehicleKind
    {
        BasicRental = 0,
        PremiumRental = 1,
        Owned = 2,
        Service = 3,
    }

    public enum VehicleHolder
    {
        Player = 0,
        Company = 1,
    }

    /// <summary>Location state of SPEC-GARAGE section 3.</summary>
    public enum VehicleLocationState
    {
        InUse = 0,
        Parked = 1,
        Stored = 2,
        Immobilized = 3,
        Removed = 4,
    }

    public enum ImmobilizedReason
    {
        None = 0,
        OutOfFuel = 1,
        Breakdown = 2,
    }

    /// <summary>Company fleet assignment (SPEC-COMPANY section 3.3).</summary>
    public enum FleetAssignment
    {
        Unassigned = 0,
        PlayerDriven = 1,

        /// <summary>On a managed contract (simulated): no world presence, cannot be driven.</summary>
        Managed = 2,
    }

    public enum FuelWarning
    {
        None = 0,
        Low = 1,
        Critical = 2,
        LimpReserve = 3,
        Empty = 4,
    }

    /// <summary>
    /// Effects of condition, fuel and upgrades for the mod's vehicle model (SPEC-GARAGE sections 6 and 7). Multipliers
    /// apply to acceleration, braking deceleration and lateral grip; <see cref="MaxSpeedMps"/> is a cap
    /// (<see cref="double.PositiveInfinity"/> when none). <see cref="PerformanceFactor"/> summarises condition and fuel
    /// effects in 0..1 (1 = nominal, 0 = immobilized).
    /// </summary>
    public struct VehiclePerformance
    {
        public double PerformanceFactor;
        public double AccelerationMultiplier;
        public double BrakingMultiplier;
        public double GripMultiplier;
        public double MaxSpeedMps;
        public bool Immobilized;
        public ImmobilizedReason Reason;
        public FuelWarning FuelWarning;
        public bool ConditionWarning;

        /// <summary>Harsh-acceleration penalty multiplier for comfort/cargo (comfort suspension: 0.8).</summary>
        public double HarshPenaltyMultiplier;

        public static VehiclePerformance Nominal => new VehiclePerformance
        {
            PerformanceFactor = 1,
            AccelerationMultiplier = 1,
            BrakingMultiplier = 1,
            GripMultiplier = 1,
            MaxSpeedMps = double.PositiveInfinity,
            HarshPenaltyMultiplier = 1,
        };
    }

    /// <summary>
    /// A career vehicle record (SPEC-GARAGE section 3). The world entity is created from it by the mod; the record holds
    /// only stable data (REQ-SAVE-003). Rental terms are stored on the record.
    /// </summary>
    public sealed class CareerVehicle
    {
        public string Id;
        public string ModelId;
        public VehicleClass Class;
        public VehicleKind Kind;
        public VehicleHolder Holder;
        public VehicleLocationState State = VehicleLocationState.Parked;
        public ImmobilizedReason Immobilized;
        public double FuelLiters;
        public double Condition = 100;
        public double OdometerKm;
        public double LimpMeters;
        public MapPoint LastPosition;
        public bool HasPosition;
        public List<UpgradeKind> Upgrades = new List<UpgradeKind>();
        public long PurchasePriceCents;
        public double AcquiredAt;
        public string StoredAtPlaceId;

        // Rental terms (SPEC-GARAGE section 4).
        public string RentalId;
        public double RentalStartedAt;
        public long RentalFeeCents;
        public long DepositCents;
        public double FuelAtStart;
        public double ConditionAtStart;

        // Company fleet (SPEC-COMPANY section 3.3).
        public FleetAssignment Assignment;
        public string AssignedEmployeeId;

        public VehicleModel Model => CareerCatalog.Find(ModelId);

        public bool IsRemoved => State == VehicleLocationState.Removed;

        public bool IsRental => Kind == VehicleKind.BasicRental || Kind == VehicleKind.PremiumRental;

        public bool IsImmobilized => State == VehicleLocationState.Immobilized;

        /// <summary>Fuel and condition are free for the player: basic rental (fuel included) and service vehicles.</summary>
        public bool FuelIncluded => Kind == VehicleKind.BasicRental || Kind == VehicleKind.Service;

        public bool HasUpgrade(UpgradeKind kind) => Upgrades.Contains(kind);

        public double TankLiters
        {
            get
            {
                VehicleModel m = Model;
                double tank = m != null ? m.TankLiters : 40;
                return HasUpgrade(UpgradeKind.ExtendedTank) ? tank * 1.25 : tank;
            }
        }

        public double FuelFraction => TankLiters > 0 ? CareerMath.Clamp(FuelLiters / TankLiters, 0, 1) : 0;

        public int ParcelSlots
        {
            get
            {
                VehicleModel m = Model;
                int slots = m != null ? m.ParcelSlots : 0;
                return HasUpgrade(UpgradeKind.CargoRacks) ? slots + CareerBalance.CargoRackSlots : slots;
            }
        }

        public double PayloadTonnes
        {
            get
            {
                VehicleModel m = Model;
                double p = m != null ? m.PayloadTonnes : 0;
                return HasUpgrade(UpgradeKind.PayloadReinforcement) ? p + CareerBalance.PayloadReinforcementTonnes : p;
            }
        }

        public int PassengerCapacity
        {
            get
            {
                VehicleModel m = Model;
                return m != null ? m.PassengerCapacity : 0;
            }
        }

        /// <summary>
        /// Applies distance driven: fuel per km x (1 + 0.5 x payload fraction) (SPEC-GARAGE 6.1), the 2 km limp reserve,
        /// and wear per km (6.2). Returns true when the vehicle just became immobilized.
        /// </summary>
        internal bool ApplyDistance(double meters, double payloadFraction)
        {
            if (meters <= 0 || IsRemoved)
            {
                return false;
            }
            VehicleModel m = Model;
            OdometerKm += meters / 1000.0;
            bool wasImmobilized = IsImmobilized;
            if (!FuelIncluded && m != null)
            {
                double perMeter = m.ConsumptionLitersPerKm / 1000.0 * (1.0 + 0.5 * CareerMath.Clamp(payloadFraction, 0, 1));
                double need = perMeter * meters;
                if (FuelLiters >= need)
                {
                    FuelLiters -= need;
                }
                else
                {
                    double covered = perMeter > 0 ? FuelLiters / perMeter : meters;
                    FuelLiters = 0;
                    LimpMeters += Math.Max(0, meters - covered);
                    if (LimpMeters >= CareerBalance.LimpReserveMeters)
                    {
                        LimpMeters = CareerBalance.LimpReserveMeters;
                        Immobilize(ImmobilizedReason.OutOfFuel);
                    }
                }
            }
            if (m != null)
            {
                Condition = Math.Max(0, Condition - m.WearPerKm * meters / 1000.0);
            }
            if (Condition <= 0)
            {
                Condition = 0;
                Immobilize(ImmobilizedReason.Breakdown);
            }
            return !wasImmobilized && IsImmobilized;
        }

        /// <summary>Applies contact wear (SPEC-GARAGE 6.2). Returns true when the vehicle just broke down.</summary>
        internal bool ApplyContact(ContactSeverity severity)
        {
            if (severity == ContactSeverity.None || IsRemoved)
            {
                return false;
            }
            bool was = IsImmobilized;
            double loss = severity == ContactSeverity.Light ? CareerBalance.ContactWearLight : severity == ContactSeverity.Medium ? CareerBalance.ContactWearMedium : CareerBalance.ContactWearSevere;
            Condition = Math.Max(0, Condition - loss);
            if (Condition <= 0)
            {
                Immobilize(ImmobilizedReason.Breakdown);
            }
            return !was && IsImmobilized;
        }

        internal void Immobilize(ImmobilizedReason reason)
        {
            State = VehicleLocationState.Immobilized;
            if (Immobilized == ImmobilizedReason.None || reason == ImmobilizedReason.Breakdown)
            {
                Immobilized = reason;
            }
        }

        /// <summary>Clears immobilization once both fuel and condition allow driving again.</summary>
        internal void ReleaseIfDrivable()
        {
            if (!IsImmobilized)
            {
                return;
            }
            bool fuelOk = FuelIncluded || FuelLiters > 0 || LimpMeters < CareerBalance.LimpReserveMeters;
            bool conditionOk = Condition > 0;
            if (fuelOk && conditionOk)
            {
                State = VehicleLocationState.Parked;
                Immobilized = ImmobilizedReason.None;
            }
            else
            {
                Immobilized = conditionOk ? ImmobilizedReason.OutOfFuel : ImmobilizedReason.Breakdown;
            }
        }

        internal void AddFuel(double liters)
        {
            FuelLiters = CareerMath.Clamp(FuelLiters + Math.Max(0, liters), 0, TankLiters);
            if (FuelLiters > 0)
            {
                LimpMeters = 0;
            }
            ReleaseIfDrivable();
        }

        internal void AddCondition(double points)
        {
            Condition = CareerMath.Clamp(Condition + Math.Max(0, points), 0, 100);
            ReleaseIfDrivable();
        }

        /// <summary>Condition, fuel and upgrade effects (SPEC-GARAGE 6.1, 6.2 and 7).</summary>
        public VehiclePerformance GetPerformance()
        {
            VehiclePerformance p = VehiclePerformance.Nominal;
            if (IsRemoved)
            {
                p.PerformanceFactor = 0;
                p.MaxSpeedMps = 0;
                p.Immobilized = true;
                return p;
            }
            double conditionFactor = 1;
            double brake = 1;
            if (Condition <= 0)
            {
                conditionFactor = 0;
            }
            else if (Condition < 20)
            {
                conditionFactor = 1.0 / 1.25;
                brake = 1.0 / 1.15;
                p.MaxSpeedMps = CareerBalance.WornSpeedCapMps;
                p.ConditionWarning = true;
            }
            else if (Condition < 50)
            {
                conditionFactor = 1.0 / 1.10;
                brake = 1.0 / 1.08;
                p.ConditionWarning = true;
            }
            double accel = conditionFactor;
            if (HasUpgrade(UpgradeKind.EngineTune))
            {
                accel /= 0.92;
            }
            if (HasUpgrade(UpgradeKind.BrakePads))
            {
                brake /= 0.90;
            }
            if (HasUpgrade(UpgradeKind.GripTires))
            {
                p.GripMultiplier = 1.08;
            }
            if (HasUpgrade(UpgradeKind.ComfortSuspension))
            {
                p.HarshPenaltyMultiplier = 0.8;
            }
            p.AccelerationMultiplier = accel;
            p.BrakingMultiplier = brake;
            p.PerformanceFactor = conditionFactor;

            if (!FuelIncluded)
            {
                double f = FuelFraction;
                if (FuelLiters <= 0)
                {
                    p.FuelWarning = LimpMeters >= CareerBalance.LimpReserveMeters ? FuelWarning.Empty : FuelWarning.LimpReserve;
                    p.MaxSpeedMps = Math.Min(p.MaxSpeedMps, CareerBalance.LimpSpeedCapMps);
                    p.PerformanceFactor = Math.Min(p.PerformanceFactor, 0.5);
                }
                else if (f <= 0.10)
                {
                    p.FuelWarning = FuelWarning.Critical;
                }
                else if (f <= 0.20)
                {
                    p.FuelWarning = FuelWarning.Low;
                }
            }
            if (IsImmobilized)
            {
                p.Immobilized = true;
                p.Reason = Immobilized;
                p.PerformanceFactor = 0;
                p.MaxSpeedMps = 0;
            }
            return p;
        }

        /// <summary>Sale value: 60% x price x (0.5 + 0.5 x condition) + 30% of upgrade prices (SPEC-ECON 8, SPEC-GARAGE 5).</summary>
        public long SaleValueCents()
        {
            double v = CareerBalance.SaleShare * PurchasePriceCents * (0.5 + 0.5 * CareerMath.Clamp(Condition, 0, 100) / 100.0);
            foreach (UpgradeKind u in Upgrades)
            {
                UpgradeInfo info = CareerCatalog.FindUpgrade(u);
                if (info != null)
                {
                    v += CareerBalance.SaleUpgradeShare * info.PriceCents;
                }
            }
            return CareerMath.RoundCents(v);
        }

        internal JsonValue ToJson()
        {
            JsonValue upgrades = JsonValue.Array();
            foreach (UpgradeKind u in Upgrades)
            {
                upgrades.Add(JsonValue.From(u.ToString()));
            }
            return JsonValue.Object()
                .Set("id", Id)
                .Set("model", ModelId)
                .Set("class", Class.ToString())
                .Set("kind", Kind.ToString())
                .Set("holder", Holder.ToString())
                .Set("state", State.ToString())
                .Set("immobilized", Immobilized.ToString())
                .Set("fuel", FuelLiters)
                .Set("condition", Condition)
                .Set("odometerKm", OdometerKm)
                .Set("limpMeters", LimpMeters)
                .Set("hasPosition", HasPosition)
                .Set("position", JsonRead.WritePoint(LastPosition))
                .Set("upgrades", upgrades)
                .Set("price", PurchasePriceCents)
                .Set("acquiredAt", AcquiredAt)
                .Set("storedAt", StoredAtPlaceId)
                .Set("rentalId", RentalId)
                .Set("rentalStartedAt", RentalStartedAt)
                .Set("rentalFee", RentalFeeCents)
                .Set("deposit", DepositCents)
                .Set("fuelAtStart", FuelAtStart)
                .Set("conditionAtStart", ConditionAtStart)
                .Set("assignment", Assignment.ToString())
                .Set("employee", AssignedEmployeeId);
        }

        internal static CareerVehicle FromJson(JsonValue o, string path, JsonRead r)
        {
            var v = new CareerVehicle
            {
                Id = r.Str(o, "id", path),
                ModelId = r.Str(o, "model", path),
                Class = r.Enum<VehicleClass>(o, "class", path),
                Kind = r.Enum<VehicleKind>(o, "kind", path),
                Holder = r.Enum<VehicleHolder>(o, "holder", path),
                State = r.Enum<VehicleLocationState>(o, "state", path),
                Immobilized = r.Enum<ImmobilizedReason>(o, "immobilized", path),
                FuelLiters = r.Num(o, "fuel", path, 0, 1000),
                Condition = r.Num(o, "condition", path, 0, 100),
                OdometerKm = r.Num(o, "odometerKm", path, 0),
                LimpMeters = r.Num(o, "limpMeters", path, 0, CareerBalance.LimpReserveMeters),
                HasPosition = r.Bool(o, "hasPosition", path),
                LastPosition = r.Point(o, "position", path),
                PurchasePriceCents = r.Long(o, "price", path, 0),
                AcquiredAt = r.Num(o, "acquiredAt", path, 0),
                StoredAtPlaceId = r.Str(o, "storedAt", path, false),
                RentalId = r.Str(o, "rentalId", path, false),
                RentalStartedAt = r.Num(o, "rentalStartedAt", path, 0),
                RentalFeeCents = r.Long(o, "rentalFee", path, 0),
                DepositCents = r.Long(o, "deposit", path, 0),
                FuelAtStart = r.Num(o, "fuelAtStart", path, 0, 1000),
                ConditionAtStart = r.Num(o, "conditionAtStart", path, 0, 100),
                Assignment = r.Enum<FleetAssignment>(o, "assignment", path),
                AssignedEmployeeId = r.Str(o, "employee", path, false),
            };
            foreach (string u in r.StrList(o, "upgrades", path))
            {
                if (Enum.TryParse(u, false, out UpgradeKind k) && Enum.IsDefined(typeof(UpgradeKind), k) && !v.Upgrades.Contains(k))
                {
                    v.Upgrades.Add(k);
                }
                else
                {
                    r.Error(path + ".upgrades", "invalid upgrade '" + u + "'");
                }
            }
            VehicleModel m = CareerCatalog.Find(v.ModelId);
            if (m == null)
            {
                r.Error(path + ".model", "unknown model '" + v.ModelId + "'");
            }
            else
            {
                if (m.Class != v.Class)
                {
                    r.Error(path + ".class", "class does not match model");
                }
                if (v.FuelLiters > v.TankLiters + 1e-6)
                {
                    r.Error(path + ".fuel", "fuel exceeds tank");
                }
            }
            if (v.IsRental && string.IsNullOrEmpty(v.RentalId) && v.State != VehicleLocationState.Removed)
            {
                r.Error(path + ".rentalId", "rental without rental id");
            }
            return v;
        }
    }
}
