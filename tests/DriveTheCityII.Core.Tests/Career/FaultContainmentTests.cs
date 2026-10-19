using System;
using System.Collections.Generic;
using DriveTheCityII.Core.Career;
using Xunit;

namespace DriveTheCityII.Core.Tests.Career
{
    public class FaultContainmentTests
    {
        /// <summary>A world port that fails on demand (exceptions and garbage answers).</summary>
        private sealed class FlakyWorld : ICareerWorld
        {
            public readonly FakeCareerWorld Inner;
            public bool Throw;

            public FlakyWorld(FakeCareerWorld inner)
            {
                Inner = inner;
            }

            public long CityRevision => Throw ? throw new InvalidOperationException("revision") : Inner.CityRevision;

            public IReadOnlyList<PlaceInfo> QueryPlaces(PlaceQuery query) => Throw ? throw new InvalidOperationException("query") : Inner.QueryPlaces(query);

            public PlaceInfo Describe(string placeId) => Throw ? throw new InvalidOperationException("describe") : Inner.Describe(placeId);

            public RouteEstimate Estimate(MapPoint from, string placeId, VehicleClass cls)
            {
                if (Throw)
                {
                    throw new InvalidOperationException("estimate");
                }
                RouteEstimate e = Inner.Estimate(from, placeId, cls);
                return e;
            }

            public IReadOnlyList<TransitLineInfo> BusLines() => Throw ? throw new InvalidOperationException("lines") : Inner.BusLines();
        }

        [Fact]
        public void PortExceptionsAreContainedAndNeverTreatedAsReachable()
        {
            var flaky = new FlakyWorld(TestCities.Taxi());
            CareerService svc = CareerService.CreateNew("p", 1, flaky);
            flaky.Throw = true;
            Assert.Empty(svc.GetOffers(CareerKind.Taxi, default, 1));
            var snap = PlayerSnapshot.Driving(null, default, 0, 0);
            svc.Tick(snap, 2);

            flaky.Throw = false;
            string car = svc.RentBasic(3).Id;
            snap = PlayerSnapshot.Driving(car, default, 0, 0);
            svc.Tick(snap, 4);
            var offers = svc.GetOffers(CareerKind.Taxi, default, 5);
            Assert.Single(offers);
            Assert.True(svc.Accept(offers[0].Id, car, 6).IsOk);
            svc.Tick(snap, 7);

            // During an active mission, a failing port is an "unknown" answer: the mission is not cancelled.
            // Ticks span three watchdog checks but stay within the passenger's patience (at least 240 s).
            flaky.Throw = true;
            var events = new List<CareerEvent>();
            for (int i = 0; i < 10; i++)
            {
                events.AddRange(svc.Tick(snap, 8 + i * 22));
            }
            Assert.NotNull(svc.GetActiveMission());
            Assert.DoesNotContain(events, e => e.ToState == MissionState.Invalidated);
            Assert.Equal(CareerBalance.InitialBalanceCents, svc.WalletBalanceCents);
        }

        [Fact]
        public void GarbagePortAnswersAreIgnored()
        {
            FakeCareerWorld w = TestCities.Taxi();
            w.Add("nan", PlaceKind.Residential, double.NaN, 0);
            w.Add(string.Empty, PlaceKind.Residential, 100, 0);
            CareerService svc = CareerService.CreateNew("p", 1, w);
            var offers = svc.GetOffers(CareerKind.Taxi, default, 1);
            Assert.All(offers, o => Assert.All(o.Legs, l => Assert.True(l.Place.Access.IsFinite && l.Place.Id.Length > 0)));
        }

        [Fact]
        public void NonFiniteSnapshotValuesAreIgnored()
        {
            var sim = new CareerSim(TestCities.Taxi());
            string car = sim.RentBasicAndBoard();
            sim.Drive(1, double.NaN, double.PositiveInfinity, impact: double.NaN);
            sim.Drive(1, 5, 5000); // teleport-sized jump is not charged
            Assert.Equal(0, sim.Svc.GetVehicle(car).OdometerKm, 6);
            Assert.Empty(sim.Svc.Tick(PlayerSnapshot.OnFoot(default), double.NaN));
        }
    }
}
