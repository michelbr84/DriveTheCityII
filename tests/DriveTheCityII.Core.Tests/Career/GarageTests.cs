using System.Linq;
using DriveTheCityII.Core.Career;
using Xunit;

namespace DriveTheCityII.Core.Tests.Career
{
    public class GarageTests
    {
        private const long UsedCar = 65000;

        private static CareerSim RichSim()
        {
            var sim = new CareerSim(TestCities.Taxi());
            sim.Svc.DebugGrant(200000, sim.Now);
            return sim;
        }

        [Fact]
        public void PurchaseAndSaleChargeExactlyOnceAndSaleIsBelowPrice()
        {
            CareerSim sim = RichSim();
            long start = sim.Svc.WalletBalanceCents;
            CareerResult buy = sim.Svc.BuyVehicle("car.used", "op-buy", sim.Now);
            Assert.True(buy.IsOk, buy.ToString());
            Assert.Equal("VP-" + buy.Id, buy.TransactionId);
            Assert.Equal(start - UsedCar, sim.Svc.WalletBalanceCents);

            CareerResult again = sim.Svc.BuyVehicle("car.used", "op-buy", sim.Now);
            Assert.Equal(CareerResultCode.AlreadyApplied, again.Code);
            Assert.Equal(start - UsedCar, sim.Svc.WalletBalanceCents);
            Assert.Single(sim.Svc.GetVehicles(), v => v.Kind == VehicleKind.Owned);

            VehicleView view = sim.Svc.GetVehicle(buy.Id);
            Assert.Equal(VehicleLocationState.Stored, view.State);
            Assert.Equal(80, view.ConditionPercent, 6);
            long expectedSale = CareerMath.RoundCents(0.6 * UsedCar * (0.5 + 0.5 * 0.8));
            Assert.Equal(expectedSale, view.SaleValueCents);

            CareerResult sale = sim.Svc.SellVehicle(buy.Id, sim.Now);
            Assert.True(sale.IsOk);
            Assert.Equal(expectedSale, sale.AmountCents);
            Assert.True(expectedSale < UsedCar);
            Assert.Equal(CareerResultCode.AlreadyApplied, sim.Svc.SellVehicle(buy.Id, sim.Now).Code);
            Assert.Equal(start - UsedCar + expectedSale, sim.Svc.WalletBalanceCents);
            Assert.Equal(sim.LedgerSum(), sim.Svc.WalletBalanceCents);
        }

        [Fact]
        public void PurchasesAreRefusedWithoutFundsOrUnlocksAndCompanyModelsArePrivate()
        {
            var sim = new CareerSim(TestCities.Taxi());
            Assert.Equal(CareerResultCode.InsufficientFunds, sim.Svc.BuyVehicle("car.used", null, sim.Now).Code);
            Assert.Equal(CareerResultCode.Locked, sim.Svc.BuyVehicle("van.light", null, sim.Now).Code);
            Assert.Equal(CareerResultCode.Locked, sim.Svc.BuyVehicle("bus.city", null, sim.Now).Code);
            Assert.Equal(CareerResultCode.InvalidArgument, sim.Svc.BuyVehicle("car.basic", null, sim.Now).Code);
            Assert.Equal(CareerBalance.InitialBalanceCents, sim.Svc.WalletBalanceCents);
            Assert.Single(sim.Svc.GetLedger());
        }

        [Fact]
        public void PremiumRentalChargesFeeAndDepositAndRefundsTheRestOnReturn()
        {
            var sim = new CareerSim(TestCities.Delivery());
            sim.Svc.DebugSeedXp(CareerKind.Delivery, 100);
            long start = sim.Svc.WalletBalanceCents;
            CareerResult rent = sim.Svc.RentVehicle("van.rental", "op-r", sim.Now);
            Assert.True(rent.IsOk);
            Assert.Equal(start - 6000 - 10000, sim.Svc.WalletBalanceCents);
            Assert.Equal(CareerResultCode.AlreadyApplied, sim.Svc.RentVehicle("van.rental", "op-r", sim.Now).Code);

            sim.VehicleId = rent.Id;
            sim.Drive(1, 10, 1000); // 0.5 L of fuel, 0.25 condition points
            CareerResult ret = sim.Svc.ReturnRental(rent.Id, true, null, sim.Now);
            Assert.True(ret.IsOk);
            long expectedRefund = 10000 - CareerMath.RoundCents(0.5 * 140) - CareerMath.RoundCents(0.25 * 180);
            Assert.Equal(expectedRefund, ret.AmountCents);
            Assert.Equal(start - 6000 - 10000 + expectedRefund, sim.Svc.WalletBalanceCents);
            Assert.Equal(CareerResultCode.AlreadyApplied, sim.Svc.ReturnRental(rent.Id, true, null, sim.Now).Code);
        }

        [Fact]
        public void PremiumRentalRefusedWhenItCannotBePaid()
        {
            var sim = new CareerSim(TestCities.Freight());
            sim.Svc.DebugSeedXp(CareerKind.Freight, 100);
            Assert.Equal(CareerResultCode.InsufficientFunds, sim.Svc.RentVehicle("truck.rental", null, sim.Now).Code);
            Assert.Equal(CareerBalance.InitialBalanceCents, sim.Svc.WalletBalanceCents);
        }

        [Fact]
        public void RefuelAndRepairAtServicePointsAreChargedOncePerOperation()
        {
            CareerSim sim = RichSim();
            string car = sim.Svc.BuyVehicle("car.new", "op-car", sim.Now).Id;
            sim.VehicleId = car;
            for (int i = 0; i < 30; i++)
            {
                sim.Drive(60, 16, 1000);
            }
            sim.Drive(1, 5, 0, impact: 12); // severe contact: -10 condition
            VehicleView v = sim.Svc.GetVehicle(car);
            double litersUsed = 30 * 45.0 / 140.0;
            Assert.Equal(100 - 100 * litersUsed / 45.0, v.FuelPercent, 6);
            Assert.Equal(100 - 30 * 0.18 - 10, v.ConditionPercent, 6);

            Assert.Equal(CareerResultCode.NotAtServicePoint, sim.Svc.Refuel(car, null, 1, null, sim.Now).Code);
            long before = sim.Svc.WalletBalanceCents;
            CareerResult fuel = sim.Svc.Refuel(car, "svc-fuel", 1, "op-fuel", sim.Now);
            Assert.True(fuel.IsOk, fuel.ToString());
            long fuelCost = (long)System.Math.Ceiling(litersUsed * 150 - 1e-9);
            Assert.Equal(-fuelCost, fuel.AmountCents);
            Assert.Equal(100, sim.Svc.GetVehicle(car).FuelPercent, 6);
            Assert.Equal(CareerResultCode.AlreadyApplied, sim.Svc.Refuel(car, "svc-fuel", 1, "op-fuel", sim.Now).Code);

            CareerResult repair = sim.Svc.Repair(car, "svc-fuel", 100, "op-fix", sim.Now);
            Assert.True(repair.IsOk);
            long repairCost = (long)System.Math.Ceiling((30 * 0.18 + 10) * 120 - 1e-9);
            Assert.Equal(-repairCost, repair.AmountCents);
            Assert.Equal(100, sim.Svc.GetVehicle(car).ConditionPercent, 6);
            Assert.Equal(CareerResultCode.AlreadyApplied, sim.Svc.Repair(car, "svc-fuel", 100, "op-fix", sim.Now).Code);
            Assert.Equal(before - fuelCost - repairCost, sim.Svc.WalletBalanceCents);

            sim.World.Remove("svc-fuel");
            Assert.Equal(CareerResultCode.NotAtServicePoint, sim.Svc.Refuel(car, "svc-fuel", 1, null, sim.Now).Code);
        }

        [Fact]
        public void RefuelBuysWhatTheWalletCanAfford()
        {
            var sim = new CareerSim(TestCities.Taxi(), 42, new CareerOptions { InitialBalanceCents = UsedCar + 900 });
            string car = sim.Svc.BuyVehicle("car.used", null, sim.Now).Id;
            sim.VehicleId = car;
            for (int i = 0; i < 60; i++)
            {
                sim.Drive(60, 16, 1000); // 20 L used
            }
            CareerResult fuel = sim.Svc.Refuel(car, "svc-fuel", 1, null, sim.Now);
            Assert.True(fuel.IsOk);
            Assert.Equal(-900, fuel.AmountCents);
            Assert.Equal(0, sim.Svc.WalletBalanceCents);
            Assert.Equal(CareerResultCode.InsufficientFunds, sim.Svc.Refuel(car, "svc-fuel", 1, null, sim.Now).Code);
        }

        [Fact]
        public void FuelExhaustionUsesTheLimpReserveThenImmobilizesAndRoadsideFuelRecoversWithDeferral()
        {
            var sim = new CareerSim(TestCities.Taxi(), 42, new CareerOptions { InitialBalanceCents = UsedCar });
            string car = sim.Svc.BuyVehicle("car.used", null, sim.Now).Id;
            Assert.Equal(0, sim.Svc.WalletBalanceCents);
            sim.VehicleId = car;
            for (int i = 0; i < 121; i++)
            {
                sim.Drive(60, 16, 1000); // 120 km empties the tank; the last km uses the limp reserve
            }
            VehiclePerformance p = sim.Svc.GetPerformance(car);
            Assert.Equal(FuelWarning.LimpReserve, p.FuelWarning);
            Assert.Equal(CareerBalance.LimpSpeedCapMps, p.MaxSpeedMps, 6);
            Assert.False(p.Immobilized);

            sim.Drive(60, 8, 1000);
            sim.Drive(60, 8, 1000);
            p = sim.Svc.GetPerformance(car);
            Assert.True(p.Immobilized);
            Assert.Equal(ImmobilizedReason.OutOfFuel, p.Reason);
            Assert.Equal(0, p.MaxSpeedMps);
            Assert.Contains(sim.Events, e => e.Kind == CareerEventKind.VehicleImmobilized && e.VehicleId == car);

            RecoveryView rec = sim.Svc.GetRecoveryOptions(sim.Now);
            Assert.True(rec.Insolvent);
            Assert.Equal(RecoveryOptionKind.TakeBasicRental, rec.Options[0].Kind);
            Assert.Equal(0, rec.Options[0].CostCents);
            Assert.Contains(rec.Options, o => o.Kind == RecoveryOptionKind.RoadsideFuel && o.CanDefer);

            CareerResult rs = sim.Svc.RequestRoadsideService(car, RoadsideService.Fuel, "op-rs", sim.Now);
            Assert.True(rs.IsOk);
            long expectedCost = CareerBalance.RoadsideFuelFeeCents + CareerMath.RoundCents(0.25 * 40 * 150);
            Assert.Equal(expectedCost, rs.DeferredCents);
            Assert.Equal(0, sim.Svc.WalletBalanceCents);
            Assert.Equal(expectedCost, sim.Svc.DeferredChargesCents);
            Assert.False(sim.Svc.GetPerformance(car).Immobilized);
            Assert.Equal(25, sim.Svc.GetVehicle(car).FuelPercent, 6);
            Assert.Equal(CareerResultCode.AlreadyApplied, sim.Svc.RequestRoadsideService(car, RoadsideService.Fuel, "op-rs", sim.Now).Code);
            Assert.Equal(expectedCost, sim.Svc.DeferredChargesCents);

            // While a deferred charge is open: purchases and upgrades blocked, rentals allowed.
            Assert.Equal(CareerResultCode.DeferredChargesOpen, sim.Svc.BuyVehicle("car.used", null, sim.Now).Code);
            Assert.True(sim.Svc.RentBasic(sim.Now).IsOk);
        }

        [Fact]
        public void BankruptcyAlwaysLeavesAZeroCostPathToPaidWork()
        {
            var sim = new CareerSim(TestCities.Taxi(), 42, new CareerOptions { InitialBalanceCents = UsedCar });
            string car = sim.Svc.BuyVehicle("car.used", null, sim.Now).Id;
            sim.VehicleId = car;
            for (int i = 0; i < 125; i++)
            {
                sim.Drive(60, 16, 1000);
            }
            Assert.True(sim.Svc.GetPerformance(car).Immobilized);
            RecoveryView rec = sim.Svc.GetRecoveryOptions(sim.Now);
            Assert.True(rec.Insolvent);
            Assert.False(rec.HasUsableVehicle);
            Assert.Contains(rec.Options, o => o.Kind == RecoveryOptionKind.StarterJob && o.CostCents == 0);

            // Zero-credit path: basic rental -> starter taxi job -> paid.
            CareerResult basic = sim.Svc.RentBasic(sim.Now);
            Assert.True(basic.IsOk);
            Assert.Equal(0, sim.Svc.WalletBalanceCents);
            sim.VehicleId = basic.Id;
            sim.Pos = default;
            sim.Drive(1, 0, 0);
            Assert.True(sim.Svc.GetRecoveryOptions(sim.Now).HasUsableVehicle);
            var offers = sim.Svc.GetOffers(CareerKind.Taxi, sim.Pos, sim.Now);
            Assert.NotEmpty(offers);
            Assert.True(sim.Svc.Accept(offers[0].Id, basic.Id, sim.Now).IsOk);
            sim.DriveTo(offers[0].Legs[0].Place.Access);
            sim.Stop(5);
            sim.DriveTo(offers[0].Legs[1].Place.Access);
            sim.Stop(4);
            Assert.True(sim.Svc.WalletBalanceCents > 0);
            Assert.Equal(sim.LedgerSum(), sim.Svc.WalletBalanceCents);
        }

        [Fact]
        public void ImmobilizedBasicRentalIsReplacedForFree()
        {
            var sim = new CareerSim(TestCities.Taxi());
            string first = sim.RentBasicAndBoard();
            for (int i = 0; i < 10; i++)
            {
                sim.Drive(1, 10, 10, impact: 12);
            }
            Assert.True(sim.Svc.GetPerformance(first).Immobilized);
            Assert.Equal(ImmobilizedReason.Breakdown, sim.Svc.GetPerformance(first).Reason);
            CareerResult second = sim.Svc.RentBasic(sim.Now);
            Assert.True(second.IsOk);
            Assert.NotEqual(first, second.Id);
            Assert.Equal(VehicleLocationState.Removed, sim.Svc.GetVehicle(first).State);
            Assert.Equal(CareerBalance.InitialBalanceCents, sim.Svc.WalletBalanceCents);
            Assert.Equal(second.Id, sim.Svc.RentBasic(sim.Now).Id); // one active basic rental at a time
        }

        [Fact]
        public void WearThresholdsReducePerformanceAndRepairRestoresIt()
        {
            CareerSim sim = RichSim();
            string car = sim.Svc.BuyVehicle("car.new", null, sim.Now).Id;
            sim.VehicleId = car;
            Assert.Equal(1.0, sim.Svc.GetPerformance(car).PerformanceFactor, 6);
            for (int i = 0; i < 6; i++)
            {
                sim.Drive(1, 10, 0, impact: 12);
            }
            VehiclePerformance p = sim.Svc.GetPerformance(car);
            Assert.Equal(40, sim.Svc.GetVehicle(car).ConditionPercent, 6);
            Assert.Equal(1 / 1.10, p.PerformanceFactor, 6);
            Assert.Equal(1 / 1.08, p.BrakingMultiplier, 6);
            Assert.True(p.ConditionWarning);
            Assert.True(double.IsPositiveInfinity(p.MaxSpeedMps));

            for (int i = 0; i < 3; i++)
            {
                sim.Drive(1, 10, 0, impact: 12);
            }
            p = sim.Svc.GetPerformance(car);
            Assert.Equal(1 / 1.25, p.PerformanceFactor, 6);
            Assert.Equal(CareerBalance.WornSpeedCapMps, p.MaxSpeedMps, 6);

            sim.Drive(1, 10, 0, impact: 12);
            p = sim.Svc.GetPerformance(car);
            Assert.True(p.Immobilized);
            Assert.Equal(0, p.PerformanceFactor);

            Assert.True(sim.Svc.Repair(car, "svc-fuel", 100, null, sim.Now).IsOk);
            p = sim.Svc.GetPerformance(car);
            Assert.False(p.Immobilized);
            Assert.Equal(1.0, p.PerformanceFactor, 6);
        }

        [Fact]
        public void ContactsInsideTheDebounceWindowCountOnceUnlessMoreSevere()
        {
            CareerSim sim = RichSim();
            string car = sim.Svc.BuyVehicle("car.new", null, sim.Now).Id;
            sim.VehicleId = car;
            sim.Drive(0.1, 5, 0, impact: 2); // light -0.5
            sim.Drive(0.1, 5, 0, impact: 2); // merged
            sim.Drive(0.1, 5, 0, impact: 12); // more severe: -10
            Assert.Equal(89.5, sim.Svc.GetVehicle(car).ConditionPercent, 6);
        }

        [Fact]
        public void UpgradesHaveMeasurableEffectsOneSlotEachAndOwnedOnly()
        {
            CareerSim sim = RichSim();
            sim.Svc.DebugGrant(200000, sim.Now);
            string car = sim.Svc.BuyVehicle("car.new", null, sim.Now).Id;
            string basic = sim.Svc.RentBasic(sim.Now).Id;
            Assert.Equal(CareerResultCode.NotOwned, sim.Svc.Upgrade(basic, UpgradeKind.EngineTune, "svc-fuel", null, sim.Now).Code);
            Assert.Equal(CareerResultCode.WrongVehicleClass, sim.Svc.Upgrade(car, UpgradeKind.CargoRacks, "svc-fuel", null, sim.Now).Code);

            CareerResult up = sim.Svc.Upgrade(car, UpgradeKind.EngineTune, "svc-fuel", "op-up", sim.Now);
            Assert.True(up.IsOk);
            Assert.Equal(-60000, up.AmountCents);
            Assert.Equal(1 / 0.92, sim.Svc.GetPerformance(car).AccelerationMultiplier, 6);
            Assert.Equal(CareerResultCode.AlreadyApplied, sim.Svc.Upgrade(car, UpgradeKind.EngineTune, "svc-fuel", "op-up", sim.Now).Code);

            Assert.True(sim.Svc.Upgrade(car, UpgradeKind.ExtendedTank, "svc-fuel", null, sim.Now).IsOk);
            Assert.Equal(80, sim.Svc.GetVehicle(car).FuelPercent, 6); // same liters, tank +25%
            Assert.True(sim.Svc.Upgrade(car, UpgradeKind.BrakePads, "svc-fuel", null, sim.Now).IsOk);
            Assert.Equal(1 / 0.9, sim.Svc.GetPerformance(car).BrakingMultiplier, 6);
            Assert.True(sim.Svc.Upgrade(car, UpgradeKind.ComfortSuspension, "svc-fuel", null, sim.Now).IsOk);
            Assert.Equal(0.8, sim.Svc.GetPerformance(car).HarshPenaltyMultiplier, 6);
            Assert.True(sim.Svc.Upgrade(car, UpgradeKind.GripTires, "svc-fuel", null, sim.Now).IsOk);
            Assert.Equal(1.08, sim.Svc.GetPerformance(car).GripMultiplier, 6);
        }

        [Fact]
        public void CatalogIsConsistent()
        {
            Assert.All(CareerCatalog.Models, m =>
            {
                Assert.False(string.IsNullOrEmpty(m.Id));
                Assert.True(m.TankLiters > 0 && m.ConsumptionLitersPerKm > 0);
                Assert.Contains(m.PrefabCategory, new[] { "car", "taxi", "van", "truck", "bus" });
            });
            Assert.Equal(120, CareerCatalog.Find("car.used").RangeKm, 6);
            Assert.Equal(100, CareerCatalog.Find("van.light").RangeKm, 6);
            Assert.Equal(150, CareerCatalog.Find("truck.rigid").RangeKm, 6);
            Assert.Equal(6000, CareerCatalog.Find("car.used").TankLiters * CareerCatalog.Find("car.used").FuelPricePerLiterCents, 6);
            Assert.Equal(CareerCatalog.Models.Count, CareerCatalog.Models.Select(m => m.Id).Distinct().Count());
        }
    }
}
