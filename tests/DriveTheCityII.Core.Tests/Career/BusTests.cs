using System.Collections.Generic;
using System.Linq;
using DriveTheCityII.Core.Career;
using Xunit;

namespace DriveTheCityII.Core.Tests.Career
{
    public class BusTests
    {
        private static CareerSim BusSim(out MissionOffer offer, ulong seed = 7)
        {
            var sim = new CareerSim(TestCities.Bus(), seed);
            sim.Svc.DebugSetReputation(60); // bus shifts unlocked (SPEC-ECON 5.3)
            var offers = sim.Svc.GetOffers(CareerKind.Bus, sim.Pos, sim.Now);
            Assert.Single(offers);
            offer = offers[0];
            CareerResult r = sim.Svc.Accept(offer.Id, null, sim.Now);
            Assert.True(r.IsOk, r.ToString());
            CareerEvent provided = r.Events.Single(e => e.Kind == CareerEventKind.ServiceVehicleProvided);
            Assert.Equal("depot", provided.PlaceId);
            sim.VehicleId = provided.VehicleId;
            sim.Drive(0.5, 0, 0);
            Assert.Equal(MissionState.Active, sim.Svc.GetActiveMission().State);
            return sim;
        }

        private static void ServeStop(CareerSim sim, string stopId)
        {
            sim.DriveTo(stopId);
            sim.Stop(CareerBalance.BusDwellCapSeconds + 1);
        }

        [Fact]
        public void BusShiftServesStopsInOrderWithCareerPassengersAndSettlesOnce()
        {
            CareerSim sim = BusSim(out MissionOffer offer);
            Assert.Equal("line-1", offer.LineId);
            Assert.Equal(5, offer.StopCount);
            Assert.Equal(VehicleClass.Bus, offer.AllowedClasses.Single());
            int boardedTotal = 0;
            for (int i = 0; i < 5; i++)
            {
                if (i < 4)
                {
                    boardedTotal += sim.Svc.GetActiveMission().BusWaitingAtNextStop;
                }
                ServeStop(sim, "stop-" + i);
                if (i < 4)
                {
                    ActiveMissionView v = sim.Svc.GetActiveMission();
                    Assert.Equal(i + 1, v.StopsServed);
                    Assert.True(v.BusOnboard <= CareerBalance.BusCapacity);
                }
            }
            Assert.Null(sim.Svc.GetActiveMission());
            LedgerEntryView settle = sim.Svc.GetLedger().Single(e => e.Type == LedgerEntryType.MissionSettlement);
            Assert.EndsWith("-COMPLETED", settle.TransactionId);
            // 5 stops + every boarded career passenger delivered, +20% punctuality, x1.15 evaluation.
            long baseline = 5 * CareerBalance.BusPerStopCents + boardedTotal * CareerBalance.BusPerPassengerCents;
            long bonus = CareerMath.RoundCents(baseline * 0.2);
            Assert.Equal(CareerMath.RoundCents((baseline + bonus) * 1.15), settle.AmountCents);
            Assert.Equal(62, sim.Svc.GetProgress().Reputation); // 60 seeded, +2 for an evaluation of 80 or more
            Assert.Contains(sim.Events, e => e.Kind == CareerEventKind.VehicleReleased);
        }

        [Fact]
        public void PassengerCountsAreDeterministicForTheSeed()
        {
            long Run(ulong seed)
            {
                CareerSim sim = BusSim(out _, seed);
                for (int i = 0; i < 5; i++)
                {
                    ServeStop(sim, "stop-" + i);
                }
                return sim.Svc.GetLedger().Single(e => e.Type == LedgerEntryType.MissionSettlement).AmountCents;
            }
            Assert.Equal(Run(11), Run(11));
        }

        [Fact]
        public void LineRemovedMidRouteEndsTheShiftWithNoFaultAndPaysServedStopsInFull()
        {
            CareerSim sim = BusSim(out _);
            ServeStop(sim, "stop-0");
            ServeStop(sim, "stop-1");
            Assert.Equal(2, sim.Svc.GetActiveMission().StopsServed);
            sim.World.Lines.Clear();
            sim.World.CityRevision++;
            sim.Building();
            sim.Drive(0.5, 0, 0);

            Assert.Null(sim.Svc.GetActiveMission());
            Assert.Contains(sim.Events, e => e.ToState == MissionState.Invalidated && e.Key == "DTC.Bus.Invalidated.LineRemoved");
            LedgerEntryView settle = sim.Svc.GetLedger().Single(e => e.Type == LedgerEntryType.MissionSettlement);
            Assert.EndsWith("-CANCELLED", settle.TransactionId);
            long passengers = settle.AmountCents - 2 * CareerBalance.BusPerStopCents;
            Assert.True(passengers >= 0 && passengers % CareerBalance.BusPerPassengerCents == 0);
            Assert.Equal(60, sim.Svc.GetProgress().Reputation);
        }

        [Fact]
        public void ModifiedLineIsReplannedAndPassengersMoveToTheNearestRemainingStop()
        {
            CareerSim sim = BusSim(out _);
            ServeStop(sim, "stop-0");
            ServeStop(sim, "stop-1");
            int onboard = sim.Svc.GetActiveMission().BusOnboard;
            sim.World.Lines.Clear();
            sim.World.AddLine("line-1", "Line 1", 1, "stop-0", "stop-1", "stop-2", "stop-4");
            sim.World.CityRevision++;
            sim.Drive(0.5, 0, 0);

            ActiveMissionView v = sim.Svc.GetActiveMission();
            Assert.NotNull(v);
            Assert.Equal(4, v.Legs.Count);
            Assert.DoesNotContain(v.Legs, l => l.PlaceId == "stop-3");
            Assert.Equal(onboard, v.BusOnboard);
            Assert.Contains(sim.Events, e => e.Key == "DTC.Bus.LineModified");
            ServeStop(sim, "stop-2");
            ServeStop(sim, "stop-4");
            Assert.Null(sim.Svc.GetActiveMission());
            Assert.EndsWith("-COMPLETED", sim.Svc.GetLedger().Single(e => e.Type == LedgerEntryType.MissionSettlement).TransactionId);
        }

        [Fact]
        public void DrivingPastAStopMarksItMissed()
        {
            CareerSim sim = BusSim(out _);
            ServeStop(sim, "stop-0");
            ServeStop(sim, "stop-1");
            sim.DriveTo(sim.World.Get("stop-3").Access, stop: false); // passes stop-2 without stopping
            ActiveMissionView v = sim.Svc.GetActiveMission();
            Assert.Equal(1, v.StopsMissed);
            Assert.Equal(LegOutcome.Missed, v.Legs.Single(l => l.PlaceId == "stop-2").Outcome);
            Assert.Contains(sim.Events, e => e.Key == "DTC.Bus.StopMissed");
            sim.Stop(CareerBalance.BusDwellCapSeconds + 1);
            ServeStop(sim, "stop-4");
            Assert.Null(sim.Svc.GetActiveMission());
        }

        [Fact]
        public void EndingTheShiftEarlyPaysHalfOfServedStops()
        {
            CareerSim sim = BusSim(out _);
            ServeStop(sim, "stop-0");
            sim.Svc.Cancel("end shift", sim.Now);
            LedgerEntryView settle = sim.Svc.GetLedger().Single(e => e.Type == LedgerEntryType.MissionSettlement);
            Assert.Equal(CareerBalance.BusPerStopCents / 2, settle.AmountCents);
            Assert.Equal(58, sim.Svc.GetProgress().Reputation);
        }

        [Fact]
        public void SaveAndLoadMidShiftRestoresPassengersAndProgress()
        {
            CareerSim sim = BusSim(out _);
            ServeStop(sim, "stop-0");
            ServeStop(sim, "stop-1");
            ActiveMissionView before = sim.Svc.GetActiveMission();
            sim.Reload();
            ActiveMissionView after = sim.Svc.GetActiveMission();
            Assert.Equal(before.BusOnboard, after.BusOnboard);
            Assert.Equal(before.StopsServed, after.StopsServed);
            Assert.Equal(before.BusWaitingAtNextStop, after.BusWaitingAtNextStop);
            ServeStop(sim, "stop-2");
            ServeStop(sim, "stop-3");
            ServeStop(sim, "stop-4");
            Assert.Null(sim.Svc.GetActiveMission());
            Assert.Equal(1, sim.Svc.GetLedger().Count(e => e.Type == LedgerEntryType.MissionSettlement));
        }

        [Fact]
        public void LinesWithTooFewStopsOrDisconnectedStopsAreNotOffered()
        {
            FakeCareerWorld w = TestCities.Bus();
            w.Add("x-0", PlaceKind.TransitStop, 5000, 0);
            w.Add("x-1", PlaceKind.TransitStop, 5600, 0);
            w.AddLine("line-short", "Short", 2, "x-0", "x-1");
            w.Unreachable.Add("stop-3");
            var sim = new CareerSim(w);
            sim.Svc.DebugSetReputation(60);
            Assert.Empty(sim.Svc.GetOffers(CareerKind.Bus, sim.Pos, sim.Now));

            var locked = new CareerSim(TestCities.Bus());
            Assert.Empty(locked.Svc.GetOffers(CareerKind.Bus, locked.Pos, locked.Now));
        }
    }
}
