using System.Collections.Generic;

namespace DriveTheCityII.Core.Career
{
    /// <summary>How a catalog model is obtained.</summary>
    public enum VehicleAcquisition
    {
        BasicRental = 0,
        PremiumRental = 1,
        Purchase = 2,
        Service = 3,
        CompanyPurchase = 4,
    }

    /// <summary>
    /// A career vehicle model. The mod maps <see cref="PrefabCategory"/> ("car", "taxi", "van", "truck", "bus") to game
    /// prefabs (OPEN-034). Fuel values are gameplay-scaled so a full tank matches SPEC-ECON section 8 ranges and costs.
    /// </summary>
    public sealed class VehicleModel
    {
        public string Id;
        public string Name;
        public VehicleClass Class;
        public VehicleAcquisition Acquisition;
        public string PrefabCategory;

        /// <summary>Purchase price in cents (0 when not purchasable).</summary>
        public long PriceCents;

        public long RentalShiftFeeCents;
        public long RentalDepositCents;
        public double TankLiters;
        public double ConsumptionLitersPerKm;
        public long FuelPricePerLiterCents;

        /// <summary>Condition points lost per km (SPEC-GARAGE section 6.2: car 1 point per 5 km; classes scale).</summary>
        public double WearPerKm;

        public long MaintenancePerPointCents;
        public Unlock RequiredUnlock;
        public int PassengerCapacity;
        public int ParcelSlots;
        public double PayloadTonnes;

        public double RangeKm => ConsumptionLitersPerKm > 0 ? TankLiters / ConsumptionLitersPerKm : 0;
    }

    /// <summary>Upgrade slots and kinds (SPEC-GARAGE section 7). At most one per slot; never required by any content.</summary>
    public enum UpgradeKind
    {
        BrakePads = 0,
        GripTires = 1,
        EngineTune = 2,
        ExtendedTank = 3,
        ComfortSuspension = 4,
        CargoRacks = 5,
        PayloadReinforcement = 6,
    }

    public enum UpgradeSlot
    {
        Brakes = 0,
        Tires = 1,
        Engine = 2,
        Fuel = 3,
        Suspension = 4,
        Cargo = 5,
    }

    public sealed class UpgradeInfo
    {
        public UpgradeKind Kind;
        public UpgradeSlot Slot;
        public string Name;
        public long PriceCents;
        public VehicleClass[] Classes;
    }

    /// <summary>Static catalog of career vehicle models and upgrades.</summary>
    public static class CareerCatalog
    {
        public const string BasicRentalModelId = "car.basic";
        public const string ServiceTruckModelId = "truck.service";
        public const string ServiceBusModelId = "bus.service";

        private static readonly VehicleModel[] s_Models =
        {
            Model("car.basic", "Basic rental compact", VehicleClass.Car, VehicleAcquisition.BasicRental, "car", 0, 0, 0, 40, 40.0 / 120.0, 150, 0.2, 120, Unlock.BasicRental, 3, CareerBalance.CarParcelSlots, 0.3),
            Model("car.used", "Used hatchback", VehicleClass.Car, VehicleAcquisition.Purchase, "car", 65000, 0, 0, 40, 40.0 / 120.0, 150, 0.2, 120, Unlock.CarPurchase, 3, CareerBalance.CarParcelSlots, 0.3),
            Model("car.new", "New sedan", VehicleClass.Car, VehicleAcquisition.Purchase, "car", 160000, 0, 0, 45, 45.0 / 140.0, 150, 0.18, 120, Unlock.CarPurchase, 3, CareerBalance.CarParcelSlots, 0.3),
            Model("taxi.sedan", "Taxi sedan", VehicleClass.Taxi, VehicleAcquisition.Purchase, "taxi", 190000, 0, 0, 45, 45.0 / 140.0, 150, 0.18, 120, Unlock.TaxiPurchase, 3, CareerBalance.CarParcelSlots, 0.3),
            Model("van.rental", "Rental delivery van", VehicleClass.Van, VehicleAcquisition.PremiumRental, "van", 0, 6000, 10000, 50, 0.5, 140, 0.25, 180, Unlock.VanRental, 2, CareerBalance.VanParcelSlots, 1.2),
            Model("van.light", "Light delivery van", VehicleClass.Van, VehicleAcquisition.Purchase, "van", 240000, 0, 0, 50, 0.5, 140, 0.25, 180, Unlock.VanPurchase, 2, CareerBalance.VanParcelSlots, 1.2),
            Model("truck.rental", "Rental rigid truck", VehicleClass.RigidTruck, VehicleAcquisition.PremiumRental, "truck", 0, 15000, 30000, 120, 0.8, 100, 0.3, 350, Unlock.TruckRentalPurchase, 1, 0, CareerBalance.TruckPayloadTonnes),
            Model("truck.rigid", "Rigid truck", VehicleClass.RigidTruck, VehicleAcquisition.Purchase, "truck", 650000, 0, 0, 120, 0.8, 100, 0.3, 350, Unlock.TruckRentalPurchase, 1, 0, CareerBalance.TruckPayloadTonnes),
            Model("truck.service", "Shipper service truck", VehicleClass.RigidTruck, VehicleAcquisition.Service, "truck", 0, 0, 0, 120, 0.8, 100, 0.3, 350, Unlock.Freight, 1, 0, CareerBalance.TruckPayloadTonnes),
            Model("bus.service", "Operator service bus", VehicleClass.Bus, VehicleAcquisition.Service, "bus", 0, 0, 0, 150, 1.0, 100, 0.3, 350, Unlock.BusShifts, CareerBalance.BusCapacity, 0, 4.5),
            Model("bus.city", "City bus", VehicleClass.Bus, VehicleAcquisition.CompanyPurchase, "bus", 900000, 0, 0, 150, 1.0, 100, 0.3, 350, Unlock.CompanyOnly, CareerBalance.BusCapacity, 0, 4.5),
        };

        private static readonly UpgradeInfo[] s_Upgrades =
        {
            Upgrade(UpgradeKind.BrakePads, UpgradeSlot.Brakes, "Performance brake pads", 25000, VehicleClass.Car, VehicleClass.Taxi, VehicleClass.Van, VehicleClass.RigidTruck, VehicleClass.Bus),
            Upgrade(UpgradeKind.GripTires, UpgradeSlot.Tires, "Grip tires", 20000, VehicleClass.Car, VehicleClass.Taxi, VehicleClass.Van),
            Upgrade(UpgradeKind.EngineTune, UpgradeSlot.Engine, "Engine tune", 60000, VehicleClass.Car, VehicleClass.Taxi, VehicleClass.Van, VehicleClass.RigidTruck),
            Upgrade(UpgradeKind.ExtendedTank, UpgradeSlot.Fuel, "Extended tank", 15000, VehicleClass.Car, VehicleClass.Taxi, VehicleClass.Van, VehicleClass.RigidTruck, VehicleClass.Bus),
            Upgrade(UpgradeKind.ComfortSuspension, UpgradeSlot.Suspension, "Comfort suspension", 30000, VehicleClass.Car, VehicleClass.Taxi, VehicleClass.Van),
            Upgrade(UpgradeKind.CargoRacks, UpgradeSlot.Cargo, "Cargo racks", 20000, VehicleClass.Van),
            Upgrade(UpgradeKind.PayloadReinforcement, UpgradeSlot.Cargo, "Payload reinforcement", 45000, VehicleClass.RigidTruck),
        };

        public static IReadOnlyList<VehicleModel> Models => s_Models;

        public static IReadOnlyList<UpgradeInfo> Upgrades => s_Upgrades;

        public static VehicleModel Find(string modelId)
        {
            if (modelId == null)
            {
                return null;
            }
            for (int i = 0; i < s_Models.Length; i++)
            {
                if (s_Models[i].Id == modelId)
                {
                    return s_Models[i];
                }
            }
            return null;
        }

        public static UpgradeInfo FindUpgrade(UpgradeKind kind)
        {
            for (int i = 0; i < s_Upgrades.Length; i++)
            {
                if (s_Upgrades[i].Kind == kind)
                {
                    return s_Upgrades[i];
                }
            }
            return null;
        }

        /// <summary>The premium rental model for a class, or null.</summary>
        public static VehicleModel PremiumRentalFor(VehicleClass cls)
        {
            for (int i = 0; i < s_Models.Length; i++)
            {
                if (s_Models[i].Class == cls && s_Models[i].Acquisition == VehicleAcquisition.PremiumRental)
                {
                    return s_Models[i];
                }
            }
            return null;
        }

        private static VehicleModel Model(string id, string name, VehicleClass cls, VehicleAcquisition acq, string prefab, long price, long shiftFee, long deposit, double tank, double perKm, long fuelPrice, double wearPerKm, long maintenance, Unlock unlock, int passengers, int slots, double payload)
        {
            return new VehicleModel
            {
                Id = id,
                Name = name,
                Class = cls,
                Acquisition = acq,
                PrefabCategory = prefab,
                PriceCents = price,
                RentalShiftFeeCents = shiftFee,
                RentalDepositCents = deposit,
                TankLiters = tank,
                ConsumptionLitersPerKm = perKm,
                FuelPricePerLiterCents = fuelPrice,
                WearPerKm = wearPerKm,
                MaintenancePerPointCents = maintenance,
                RequiredUnlock = unlock,
                PassengerCapacity = passengers,
                ParcelSlots = slots,
                PayloadTonnes = payload,
            };
        }

        private static UpgradeInfo Upgrade(UpgradeKind kind, UpgradeSlot slot, string name, long price, params VehicleClass[] classes)
        {
            return new UpgradeInfo { Kind = kind, Slot = slot, Name = name, PriceCents = price, Classes = classes };
        }
    }
}
