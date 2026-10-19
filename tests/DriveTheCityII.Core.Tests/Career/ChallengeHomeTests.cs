using System.Linq;
using DriveTheCityII.Core.Career;
using Xunit;

namespace DriveTheCityII.Core.Tests.Career
{
    public class ChallengeHomeTests
    {
        private static CareerSim WithCar()
        {
            var sim = new CareerSim(TestCities.Taxi());
            sim.RentBasicAndBoard();
            return sim;
        }

        private static string DefineTimeTrial(CareerSim sim)
        {
            CareerResult r = sim.Svc.DefineChallenge(ChallengeType.TimeTrial, "Harbour sprint", new[] { "res-a", "off-b" }, VehicleClass.Car, 0, sim.Now);
            Assert.True(r.IsOk, r.ToString());
            return r.Id;
        }

        private static void RunTimeTrial(CareerSim sim, string id)
        {
            Assert.True(sim.Svc.StartChallenge(id, sim.VehicleId, sim.Now).IsOk);
            sim.DriveTo("res-a", 20);
            sim.DriveTo(sim.World.Get("off-b").Access, 40);
            sim.Pos = default;
            sim.Drive(1, 0, 0);
        }

        [Fact]
        public void TimeTrialRewardsEachTierOncePerRouteVersion()
        {
            CareerSim sim = WithCar();
            string id = DefineTimeTrial(sim);
            ChallengeView v = sim.Svc.GetChallenges().Single();
            Assert.Equal(250, v.ReferenceSeconds, 6);
            long start = sim.Svc.WalletBalanceCents;

            RunTimeTrial(sim, id);
            v = sim.Svc.GetChallenges().Single();
            Assert.Equal(MedalTier.Gold, v.Medal);
            Assert.Equal(1, v.Attempts);
            Assert.InRange(v.BestSeconds, 49, 52);
            long all = CareerBalance.ChallengeRewardCents.Sum();
            Assert.Equal(start + all, sim.Svc.WalletBalanceCents);
            Assert.Equal(CareerBalance.ChallengeRewardXp.Sum(), sim.Svc.GetProgress().Careers.Single(c => c.Career == CareerKind.Taxi).Xp);
            Assert.Contains(sim.Svc.GetLedger(), e => e.TransactionId == "CH-" + id + "-1-Gold");

            RunTimeTrial(sim, id);
            Assert.Equal(2, sim.Svc.GetChallenges().Single().Attempts);
            Assert.Equal(start + all, sim.Svc.WalletBalanceCents); // paid once per tier

            sim.World.Move("off-b", 2400, 0);
            sim.Drive(1, 0, 0);
            v = sim.Svc.GetChallenges().Single();
            Assert.Equal(2, v.RouteVersion);
            Assert.True(v.HasOlderRecords);
            Assert.Equal(0, v.Attempts);
            RunTimeTrial(sim, id);
            Assert.Equal(start + 2 * all, sim.Svc.WalletBalanceCents); // a new route version has its own rewards
        }

        [Fact]
        public void LeavingTheVehicleVoidsTheRunWithNoResult()
        {
            CareerSim sim = WithCar();
            string id = DefineTimeTrial(sim);
            sim.Svc.StartChallenge(id, sim.VehicleId, sim.Now);
            sim.DriveTo("res-a");
            sim.Walk(2);
            Assert.Contains(sim.Events, e => e.Kind == CareerEventKind.ChallengeStateChanged && e.ToState == MissionState.Cancelled);
            Assert.Equal(0, sim.Svc.GetChallenges().Single().Attempts);
            Assert.False(sim.Svc.GetChallenges().Single().Running);
        }

        [Fact]
        public void BrokenRouteMakesTheChallengeUnavailable()
        {
            CareerSim sim = WithCar();
            string id = DefineTimeTrial(sim);
            sim.World.SetUnreachable("off-b", true);
            sim.Drive(1, 0, 0);
            Assert.False(sim.Svc.GetChallenges().Single().Available);
            Assert.Equal(CareerResultCode.InvalidState, sim.Svc.StartChallenge(id, sim.VehicleId, sim.Now).Code);
            sim.World.SetUnreachable("off-b", false);
            sim.Drive(1, 0, 0);
            Assert.True(sim.Svc.GetChallenges().Single().Available);
        }

        [Fact]
        public void ParkingChallengeChecksLateralLongitudinalAndHeadingTolerances()
        {
            CareerSim sim = WithCar();
            CareerResult def = sim.Svc.DefineChallenge(ChallengeType.Parking, "Bay 1", new[] { "svc-fuel" }, VehicleClass.Car, 90, sim.Now);
            Assert.True(def.IsOk, def.ToString());
            Assert.True(sim.Svc.StartChallenge(def.Id, sim.VehicleId, sim.Now).IsOk);
            sim.Heading = 90;
            sim.DriveTo(new MapPoint(-200, 0, 1.0)); // 1 m lateral error
            sim.Stop(2);
            Assert.Equal(0, sim.Svc.GetChallenges().Single().Attempts);
            sim.Heading = 120;
            sim.DriveTo(new MapPoint(-200.3, 0, 0.2)); // position fine, heading 30 degrees off
            sim.Stop(2);
            Assert.Equal(0, sim.Svc.GetChallenges().Single().Attempts);
            sim.Heading = 95;
            sim.Stop(1);
            ChallengeView v = sim.Svc.GetChallenges().Single();
            Assert.Equal(1, v.Attempts);
            Assert.Equal(MedalTier.Gold, v.Medal);
        }

        [Fact]
        public void SevereContactFailsAParkingAttempt()
        {
            CareerSim sim = WithCar();
            CareerResult def = sim.Svc.DefineChallenge(ChallengeType.Parking, "Bay 1", new[] { "svc-fuel" }, VehicleClass.Car, 0, sim.Now);
            sim.Svc.StartChallenge(def.Id, sim.VehicleId, sim.Now);
            sim.Drive(1, 10, 10, impact: 12);
            ChallengeView v = sim.Svc.GetChallenges().Single();
            Assert.Equal(1, v.Attempts);
            Assert.Equal(MedalTier.None, v.Medal);
            Assert.Contains(sim.Events, e => e.Kind == CareerEventKind.ChallengeStateChanged && e.ToState == MissionState.Failed);
        }

        [Fact]
        public void GeneratedChallengesAreDeterministicAndNotDuplicated()
        {
            FakeCareerWorld w = TestCities.Delivery();
            var a = new CareerSim(w);
            var b = new CareerSim(w, 999);
            var la = a.Svc.GenerateChallenges(default, 0).Select(c => c.Id + c.Type).ToList();
            var lb = b.Svc.GenerateChallenges(default, 0).Select(c => c.Id + c.Type).ToList();
            Assert.NotEmpty(la);
            Assert.Equal(la, lb);
            Assert.Equal(la.Count, a.Svc.GenerateChallenges(default, 0).Count);
        }

        [Fact]
        public void MedalThresholds()
        {
            Assert.Equal(MedalTier.Gold, CareerService.MedalFor(85, 100));
            Assert.Equal(MedalTier.Silver, CareerService.MedalFor(100, 100));
            Assert.Equal(MedalTier.Bronze, CareerService.MedalFor(120, 100));
            Assert.Equal(MedalTier.None, CareerService.MedalFor(121, 100));
        }

        // ------------------------------------------------------------------ home and garage

        [Fact]
        public void HomeMustBeResidentialHasACooldownAndProvidesReturnHome()
        {
            FakeCareerWorld w = TestCities.Taxi();
            w.Add("res-c", PlaceKind.Residential, 0, 400, "Birch Residence");
            var sim = new CareerSim(w);
            Assert.Equal(CareerResultCode.InvalidArgument, sim.Svc.SetHome("off-b", sim.Now).Code);
            Assert.True(sim.Svc.SetHome("res-a", sim.Now).IsOk);
            Assert.Equal(CareerResultCode.Cooldown, sim.Svc.SetHome("res-c", sim.Now).Code);
            Assert.True(sim.Svc.TryGetRespawnPoint(out MapPoint p, out string pid));
            Assert.Equal("res-a", pid);
            Assert.Equal(300, p.X, 6);

            CareerResult home = sim.Svc.RequestReturnHome(sim.Now);
            Assert.True(home.IsOk && home.HasPoint);
            Assert.Equal(CareerResultCode.Cooldown, sim.Svc.RequestReturnHome(sim.Now).Code);
            sim.Walk(61, 61);
            Assert.True(sim.Svc.RequestReturnHome(sim.Now).IsOk);
            sim.Walk(CareerBalance.HomeChangeCooldownSeconds, 100);
            Assert.True(sim.Svc.SetHome("res-c", sim.Now).IsOk);
        }

        [Fact]
        public void DemolishedHomeBecomesATemporaryBaseAndDemolishedGarageKeepsStoredVehicles()
        {
            FakeCareerWorld w = TestCities.Taxi();
            w.Add("res-c", PlaceKind.Residential, 300, 300, "Birch Residence");
            w.Add("lot-1", PlaceKind.Commercial, -500, 0, "Parking lot");
            var sim = new CareerSim(w);
            sim.Svc.DebugGrant(100000, sim.Now);
            sim.Svc.SetHome("res-a", sim.Now);
            Assert.True(sim.Svc.SetGarage("lot-1", sim.Now).IsOk);
            string car = sim.Svc.BuyVehicle("car.used", null, sim.Now).Id;
            Assert.Equal("lot-1", sim.Svc.GetVehicle(car).StoredAtPlaceId);

            w.Remove("res-a");
            w.Remove("lot-1");
            sim.Walk(1);
            HomeView h = sim.Svc.GetHome();
            Assert.True(h.TemporaryBase);
            Assert.Equal("res-c", h.HomePlaceId);
            Assert.False(h.HasGarage);
            Assert.Contains(sim.Events, e => e.Key == "DTC.Home.Demolished");
            Assert.Contains(sim.Events, e => e.Key == "DTC.Home.GarageDemolished");
            VehicleView v = sim.Svc.GetVehicle(car);
            Assert.Equal(VehicleLocationState.Stored, v.State);
            Assert.Null(v.StoredAtPlaceId); // default storage
            Assert.True(sim.Svc.SetHome("res-c", sim.Now).IsOk); // no cooldown from a temporary base
        }

        [Fact]
        public void GarageCapacityAndExpansion()
        {
            var sim = new CareerSim(TestCities.Taxi());
            sim.Svc.DebugGrant(1000000, sim.Now);
            for (int i = 0; i < 3; i++)
            {
                Assert.True(sim.Svc.BuyVehicle("car.used", "op-" + i, sim.Now).IsOk);
            }
            string third = "V-op-2";
            Assert.Equal(VehicleLocationState.Stored, sim.Svc.GetVehicle("V-op-1").State);
            Assert.Equal(VehicleLocationState.Parked, sim.Svc.GetVehicle(third).State); // beyond capacity 2
            Assert.Equal(CareerResultCode.CapacityExceeded, sim.Svc.StoreVehicle(third, sim.Now).Code);
            Assert.True(sim.Svc.RetrieveVehicle("V-op-1", sim.Now).IsOk);
            Assert.True(sim.Svc.StoreVehicle("V-op-1", sim.Now).IsOk);
            CareerResult exp = sim.Svc.ExpandGarage("op-g", sim.Now);
            Assert.True(exp.IsOk);
            Assert.Equal(-CareerBalance.GarageExpansionCents[0], exp.AmountCents);
            Assert.Equal(CareerResultCode.AlreadyApplied, sim.Svc.ExpandGarage("op-g", sim.Now).Code);
            Assert.True(sim.Svc.StoreVehicle(third, sim.Now).IsOk);
            Assert.Equal(4, sim.Svc.GetHome().GarageCapacity);
        }
    }
}
