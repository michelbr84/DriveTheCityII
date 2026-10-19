using System;
using System.Linq;
using DriveTheCityII.Core.Career;
using Xunit;

namespace DriveTheCityII.Core.Tests.Career
{
    public class DeliveryFreightTests
    {
        /// <summary>Refreshes the board until an offer matches (bounded; deterministic for a seed).</summary>
        private static MissionOffer FindOffer(CareerSim sim, CareerKind kind, Func<MissionOffer, bool> match)
        {
            for (int i = 0; i < 40; i++)
            {
                MissionOffer o = sim.Svc.RefreshOffers(kind, sim.Pos, sim.Now).FirstOrDefault(match);
                if (o != null)
                {
                    return o;
                }
            }
            return null;
        }

        private static CareerSim VanSim()
        {
            var sim = new CareerSim(TestCities.Delivery());
            sim.Svc.DebugSeedXp(CareerKind.Delivery, 100); // level 2: van rental unlocked
            CareerResult van = sim.Svc.RentVehicle("van.rental", "op-van", sim.Now);
            Assert.True(van.IsOk, van.ToString());
            sim.VehicleId = van.Id;
            sim.Drive(0.5, 0, 0);
            return sim;
        }

        [Fact]
        public void MultiStopDeliveryLoadsThenDeliversInAnyOrderAndPaysPerParcel()
        {
            CareerSim sim = VanSim();
            MissionOffer o = FindOffer(sim, CareerKind.Delivery, x => x.Legs.Count >= 4 && x.Eligible);
            Assert.NotNull(o);
            Assert.Equal(LegKind.Load, o.Legs[0].Kind);
            Assert.All(o.Legs.Skip(1), l => Assert.Equal(1, l.Group));
            Assert.True(o.DeadlineSeconds > 0);
            long balanceBefore = sim.Svc.WalletBalanceCents;

            CareerResult r = sim.Svc.Accept(o.Id, sim.VehicleId, sim.Now);
            Assert.True(r.IsOk, r.ToString());
            sim.Drive(0.5, 0, 0);
            sim.DriveTo(o.Legs[0].Place.Access);
            Assert.Equal(0, sim.Svc.GetActiveMission().ParcelsLoaded);
            sim.Stop(2 * o.ParcelCount + 1);
            ActiveMissionView v = sim.Svc.GetActiveMission();
            Assert.Equal(o.ParcelCount, v.ParcelsLoaded);

            // Deliver the drops in reverse order: the group is unordered.
            foreach (MissionLeg drop in o.Legs.Skip(1).Reverse())
            {
                sim.DriveTo(drop.Place.Access);
                sim.Stop(3 * drop.Count + 1);
            }
            Assert.Null(sim.Svc.GetActiveMission());
            LedgerEntryView settle = sim.Svc.GetLedger().Single(e => e.Type == LedgerEntryType.MissionSettlement);
            Assert.EndsWith("-COMPLETED", settle.TransactionId);
            // Perfect evaluation (integrity 100, on time, clean): x1.15 of the contract value.
            Assert.InRange(settle.AmountCents, CareerMath.RoundCents(o.ContractValueCents * 1.15) - 2, CareerMath.RoundCents(o.ContractValueCents * 1.15) + 2);
            Assert.Equal(balanceBefore + settle.AmountCents, sim.Svc.WalletBalanceCents);
            Assert.True(sim.Svc.GetProgress().Careers.Single(c => c.Career == CareerKind.Delivery).Xp > 100);
        }

        [Fact]
        public void MovingDuringLoadingKeepsLoadedParcels()
        {
            CareerSim sim = VanSim();
            MissionOffer o = FindOffer(sim, CareerKind.Delivery, x => x.ParcelCount >= 3 && x.Eligible);
            Assert.NotNull(o);
            sim.Svc.Accept(o.Id, sim.VehicleId, sim.Now);
            sim.DriveTo(o.Legs[0].Place.Access);
            sim.Stop(4.5, 0.5); // 2 parcels loaded (2 s each)
            sim.Drive(1, 3, 3);
            Assert.Equal(2, sim.Svc.GetActiveMission().ParcelsLoaded);
            sim.Stop(2 * (o.ParcelCount - 2) + 1);
            Assert.Equal(o.ParcelCount, sim.Svc.GetActiveMission().ParcelsLoaded);
        }

        [Fact]
        public void DemolishedDropIsSkippedWithNoFaultAndTheRestIsPaid()
        {
            CareerSim sim = VanSim();
            MissionOffer o = FindOffer(sim, CareerKind.Delivery, x => x.Legs.Count >= 3 && x.Eligible);
            Assert.NotNull(o);
            sim.Svc.Accept(o.Id, sim.VehicleId, sim.Now);
            sim.DriveTo(o.Legs[0].Place.Access);
            sim.Stop(2 * o.ParcelCount + 1);
            MissionLeg removed = o.Legs[1];
            sim.World.Remove(removed.Place.Id);
            sim.Drive(0.5, 0, 0);
            Assert.Contains(sim.Events, e => e.Key == "DTC.Delivery.Skipped");
            foreach (MissionLeg drop in o.Legs.Skip(2))
            {
                sim.DriveTo(drop.Place.Access);
                sim.Stop(3 * drop.Count + 1);
            }
            Assert.Null(sim.Svc.GetActiveMission());
            LedgerEntryView settle = sim.Svc.GetLedger().Single(e => e.Type == LedgerEntryType.MissionSettlement);
            Assert.EndsWith("-COMPLETED", settle.TransactionId);
            Assert.True(settle.AmountCents > 0 && settle.AmountCents < CareerMath.RoundCents(o.ContractValueCents * 1.15));
        }

        [Fact]
        public void RoadCutToAnyDropOfTheUnorderedGroupIsSkippedAfterTwoChecks()
        {
            CareerSim sim = VanSim();
            MissionOffer o = FindOffer(sim, CareerKind.Delivery, x => x.Legs.Count >= 3 && x.Eligible);
            Assert.NotNull(o);
            sim.Svc.Accept(o.Id, sim.VehicleId, sim.Now);
            sim.DriveTo(o.Legs[0].Place.Access);
            sim.Stop(2 * o.ParcelCount + 1);
            MissionLeg cut = o.Legs[o.Legs.Count - 1];
            sim.World.SetUnreachable(cut.Place.Id, true);
            sim.Drive(1, 0, 0); // revalidation: first failed check
            Assert.Equal(LegOutcome.Pending, sim.Svc.GetActiveMission().Legs.Single(l => l.PlaceId == cut.Place.Id).Outcome);
            sim.Stop(CareerBalance.WatchdogSeconds + 1, 5); // second check
            Assert.Equal(LegOutcome.SkippedNoFault, sim.Svc.GetActiveMission().Legs.Single(l => l.PlaceId == cut.Place.Id).Outcome);
            foreach (MissionLeg drop in o.Legs.Skip(1).Where(l => l.Place.Id != cut.Place.Id))
            {
                sim.DriveTo(drop.Place.Access);
                sim.Stop(3 * drop.Count + 1);
            }
            Assert.Null(sim.Svc.GetActiveMission());
            Assert.EndsWith("-COMPLETED", sim.Svc.GetLedger().Single(e => e.Type == LedgerEntryType.MissionSettlement).TransactionId);
        }

        [Fact]
        public void CancellationAfterLoadingChargesTheCappedReturnFeeAndNeverGoesNegative()
        {
            CareerSim sim = VanSim();
            MissionOffer o = FindOffer(sim, CareerKind.Delivery, x => x.Eligible);
            sim.Svc.Accept(o.Id, sim.VehicleId, sim.Now);
            sim.DriveTo(o.Legs[0].Place.Access);
            sim.Stop(2 * o.ParcelCount + 1);
            long before = sim.Svc.WalletBalanceCents;
            sim.Svc.Cancel("test", sim.Now);
            long fee = Math.Min(CareerBalance.CancellationFeeCapCents, CareerMath.RoundCents(o.ContractValueCents * 0.1));
            Assert.Equal(before - fee, sim.Svc.WalletBalanceCents);
            Assert.Equal(47, sim.Svc.GetProgress().Reputation);
            Assert.True(sim.Svc.WalletBalanceCents >= 0);
        }

        [Fact]
        public void StartWindowLapseCancelsAnAcceptedJobWithTheAcceptedStagePenaltyOnly()
        {
            var sim = new CareerSim(TestCities.Delivery());
            MissionOffer o = FindOffer(sim, CareerKind.Delivery, x => x.Eligible && x.DeadlineSeconds * 1.5 > CareerBalance.StartWindowSeconds + 10);
            Assert.NotNull(o);
            Assert.True(sim.Svc.Accept(o.Id, null, sim.Now).IsOk);
            sim.Walk(CareerBalance.StartWindowSeconds - 10, 10);
            Assert.Equal(MissionState.Accepted, sim.Svc.GetActiveMission().State);
            sim.Walk(20, 10);
            Assert.Null(sim.Svc.GetActiveMission());
            Assert.Contains(sim.Events, e => e.ToState == MissionState.Cancelled && e.Key == "DTC.Mission.Cancelled.StartWindow");
            Assert.Equal(49, sim.Svc.GetProgress().Reputation);
            Assert.Equal(CareerBalance.InitialBalanceCents, sim.Svc.WalletBalanceCents); // no return fee before loading
        }

        [Fact]
        public void ContactsDamageCargoIntegrityAndLowerPay()
        {
            CareerSim sim = VanSim();
            MissionOffer o = FindOffer(sim, CareerKind.Delivery, x => x.Legs.Count == 2 && x.Eligible);
            Assert.NotNull(o);
            sim.Svc.Accept(o.Id, sim.VehicleId, sim.Now);
            sim.DriveTo(o.Legs[0].Place.Access);
            sim.Stop(2 * o.ParcelCount + 1);
            sim.Drive(1, 5, 5, impact: 12); // severe contact: -50 integrity on standard parcels
            Assert.Equal(50, sim.Svc.GetActiveMission().WorstParcelIntegrity, 6);
            sim.DriveTo(o.Legs[1].Place.Access);
            sim.Stop(3 * o.Legs[1].Count + 1);
            LedgerEntryView settle = sim.Svc.GetLedger().Single(e => e.Type == LedgerEntryType.MissionSettlement);
            Assert.True(settle.AmountCents < CareerMath.RoundCents(o.ContractValueCents * 1.15) - 2);
        }

        // ------------------------------------------------------------------ freight

        private static CareerSim TruckSim(out string carId)
        {
            var sim = new CareerSim(TestCities.Freight());
            sim.Svc.DebugSeedXp(CareerKind.Delivery, 650); // Freight unlocked
            sim.Svc.DebugSeedXp(CareerKind.Freight, 100); // truck rental unlocked
            sim.Svc.DebugGrant(100000, sim.Now);
            carId = sim.Svc.RentBasic(sim.Now).Id;
            CareerResult truck = sim.Svc.RentVehicle("truck.rental", "op-truck", sim.Now);
            Assert.True(truck.IsOk, truck.ToString());
            sim.VehicleId = truck.Id;
            sim.Drive(0.5, 0, 0);
            return sim;
        }

        [Fact]
        public void FreightRequiresARigidTruck()
        {
            CareerSim sim = TruckSim(out string carId);
            MissionOffer o = FindOffer(sim, CareerKind.Freight, x => true);
            Assert.NotNull(o);
            Assert.Equal(new[] { VehicleClass.RigidTruck }, o.AllowedClasses.ToArray());
            Assert.False(o.ProvidesServiceVehicle);
            Assert.True(o.MassTonnes > 0 && o.MassTonnes <= CareerBalance.TruckPayloadTonnes);

            CareerResult wrong = sim.Svc.Accept(o.Id, carId, sim.Now);
            Assert.Equal(CareerResultCode.WrongVehicleClass, wrong.Code);
            Assert.Null(sim.Svc.GetActiveMission());

            // Accepted without a vehicle: driving the car never activates it.
            Assert.True(sim.Svc.Accept(o.Id, null, sim.Now).IsOk);
            string truck = sim.VehicleId;
            sim.VehicleId = carId;
            sim.Drive(1, 0, 0);
            Assert.Equal(MissionState.Accepted, sim.Svc.GetActiveMission().State);
            sim.VehicleId = truck;
            sim.Drive(1, 0, 0);
            Assert.Equal(MissionState.Active, sim.Svc.GetActiveMission().State);

            sim.DriveTo(o.Legs[0].Place.Access);
            sim.Stop(21);
            Assert.Equal(o.MassTonnes, sim.Svc.GetActiveMission().PayloadTonnes, 6);
            foreach (MissionLeg drop in o.Legs.Skip(1))
            {
                sim.DriveTo(drop.Place.Access);
                sim.Stop(21);
            }
            Assert.Null(sim.Svc.GetActiveMission());
            LedgerEntryView settle = sim.Svc.GetLedger().Single(e => e.Type == LedgerEntryType.MissionSettlement);
            Assert.EndsWith("-COMPLETED", settle.TransactionId);
            Assert.True(settle.AmountCents >= o.ContractValueCents);
        }

        [Fact]
        public void PayloadIncreasesFuelConsumption()
        {
            CareerSim sim = TruckSim(out _);
            double before = sim.Svc.GetVehicle(sim.VehicleId).FuelPercent;
            sim.Drive(1, 10, 1000);
            double emptyUse = before - sim.Svc.GetVehicle(sim.VehicleId).FuelPercent;

            MissionOffer o = FindOffer(sim, CareerKind.Freight, x => x.MassTonnes >= 4);
            Assert.NotNull(o);
            sim.Svc.Accept(o.Id, sim.VehicleId, sim.Now);
            sim.DriveTo(o.Legs[0].Place.Access);
            sim.Stop(21);
            before = sim.Svc.GetVehicle(sim.VehicleId).FuelPercent;
            sim.Drive(1, 10, 1000);
            double loadedUse = before - sim.Svc.GetVehicle(sim.VehicleId).FuelPercent;
            Assert.True(loadedUse > emptyUse * 1.2, "loaded " + loadedUse + " empty " + emptyUse);
        }

        [Fact]
        public void StarterFreightProvidesAServiceTruckAtSixtyPercentPay()
        {
            var sim = new CareerSim(TestCities.Freight());
            sim.Svc.DebugSeedXp(CareerKind.Delivery, 650);
            MissionOffer o = FindOffer(sim, CareerKind.Freight, x => true);
            Assert.NotNull(o);
            Assert.True(o.ProvidesServiceVehicle);
            CareerResult r = sim.Svc.Accept(o.Id, null, sim.Now);
            Assert.True(r.IsOk, r.ToString());
            CareerEvent provided = r.Events.Single(e => e.Kind == CareerEventKind.ServiceVehicleProvided);
            VehicleView sv = sim.Svc.GetVehicle(provided.VehicleId);
            Assert.Equal(VehicleKind.Service, sv.Kind);
            Assert.Equal(VehicleClass.RigidTruck, sv.Class);
            sim.VehicleId = sv.Id;
            sim.Drive(0.5, 0, 0);
            sim.DriveTo(o.Legs[0].Place.Access);
            sim.Stop(21);
            foreach (MissionLeg drop in o.Legs.Skip(1))
            {
                sim.DriveTo(drop.Place.Access);
                sim.Stop(21);
            }
            Assert.Null(sim.Svc.GetActiveMission());
            Assert.Contains(sim.Events, e => e.Kind == CareerEventKind.VehicleReleased && e.VehicleId == sv.Id);
            Assert.Equal(VehicleLocationState.Removed, sim.Svc.GetVehicle(sv.Id).State);
        }

        [Fact]
        public void FreightIsLockedUntilUnlocked()
        {
            var sim = new CareerSim(TestCities.Freight());
            Assert.Empty(sim.Svc.GetOffers(CareerKind.Freight, sim.Pos, sim.Now));
            Assert.Equal(CareerResultCode.Locked, sim.Svc.RentVehicle("truck.rental", null, sim.Now).Code);
        }
    }
}
