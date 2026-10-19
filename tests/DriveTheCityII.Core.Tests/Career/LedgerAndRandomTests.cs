using System.Collections.Generic;
using System.Linq;
using DriveTheCityII.Core.Career;
using Xunit;

namespace DriveTheCityII.Core.Tests.Career
{
    public class LedgerAndRandomTests
    {
        [Fact]
        public void LedgerIsAppendOnlyIdempotentAndNeverNegative()
        {
            var l = new Ledger();
            Assert.Equal(LedgerAppendStatus.Appended, l.Append("INIT-p", LedgerEntryType.InitialBalance, 1000, 0, "p", "k", "t"));
            Assert.Equal(LedgerAppendStatus.AlreadyApplied, l.Append("INIT-p", LedgerEntryType.InitialBalance, 1000, 0, "p", "k", "t"));
            Assert.Equal(1000, l.Balance);
            Assert.Equal(LedgerAppendStatus.InsufficientFunds, l.Append("X", LedgerEntryType.Fuel, -1001, 0, "v", "k", "t"));
            Assert.Equal(LedgerAppendStatus.Invalid, l.Append(null, LedgerEntryType.Fuel, -1, 0, "v", "k", "t"));
            Assert.Equal(LedgerAppendStatus.Appended, l.Append("MS-m1-COMPLETED", LedgerEntryType.MissionSettlement, 500, 1, "m1", "k", "t"));
            // A bug that tries to settle the same mission with another outcome is rejected (SPEC-MISSION 8 b).
            Assert.Equal(LedgerAppendStatus.AlreadySettled, l.Append("MS-m1-FAILED", LedgerEntryType.MissionSettlement, 0, 2, "m1", "k", "t"));
            Assert.Equal(1500, l.Balance);
            Assert.Equal(1500, l.Entries.Last().BalanceAfterCents);
            Assert.Equal(l.Entries.Sum(e => e.AmountCents), l.Balance);
            Assert.True(l.HasSettlement("m1"));
        }

        [Fact]
        public void RandomIsDeterministicAndResumesFromPersistedState()
        {
            var a = new DeterministicRandom(12345);
            var b = new DeterministicRandom(12345);
            for (int i = 0; i < 100; i++)
            {
                Assert.Equal(a.NextULong(), b.NextULong());
            }
            Assert.True(DeterministicRandom.TryParseState(a.StateHex, out ulong state));
            var c = new DeterministicRandom(state);
            for (int i = 0; i < 100; i++)
            {
                Assert.Equal(a.NextDouble(), c.NextDouble());
                int n = a.NextRange(3, 7);
                Assert.Equal(n, c.NextRange(3, 7));
                Assert.InRange(n, 3, 7);
            }
            Assert.False(DeterministicRandom.TryParseState("not-hex", out _));

            var x = DeterministicRandom.FromParts("C1", "5", "E1");
            var y = DeterministicRandom.FromParts("C1", "5", "E1");
            var z = DeterministicRandom.FromParts("C1", "6", "E1");
            ulong xv = x.NextULong();
            Assert.Equal(xv, y.NextULong());
            Assert.NotEqual(xv, z.NextULong());

            var list1 = Enumerable.Range(0, 20).ToList();
            var list2 = Enumerable.Range(0, 20).ToList();
            new DeterministicRandom(9).Shuffle(list1);
            new DeterministicRandom(9).Shuffle(list2);
            Assert.Equal(list1, list2);
            Assert.Equal(Enumerable.Range(0, 20), list1.OrderBy(v => v));
        }

        [Fact]
        public void StableHashDoesNotDependOnTheRuntime()
        {
            // FNV-1a over UTF-16 code units; a fixed value guards against accidental use of string.GetHashCode.
            Assert.Equal(StableHash.Fnv1a64("abc"), StableHash.Fnv1a64("abc"));
            Assert.NotEqual(StableHash.Fnv1a64("abc"), StableHash.Fnv1a64("abd"));
            Assert.Equal(16, StableHash.Hex(StableHash.Fnv1a64("x")).Length);
            Assert.Equal(14695981039346656037UL, StableHash.Fnv1a64(string.Empty));
        }

        [Fact]
        public void LevelsXpAndUnlocksFollowTheEconomyTable()
        {
            Assert.Equal(1, Progression.LevelFor(0));
            Assert.Equal(1, Progression.LevelFor(99));
            Assert.Equal(2, Progression.LevelFor(100));
            Assert.Equal(10, Progression.LevelFor(6200));
            Assert.Equal(10, Progression.LevelFor(100000));
            Assert.Equal(-1, Progression.NextThreshold(7000));
            Assert.Equal(Progression.MissionXp(1000, 0), (long)System.Math.Round((10 + 3) * 0.5, System.MidpointRounding.AwayFromZero));
            Assert.Equal(16, Progression.MissionXp(1000, 100));

            var p = new Progression();
            Assert.True(p.IsUnlocked(Unlock.BasicRental));
            Assert.False(p.IsUnlocked(Unlock.BusShifts));
            Assert.False(p.IsUnlocked(Unlock.Freight));
            p.AddXp(CareerKind.Taxi, 300);
            Assert.True(p.IsUnlocked(Unlock.BusShifts));
            p.AddXp(CareerKind.Delivery, 100);
            Assert.True(p.IsUnlocked(Unlock.Freight)); // total level 3 + 2 + 1 + 1 = 7 >= 6
            Assert.False(p.IsUnlocked(Unlock.CompanyFounding));
            p.AddXp(CareerKind.Taxi, -50);
            Assert.Equal(300, p.Xp(CareerKind.Taxi)); // XP never decreases
            p.AddReputation(500);
            Assert.Equal(100, p.Reputation);
        }

        [Fact]
        public void DeferredChargesAreCappedAndRepaidFromSettlements()
        {
            var sim = new CareerSim(TestCities.Taxi(), 42, new CareerOptions { InitialBalanceCents = 0 });
            Assert.True(sim.Svc.IsTestSeeded);
            string basic = sim.RentBasicAndBoard();
            // Mandatory recovery charges beyond the wallet become deferred charges, capped at 1,000 cr.
            for (int i = 0; i < 40; i++)
            {
                string car = basic;
                sim.Svc.RequestRoadsideService(car, RoadsideService.Tow, "op-tow-" + i, sim.Now, 500000);
            }
            Assert.Equal(0, sim.Svc.DeferredChargesCents); // basic rental recovery is free
            sim.Svc.DebugGrant(65000, sim.Now);
            string owned = sim.Svc.BuyVehicle("car.used", null, sim.Now).Id;
            for (int i = 0; i < 40; i++)
            {
                sim.Svc.RequestRoadsideService(owned, RoadsideService.Tow, "op-tow-owned-" + i, sim.Now, 50000);
            }
            Assert.Equal(CareerBalance.DeferredChargeCapCents, sim.Svc.DeferredChargesCents);
            Assert.Equal(0, sim.Svc.WalletBalanceCents);

            TaxiTests.RunTaxi(sim);
            LedgerEntryView repay = sim.Svc.GetLedger().Single(e => e.Type == LedgerEntryType.DeferredChargeRepayment);
            LedgerEntryView settle = sim.Svc.GetLedger().Single(e => e.Type == LedgerEntryType.MissionSettlement);
            Assert.Equal("DR-" + settle.TransactionId, repay.TransactionId);
            Assert.Equal(-CareerMath.RoundCents(settle.AmountCents * 0.25), repay.AmountCents);
            Assert.Equal(CareerBalance.DeferredChargeCapCents + repay.AmountCents, sim.Svc.DeferredChargesCents);
            Assert.True(sim.Svc.WalletBalanceCents >= 0);
            Assert.Equal(sim.LedgerSum(), sim.Svc.WalletBalanceCents);
        }

        [Fact]
        public void TutorialHooksFireOncePerTrigger()
        {
            var sim = new CareerSim(TestCities.Taxi());
            sim.RentBasicAndBoard();
            sim.Svc.ReturnRental(sim.VehicleId, true, null, sim.Now);
            sim.RentBasicAndBoard();
            Assert.Single(sim.Events, e => e.Key == "DTC.Tutorial.BasicRental");
        }

        [Fact]
        public void EveryWalletMovementIsALedgerEntryWithAUniqueTransactionId()
        {
            CareerSim sim = new CareerSim(TestCities.Taxi());
            sim.Svc.DebugGrant(500000, sim.Now);
            sim.RentBasicAndBoard();
            TaxiTests.RunTaxi(sim);
            string car = sim.Svc.BuyVehicle("car.new", null, sim.Now).Id;
            sim.VehicleId = car;
            sim.Drive(10, 10, 1000);
            sim.Svc.Refuel(car, "svc-fuel", 1, null, sim.Now);
            sim.Svc.SellVehicle(car, sim.Now);
            List<LedgerEntryView> ledger = sim.Svc.GetLedger().ToList();
            Assert.Equal(ledger.Count, ledger.Select(e => e.TransactionId).Distinct().Count());
            Assert.Equal(ledger.Sum(e => e.AmountCents), sim.Svc.WalletBalanceCents);
            long running = 0;
            foreach (LedgerEntryView e in ledger)
            {
                running += e.AmountCents;
                Assert.Equal(running, e.BalanceAfterCents);
                Assert.True(e.BalanceAfterCents >= 0);
            }
        }
    }
}
