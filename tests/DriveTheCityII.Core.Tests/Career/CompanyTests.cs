using System.Collections.Generic;
using System.Linq;
using DriveTheCityII.Core.Career;
using Xunit;

namespace DriveTheCityII.Core.Tests.Career
{
    public class CompanyTests
    {
        private const double Period = CareerBalance.CompanyPeriodSeconds;

        private static string ModelFor(VehicleClass cls)
        {
            switch (cls)
            {
                case VehicleClass.Van:
                    return "van.light";
                case VehicleClass.RigidTruck:
                    return "truck.rigid";
                case VehicleClass.Bus:
                    return "bus.city";
                default:
                    return "car.used";
            }
        }

        private static CareerSim Unlocked(ulong seed = 5)
        {
            var sim = new CareerSim(TestCities.Taxi(), seed);
            foreach (CareerKind k in new[] { CareerKind.Taxi, CareerKind.Delivery, CareerKind.Freight, CareerKind.Bus })
            {
                sim.Svc.DebugSeedXp(k, 300);
            }
            sim.Svc.DebugSetReputation(60);
            sim.Svc.DebugGrant(3000000, sim.Now);
            return sim;
        }

        /// <summary>Company with one vehicle, one driver and one contract, set up at career time 0.</summary>
        private static CareerSim CompanySim(ulong seed = 5)
        {
            CareerSim sim = Unlocked(seed);
            CareerResult found = sim.Svc.FoundCompany("Test Transport", null, "op-found", sim.Now);
            Assert.True(found.IsOk, found.ToString());
            Assert.True(sim.Svc.TransferToCompany(2500000, "op-t1", sim.Now).IsOk);
            CompanyContractOffer offer = sim.Svc.GetCompanyContractOffers(sim.Now).OrderBy(o => o.MinReliability).First();
            CareerResult fleet = sim.Svc.CompanyBuyVehicle(ModelFor(offer.RequiredClass), "op-fleet", sim.Now);
            Assert.True(fleet.IsOk, fleet.ToString());
            EmployeeCandidate cand = sim.Svc.GetCandidates(sim.Now).OrderByDescending(c => c.Reliability).First();
            Assert.True(cand.Reliability >= offer.MinReliability, "seed gives no compatible candidate");
            CareerResult hire = sim.Svc.Hire(cand.Id, sim.Now);
            Assert.True(hire.IsOk, hire.ToString());
            CareerResult contract = sim.Svc.AcceptCompanyContract(offer.Id, sim.Now);
            Assert.True(contract.IsOk, contract.ToString());
            CareerResult assign = sim.Svc.AssignEmployee(hire.Id, fleet.Id, contract.Id, sim.Now);
            Assert.True(assign.IsOk, assign.ToString());
            return sim;
        }

        private static void AdvanceTo(CareerSim sim, double t, double step)
        {
            while (sim.Now + step <= t + 1e-9)
            {
                sim.Tick(PlayerSnapshot.OnFoot(default), step);
            }
            if (sim.Now < t)
            {
                sim.Tick(PlayerSnapshot.OnFoot(default), t - sim.Now);
            }
        }

        private static string Fingerprint(CareerService svc)
        {
            CompanyView c = svc.GetCompany();
            IEnumerable<string> ledger = svc.GetCompanyLedger().Select(e => e.TransactionId + "=" + e.AmountCents + "@" + e.Time);
            IEnumerable<string> emp = c.Employees.Select(e => e.Id + ":" + e.Reliability + ":" + e.UnpaidPeriods);
            IEnumerable<string> veh = svc.GetVehicles().Select(v => v.Id + ":" + v.ConditionPercent);
            return string.Join("|", ledger) + "#" + c.CashCents + "#" + c.DebtCents + "#" + c.LastSettledPeriod + "#" + c.SimulatedJobsTotal + "#" + string.Join(",", emp) + "#" + string.Join(",", veh) + "#" + svc.GetProgress().Reputation;
        }

        [Fact]
        public void CompanyLifecycleIsLabelledAsAManagedSimulation()
        {
            CareerSim sim = CompanySim();
            CompanyView c = sim.Svc.GetCompany();
            Assert.True(c.Exists);
            Assert.Equal("Managed operations (simulated)", c.Heading);
            Assert.Equal("Your employees' work is calculated, not shown in the city.", c.Explanation);
            Assert.Single(c.Employees);
            Assert.Single(c.Contracts);
            VehicleView fleet = sim.Svc.GetVehicle(c.FleetVehicleIds.Single());
            Assert.Equal(FleetAssignment.Managed, fleet.Assignment);
            Assert.Equal("On managed contract (simulated)", fleet.ManagedBadge);
            Assert.Equal(VehicleHolder.Company, fleet.Holder);

            AdvanceTo(sim, 3 * Period, 30);
            c = sim.Svc.GetCompany();
            Assert.Equal(2, c.LastSettledPeriod);
            Assert.True(c.SimulatedJobsTotal > 0);
            CareerEvent report = sim.Events.Last(e => e.Kind == CareerEventKind.CompanySettled);
            Assert.Contains("Simulated jobs", report.Text);
            Assert.DoesNotContain("on the way", report.Text);
            // Exactly one salary line per employee per period.
            Assert.Equal(3, sim.Svc.GetCompanyLedger().Count(e => e.TransactionId.Contains("-SAL-")));
        }

        [Fact]
        public void SettlementAcrossPausesBuildingAndReloadsEqualsContinuousSettlement()
        {
            double horizon = 50 * Period;
            CareerSim continuous = CompanySim();
            AdvanceTo(continuous, horizon, 10);

            CareerSim paused = CompanySim();
            AdvanceTo(paused, 3.5 * Period, 7);
            paused.Building(20);
            for (int i = 0; i < 10; i++)
            {
                paused.Tick(PlayerSnapshot.OnFoot(default), 0); // paused: the career clock does not move
            }
            paused.Reload();
            AdvanceTo(paused, 15.2 * Period, 15.2 * Period - paused.Now); // one jump of ~12 periods
            paused.Reload();
            paused.Tick(PlayerSnapshot.OnFoot(default), horizon - paused.Now); // a 35-period jump: amortised (30 per check)
            Assert.True(paused.Svc.GetCompany().LastSettledPeriod < 49);
            paused.Tick(PlayerSnapshot.OnFoot(default), 0);

            Assert.Equal(49, continuous.Svc.GetCompany().LastSettledPeriod);
            Assert.Equal(Fingerprint(continuous.Svc), Fingerprint(paused.Svc));
        }

        [Fact]
        public void RestoringAnOlderSaveResettlesInItsOwnTimelineWithoutDoubleCounting()
        {
            CareerSim a = CompanySim();
            AdvanceTo(a, 6 * Period + 100, 25);
            string old = a.Svc.ToJsonText();
            AdvanceTo(a, 20 * Period, 25);
            string finalA = Fingerprint(a.Svc);

            CareerService restored = CareerService.FromJsonText(old, a.World, out CareerLoadResult r);
            Assert.True(r.IsOk, r.ToString());
            Assert.Equal(5, restored.GetCompany().LastSettledPeriod);
            a.Svc = restored;
            a.Now = restored.ClockSeconds;
            AdvanceTo(a, 20 * Period, 300);
            Assert.Equal(finalA, Fingerprint(a.Svc));
        }

        [Fact]
        public void FoundingIsGatedByLevelReputationNameAndFunds()
        {
            var sim = new CareerSim(TestCities.Taxi());
            Assert.Equal(CareerResultCode.Locked, sim.Svc.FoundCompany("Too Early", null, null, sim.Now).Code);
            CareerSim ok = Unlocked();
            Assert.Equal(CareerResultCode.InvalidArgument, ok.Svc.FoundCompany("ab", null, null, ok.Now).Code);
            Assert.Equal(CareerResultCode.InvalidArgument, ok.Svc.FoundCompany("bad\nname", null, null, ok.Now).Code);
            CareerSim poor = new CareerSim(TestCities.Taxi());
            foreach (CareerKind k in new[] { CareerKind.Taxi, CareerKind.Delivery, CareerKind.Freight, CareerKind.Bus })
            {
                poor.Svc.DebugSeedXp(k, 300);
            }
            poor.Svc.DebugSetReputation(60);
            Assert.Equal(CareerResultCode.InsufficientFunds, poor.Svc.FoundCompany("Poor Co", null, null, poor.Now).Code);

            long before = ok.Svc.WalletBalanceCents;
            CareerResult f = ok.Svc.FoundCompany("Good Co", "res-a", "op", ok.Now);
            Assert.True(f.IsOk);
            Assert.Equal("CF-" + f.Id, f.TransactionId);
            Assert.Equal(before - CareerBalance.CompanyFoundingCents, ok.Svc.WalletBalanceCents);
            Assert.Equal(CareerResultCode.AlreadyApplied, ok.Svc.FoundCompany("Good Co", null, "op", ok.Now).Code);
        }

        [Fact]
        public void TransfersArePairedAndIdempotentAndNeverAutomatic()
        {
            CareerSim sim = Unlocked();
            sim.Svc.FoundCompany("Pair Co", null, null, sim.Now);
            long wallet = sim.Svc.WalletBalanceCents;
            Assert.True(sim.Svc.TransferToCompany(10000, "op-x", sim.Now).IsOk);
            Assert.Equal(CareerResultCode.AlreadyApplied, sim.Svc.TransferToCompany(10000, "op-x", sim.Now).Code);
            Assert.Equal(wallet - 10000, sim.Svc.WalletBalanceCents);
            Assert.Equal(10000, sim.Svc.GetCompany().CashCents);
            Assert.True(sim.Svc.TransferToWallet(4000, "op-y", sim.Now).IsOk);
            Assert.Equal(wallet - 6000, sim.Svc.WalletBalanceCents);
            Assert.Equal(6000, sim.Svc.GetCompany().CashCents);
            Assert.Equal(CareerResultCode.InsufficientFunds, sim.Svc.TransferToWallet(999999, null, sim.Now).Code);
            Assert.Contains(sim.Svc.GetLedger(), e => e.TransactionId == "CT-T-op-x-W");
            Assert.Contains(sim.Svc.GetCompanyLedger(), e => e.TransactionId == "CT-T-op-x-C");
        }

        [Fact]
        public void UnpaidSalariesCreateDebtArrearsAndResignationsWithoutTouchingTheWallet()
        {
            CareerSim sim = Unlocked();
            sim.Svc.FoundCompany("Broke Co", null, null, sim.Now);
            EmployeeCandidate cand = sim.Svc.GetCandidates(sim.Now).First();
            Assert.Equal(CareerResultCode.InsufficientFunds, sim.Svc.Hire(cand.Id, sim.Now).Code);
            sim.Svc.TransferToCompany(CareerBalance.HiringFeeCents, null, sim.Now);
            Assert.True(sim.Svc.Hire(cand.Id, sim.Now).IsOk);
            long wallet = sim.Svc.WalletBalanceCents;

            AdvanceTo(sim, 1 * Period, 60);
            CompanyView c = sim.Svc.GetCompany();
            Assert.True(c.InArrears);
            Assert.Equal(cand.SalaryCents, c.DebtCents);
            EmployeeCandidate next = sim.Svc.GetCandidates(sim.Now).First();
            Assert.Equal(CareerResultCode.CompanyInArrears, sim.Svc.Hire(next.Id, sim.Now).Code);

            AdvanceTo(sim, 3 * Period, 60);
            Assert.Empty(sim.Svc.GetCompany().Employees);
            Assert.Contains(sim.Events, e => e.Key == "DTC.Company.Resigned");
            Assert.Equal(wallet, sim.Svc.WalletBalanceCents);

            int rep = sim.Svc.GetProgress().Reputation;
            Assert.True(sim.Svc.DissolveCompany(sim.Now).IsOk);
            Assert.False(sim.Svc.GetCompany().Exists);
            Assert.Equal(rep - 10, sim.Svc.GetProgress().Reputation);
            Assert.Equal(wallet, sim.Svc.WalletBalanceCents);
        }

        [Fact]
        public void ManagedVehiclesCannotBeDrivenOrUsedForMissions()
        {
            CareerSim sim = CompanySim();
            string fleet = sim.Svc.GetCompany().FleetVehicleIds.Single();
            VehicleView before = sim.Svc.GetVehicle(fleet);
            sim.VehicleId = fleet;
            sim.Drive(10, 10, 100);
            Assert.Equal(before.OdometerKm, sim.Svc.GetVehicle(fleet).OdometerKm);
            Assert.Equal(CareerResultCode.InvalidState, sim.Svc.RetrieveVehicle(fleet, sim.Now).Code);
            Assert.Equal(CareerResultCode.InvalidState, sim.Svc.Refuel(fleet, "svc-fuel", 1, null, sim.Now).Code);
            var offers = sim.Svc.GetOffers(CareerKind.Taxi, sim.Pos, sim.Now);
            if (offers.Count > 0 && offers[0].AllowedClasses.Contains(before.Class))
            {
                Assert.Equal(CareerResultCode.InvalidState, sim.Svc.Accept(offers[0].Id, fleet, sim.Now).Code);
            }
        }
    }
}
