using System.Linq;
using DriveTheCityII.Core.Career;
using Xunit;

namespace DriveTheCityII.Core.Tests.Career
{
    public class TaxiTests
    {
        private static MissionOffer SingleTaxiOffer(CareerSim sim)
        {
            var offers = sim.Svc.GetOffers(CareerKind.Taxi, sim.Pos, sim.Now);
            Assert.Single(offers);
            MissionOffer o = offers[0];
            Assert.Equal("res-a", o.Legs[0].Place.Id);
            Assert.Equal("off-b", o.Legs[1].Place.Id);
            return o;
        }

        /// <summary>Runs pickup and drop-off; returns the mission id.</summary>
        internal static string RunTaxi(CareerSim sim)
        {
            MissionOffer o = SingleTaxiOffer(sim);
            CareerResult accepted = sim.Svc.Accept(o.Id, sim.VehicleId, sim.Now);
            Assert.True(accepted.IsOk, accepted.ToString());
            string id = accepted.Id;
            sim.AfterStep?.Invoke(sim);
            sim.Drive(0.5, 0, 0);
            sim.DriveTo("res-a");
            sim.Stop(5);
            sim.DriveTo("off-b");
            sim.Stop(4);
            return id;
        }

        [Fact]
        public void FullTaxiLifecycleSettlesOnceWithFareTipShareXpAndReputation()
        {
            var sim = new CareerSim(TestCities.Taxi());
            sim.Walk(2);
            sim.RentBasicAndBoard();
            MissionOffer o = SingleTaxiOffer(sim);
            Assert.True(o.PayMaxCents > o.PayMinCents);
            Assert.Equal(2500, o.PlannedMeters, 3);
            Assert.True(o.XpPreview > 0);
            Assert.Contains(VehicleClass.Car, o.AllowedClasses);

            CareerResult r = sim.Svc.Accept(o.Id, sim.VehicleId, sim.Now);
            Assert.True(r.IsOk, r.ToString());
            Assert.Equal(MissionState.Accepted, sim.Svc.GetActiveMission().State);

            sim.Drive(0.5, 0, 0);
            ActiveMissionView view = sim.Svc.GetActiveMission();
            Assert.Equal(MissionState.Active, view.State);
            Assert.Equal("res-a", view.TargetPlaceId);
            Assert.Equal(LegKind.Pickup, view.CurrentLegKind);
            Assert.True(view.DistanceHintMeters > 200);
            Assert.True(view.PatienceRemaining > 0);

            sim.DriveTo("res-a");
            sim.Stop(5);
            view = sim.Svc.GetActiveMission();
            Assert.Equal(PassengerState.Aboard, view.PassengerState);
            Assert.Equal("off-b", view.TargetPlaceId);

            sim.DriveTo("off-b");
            sim.Stop(4);
            Assert.Null(sim.Svc.GetActiveMission());

            // 5-star ride: fare = base x 1.25 (offer PayMax); the basic rental takes 25% (separate entry).
            Assert.Equal(1, sim.CountSettlements(r.Id));
            LedgerEntryView settle = sim.Svc.GetLedger().Single(e => e.Type == LedgerEntryType.MissionSettlement);
            Assert.Equal("MS-" + r.Id + "-COMPLETED", settle.TransactionId);
            Assert.Equal(o.PayMaxCents, settle.AmountCents);
            LedgerEntryView share = sim.Svc.GetLedger().Single(e => e.Type == LedgerEntryType.RentalCharge);
            Assert.Equal(-CareerMath.RoundCents(o.PayMaxCents * 0.25), share.AmountCents);
            Assert.Equal(CareerBalance.InitialBalanceCents + settle.AmountCents + share.AmountCents, sim.Svc.WalletBalanceCents);
            Assert.Equal(sim.LedgerSum(), sim.Svc.WalletBalanceCents);

            ProgressView p = sim.Svc.GetProgress();
            Assert.Equal(Progression.MissionXp(2500, 100), p.Careers.Single(c => c.Career == CareerKind.Taxi).Xp);
            Assert.Equal(52, p.Reputation);
            Assert.Contains(sim.Events, e => e.Kind == CareerEventKind.Settled && e.TransactionId == settle.TransactionId);
            Assert.Contains(sim.Events, e => e.Kind == CareerEventKind.Notification && e.Key == "DTC.Tutorial.FirstSettlement");
        }

        [Fact]
        public void MovingDuringBoardingRestartsTheBoardingStep()
        {
            var sim = new CareerSim(TestCities.Taxi());
            sim.RentBasicAndBoard();
            MissionOffer o = SingleTaxiOffer(sim);
            sim.Svc.Accept(o.Id, sim.VehicleId, sim.Now);
            sim.DriveTo("res-a");
            sim.Stop(2);
            Assert.Equal(LegState.Servicing, sim.Svc.GetActiveMission().CurrentLegState);
            sim.Drive(1, 3, 3); // rolling above 0.5 m/s aborts boarding
            Assert.Equal(LegState.Approaching, sim.Svc.GetActiveMission().CurrentLegState);
            Assert.Equal(PassengerState.Waiting, sim.Svc.GetActiveMission().PassengerState);
            sim.Stop(5);
            Assert.Equal(PassengerState.Aboard, sim.Svc.GetActiveMission().PassengerState);
        }

        [Fact]
        public void UnreachableOrUnknownDestinationIsNeverOffered()
        {
            FakeCareerWorld w = TestCities.Taxi();
            w.Add("com-c", PlaceKind.Commercial, 0, 2500, "Mall C");
            w.Unreachable.Add("off-b");
            var sim = new CareerSim(w);
            for (int i = 0; i < 10; i++)
            {
                var offers = sim.Svc.RefreshOffers(CareerKind.Taxi, sim.Pos, sim.Now);
                Assert.NotEmpty(offers);
                Assert.All(offers, o => Assert.DoesNotContain(o.Legs, l => l.Place.Id == "off-b"));
            }

            // Unknown is never treated as reachable (REQ-CAREER-002), even by the relaxed starter pass.
            w.Unknown.Add("com-c");
            w.CityRevision++;
            Assert.Empty(sim.Svc.RefreshOffers(CareerKind.Taxi, sim.Pos, sim.Now));

            // Unreachable pickup: nothing either.
            w.Unknown.Clear();
            w.Unreachable.Add("res-a");
            w.CityRevision++;
            Assert.Empty(sim.Svc.RefreshOffers(CareerKind.Taxi, sim.Pos, sim.Now));
        }

        [Fact]
        public void AcceptRevalidatesAndWithdrawsAnOfferWhoseDestinationWasDemolished()
        {
            FakeCareerWorld w = TestCities.Taxi();
            var sim = new CareerSim(w);
            sim.RentBasicAndBoard();
            MissionOffer o = SingleTaxiOffer(sim);
            w.Remove("off-b");
            CareerResult r = sim.Svc.Accept(o.Id, sim.VehicleId, sim.Now);
            Assert.Equal(CareerResultCode.OfferWithdrawn, r.Code);
            Assert.Null(sim.Svc.GetActiveMission());
            Assert.Equal(CareerBalance.InitialBalanceCents, sim.Svc.WalletBalanceCents);
        }

        [Fact]
        public void OffersExpireAfterTheirTtlAndExpiredOffersCannotBeAccepted()
        {
            var sim = new CareerSim(TestCities.Taxi());
            sim.RentBasicAndBoard();
            MissionOffer o = SingleTaxiOffer(sim);
            sim.Stop(CareerBalance.OfferTtlSeconds + 5, 5);
            Assert.Equal(MissionState.Expired, o.State);
            Assert.Equal(CareerResultCode.OfferExpired, sim.Svc.Accept(o.Id, sim.VehicleId, sim.Now).Code);
        }

        [Fact]
        public void DemolishedDestinationBeforePickupCancelsWithNoFaultAndNoPayout()
        {
            FakeCareerWorld w = TestCities.Taxi();
            var sim = new CareerSim(w);
            sim.RentBasicAndBoard();
            MissionOffer o = SingleTaxiOffer(sim);
            string id = sim.Svc.Accept(o.Id, sim.VehicleId, sim.Now).Id;
            sim.DriveTo(new MapPoint(150, 0, 0));
            w.Remove("off-b");
            sim.Building();
            sim.Drive(0.5, 0, 0);

            Assert.Null(sim.Svc.GetActiveMission());
            Assert.Contains(sim.Events, e => e.Kind == CareerEventKind.MissionStateChanged && e.ToState == MissionState.Invalidated);
            Assert.Contains(sim.Events, e => e.Kind == CareerEventKind.MissionStateChanged && e.ToState == MissionState.Cancelled && e.Fault == MissionFault.NoFault);
            LedgerEntryView settle = sim.Svc.GetLedger().Single(e => e.Type == LedgerEntryType.MissionSettlement);
            Assert.Equal("MS-" + id + "-CANCELLED", settle.TransactionId);
            Assert.Equal(0, settle.AmountCents);
            Assert.Equal(CareerBalance.InitialBalanceCents, sim.Svc.WalletBalanceCents);
            Assert.Equal(50, sim.Svc.GetProgress().Reputation);
        }

        [Fact]
        public void DemolishedDestinationWithPassengersAboardPaysOnlyTheDistanceCoveredOnceWithNoPenalty()
        {
            FakeCareerWorld w = TestCities.Taxi();
            var sim = new CareerSim(w);
            sim.RentBasicAndBoard();
            MissionOffer o = SingleTaxiOffer(sim);
            string id = sim.Svc.Accept(o.Id, sim.VehicleId, sim.Now).Id;
            sim.DriveTo("res-a");
            sim.Stop(5);
            sim.DriveTo(new MapPoint(1300, 0, 0)); // half way (straight-line progress 50%)
            w.Remove("off-b");
            sim.Drive(0.5, 0, 0);
            sim.Drive(0.5, 0, 0);

            Assert.Null(sim.Svc.GetActiveMission());
            Assert.Equal(1, sim.CountSettlements(id));
            LedgerEntryView settle = sim.Svc.GetLedger().Single(e => e.Type == LedgerEntryType.MissionSettlement);
            Assert.Equal(CareerMath.RoundCents(o.ContractValueCents * 0.5), settle.AmountCents);
            Assert.Equal(50, sim.Svc.GetProgress().Reputation);
        }

        [Fact]
        public void DemolishedDestinationIsSubstitutedByANearbyPlaceAndTheFareIsRecalculated()
        {
            FakeCareerWorld w = TestCities.Taxi();
            var sim = new CareerSim(w);
            sim.RentBasicAndBoard();
            MissionOffer o = SingleTaxiOffer(sim);
            sim.Svc.Accept(o.Id, sim.VehicleId, sim.Now);
            sim.DriveTo("res-a");
            sim.Stop(5);
            w.Add("off-b2", PlaceKind.Office, 2300, 300, "Tower B2");
            w.Remove("off-b");
            sim.Drive(0.5, 0, 0);
            ActiveMissionView v = sim.Svc.GetActiveMission();
            Assert.NotNull(v);
            Assert.Equal("off-b2", v.TargetPlaceId);
            Assert.Contains(sim.Events, e => e.Key == "DTC.Taxi.Substituted");
            sim.DriveTo("off-b2");
            sim.Stop(4);
            Assert.Null(sim.Svc.GetActiveMission());
            LedgerEntryView settle = sim.Svc.GetLedger().Single(e => e.Type == LedgerEntryType.MissionSettlement);
            Assert.EndsWith("-COMPLETED", settle.TransactionId);
            Assert.True(settle.AmountCents > o.PayMaxCents); // longer planned route pays more
        }

        [Fact]
        public void PlayerCancellationBeforeAndAfterPickupFollowsTheTaxiTable()
        {
            var sim = new CareerSim(TestCities.Taxi());
            sim.RentBasicAndBoard();
            MissionOffer o = SingleTaxiOffer(sim);
            sim.Svc.Accept(o.Id, sim.VehicleId, sim.Now);
            CareerResult c = sim.Svc.Cancel("changed my mind", sim.Now);
            Assert.True(c.IsOk);
            Assert.EndsWith("-CANCELLED", c.TransactionId);
            Assert.Equal(49, sim.Svc.GetProgress().Reputation);
            Assert.Equal(CareerBalance.InitialBalanceCents, sim.Svc.WalletBalanceCents);
            Assert.Equal(CareerResultCode.NotFound, sim.Svc.Cancel("again", sim.Now).Code);

            o = SingleTaxiOffer(sim);
            sim.Svc.Accept(o.Id, sim.VehicleId, sim.Now);
            sim.DriveTo("res-a");
            sim.Stop(5);
            sim.Svc.Cancel(null, sim.Now);
            Assert.Equal(46, sim.Svc.GetProgress().Reputation); // -3 with passengers aboard, no fee
            Assert.Equal(CareerBalance.InitialBalanceCents, sim.Svc.WalletBalanceCents);
        }

        [Fact]
        public void PatienceRunsOutBeforePickupAndFailsWithTheLighterPenalty()
        {
            var sim = new CareerSim(TestCities.Taxi());
            sim.RentBasicAndBoard();
            MissionOffer o = SingleTaxiOffer(sim);
            sim.Svc.Accept(o.Id, sim.VehicleId, sim.Now);
            sim.Stop(o.PatienceSeconds + 5, 5);
            Assert.Null(sim.Svc.GetActiveMission());
            Assert.Contains(sim.Events, e => e.Kind == CareerEventKind.MissionStateChanged && e.ToState == MissionState.Failed && e.Key == "DTC.Taxi.Fail.Patience");
            Assert.Equal(49, sim.Svc.GetProgress().Reputation);
        }

        [Fact]
        public void AcceptedFareThatIsNeverStartedFailsOnPatienceBeforeTheStartWindow()
        {
            var sim = new CareerSim(TestCities.Taxi());
            sim.RentBasicAndBoard();
            MissionOffer o = SingleTaxiOffer(sim);
            Assert.True(o.PatienceSeconds < CareerBalance.StartWindowSeconds);
            sim.Svc.Accept(o.Id, null, sim.Now);
            sim.Walk(CareerBalance.StartWindowSeconds + 10, 10);
            Assert.Null(sim.Svc.GetActiveMission());
            Assert.Contains(sim.Events, e => e.ToState == MissionState.Failed && e.Key == TaxiPatienceKey);
            Assert.Equal(49, sim.Svc.GetProgress().Reputation);
        }

        private const string TaxiPatienceKey = "DTC.Taxi.Fail.Patience";

        [Fact]
        public void LostMissionVehicleEndsWithNoFaultAndPaysTheDistanceCovered()
        {
            var sim = new CareerSim(TestCities.Taxi());
            sim.RentBasicAndBoard();
            MissionOffer o = SingleTaxiOffer(sim);
            string id = sim.Svc.Accept(o.Id, sim.VehicleId, sim.Now).Id;
            sim.DriveTo("res-a");
            sim.Stop(5);
            sim.DriveTo(new MapPoint(800, 0, 0)); // 25% of the straight-line trip
            CareerResult r = sim.Svc.Abandon("vehicle entity lost", sim.Now);
            Assert.True(r.IsOk);
            Assert.Contains(r.Events, e => e.ToState == MissionState.Invalidated);
            Assert.Contains(r.Events, e => e.ToState == MissionState.Cancelled && e.Fault == MissionFault.NoFault);
            LedgerEntryView settle = sim.Svc.GetLedger().Single(e => e.Type == LedgerEntryType.MissionSettlement);
            Assert.Equal("MS-" + id + "-CANCELLED", settle.TransactionId);
            Assert.Equal(CareerMath.RoundCents(o.ContractValueCents * 0.25), settle.AmountCents);
            Assert.Equal(50, sim.Svc.GetProgress().Reputation);
        }

        [Fact]
        public void ThreeSevereContactsWithPassengersAboardFailTheRide()
        {
            var sim = new CareerSim(TestCities.Taxi());
            sim.RentBasicAndBoard();
            MissionOffer o = SingleTaxiOffer(sim);
            sim.Svc.Accept(o.Id, sim.VehicleId, sim.Now);
            sim.DriveTo("res-a");
            sim.Stop(5);
            for (int i = 0; i < 3; i++)
            {
                sim.Drive(1, 10, 10, impact: 12);
            }
            Assert.Null(sim.Svc.GetActiveMission());
            Assert.Contains(sim.Events, e => e.ToState == MissionState.Failed && e.Key == "DTC.Taxi.Fail.SevereContacts");
            Assert.Equal(45, sim.Svc.GetProgress().Reputation);
        }

        [Fact]
        public void HarshDrivingLowersRideQualityAndTheTip()
        {
            var sim = new CareerSim(TestCities.Taxi());
            sim.RentBasicAndBoard();
            MissionOffer o = SingleTaxiOffer(sim);
            sim.Svc.Accept(o.Id, sim.VehicleId, sim.Now);
            sim.DriveTo("res-a");
            sim.Stop(5);
            for (int i = 0; i < 12; i++)
            {
                sim.Drive(0.5, 5, 2, longAcc: 6); // 0.61 g braking spikes, rising edges
                sim.Drive(0.5, 5, 2);
            }
            double quality = sim.Svc.GetActiveMission().RideQuality;
            Assert.True(quality < 90 && quality >= 76 - 1e-9, "quality " + quality);
            sim.DriveTo("off-b");
            sim.Stop(4);
            LedgerEntryView settle = sim.Svc.GetLedger().Single(e => e.Type == LedgerEntryType.MissionSettlement);
            Assert.True(settle.AmountCents < o.PayMaxCents);
            Assert.True(settle.AmountCents > o.PayMinCents);
        }

        [Fact]
        public void LeavingTheTaxiWithPassengersAboardFailsAfterTwoMinutes()
        {
            var sim = new CareerSim(TestCities.Taxi());
            sim.RentBasicAndBoard();
            MissionOffer o = SingleTaxiOffer(sim);
            sim.Svc.Accept(o.Id, sim.VehicleId, sim.Now);
            sim.DriveTo("res-a");
            sim.Stop(5);
            sim.Walk(60, 5);
            Assert.NotNull(sim.Svc.GetActiveMission());
            sim.Walk(70, 5);
            Assert.Null(sim.Svc.GetActiveMission());
            Assert.Contains(sim.Events, e => e.ToState == MissionState.Failed && e.Key == "DTC.Mission.Failed.Abandoned");
        }

        [Fact]
        public void BuildingModeSuspendsTheMissionAndFreezesTimers()
        {
            var sim = new CareerSim(TestCities.Taxi());
            sim.RentBasicAndBoard();
            MissionOffer o = SingleTaxiOffer(sim);
            sim.Svc.Accept(o.Id, sim.VehicleId, sim.Now);
            double patience = sim.Svc.GetActiveMission().PatienceRemaining;
            sim.Building(50);
            Assert.Equal(patience, sim.Svc.GetActiveMission().PatienceRemaining, 6);
        }

        [Fact]
        public void OnlyOneMissionAtATime()
        {
            FakeCareerWorld w = TestCities.Taxi();
            w.Add("res-z", PlaceKind.Residential, -300, 900, "Oak Residence");
            var sim = new CareerSim(w);
            sim.RentBasicAndBoard();
            var offers = sim.Svc.GetOffers(CareerKind.Taxi, sim.Pos, sim.Now);
            Assert.True(offers.Count >= 2);
            Assert.True(sim.Svc.Accept(offers[0].Id, sim.VehicleId, sim.Now).IsOk);
            Assert.Equal(CareerResultCode.MissionAlreadyActive, sim.Svc.Accept(offers[1].Id, sim.VehicleId, sim.Now).Code);
        }

        [Fact]
        public void ClockRegressionIsReportedAndIgnored()
        {
            var sim = new CareerSim(TestCities.Taxi());
            sim.Walk(10);
            var ev = sim.Svc.Tick(PlayerSnapshot.OnFoot(default), 1);
            Assert.Contains(ev, e => e.Key == "DTC.Career.ClockRegression");
            Assert.Equal(10, sim.Svc.ClockSeconds, 6);
        }
    }
}
