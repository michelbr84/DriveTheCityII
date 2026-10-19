using System;
using System.Collections.Generic;
using System.Linq;
using DriveTheCityII.Core.Career;
using Xunit;

namespace DriveTheCityII.Core.Tests.Career
{
    /// <summary>
    /// Drives a <see cref="CareerService"/> like the mod would: a career-active clock, a player position, and
    /// per-frame snapshots. <see cref="AfterStep"/> lets a test reload the service between steps.
    /// </summary>
    internal sealed class CareerSim
    {
        public CareerService Svc;
        public readonly FakeCareerWorld World;
        public double Now;
        public MapPoint Pos;
        public string VehicleId;
        public double Heading;
        public readonly List<CareerEvent> Events = new List<CareerEvent>();

        /// <summary>Called after every DriveTo/Stop/Walk step (for reload-at-every-stage tests).</summary>
        public Action<CareerSim> AfterStep;

        public int Reloads;

        public CareerSim(FakeCareerWorld world, ulong seed = 42, CareerOptions options = null)
        {
            World = world;
            Svc = CareerService.CreateNew("test-profile", seed, world, 0, options);
        }

        public IReadOnlyList<CareerEvent> Tick(PlayerSnapshot s, double dt)
        {
            Now += dt;
            IReadOnlyList<CareerEvent> ev = Svc.Tick(s, Now);
            Events.AddRange(ev);
            return ev;
        }

        public PlayerSnapshot Snap(double speed, double driven, double impact = 0, double longAcc = 0, double latAcc = 0)
        {
            return new PlayerSnapshot
            {
                Mode = PlayerMode.Driving,
                VehicleId = VehicleId,
                Position = Pos,
                SpeedMps = speed,
                Stationary = speed < CareerBalance.ServiceSpeedMps,
                DistanceDrivenMeters = driven,
                ImpactSpeed = impact,
                HeadingDegrees = Heading,
                LongitudinalAccel = longAcc,
                LateralAccel = latAcc,
            };
        }

        public IReadOnlyList<CareerEvent> Drive(double dt, double speed, double driven, double impact = 0, double longAcc = 0, double latAcc = 0)
        {
            return Tick(Snap(speed, driven, impact, longAcc, latAcc), dt);
        }

        /// <summary>Drives in a straight line to the target and stops there (one stationary tick).</summary>
        public void DriveTo(MapPoint target, double speed = 12, double step = 50, bool stop = true)
        {
            MapPoint start = Pos;
            double total = MapPoint.Distance(start, target);
            int n = Math.Max(1, (int)Math.Ceiling(total / step));
            for (int i = 1; i <= n; i++)
            {
                double t = (double)i / n;
                Pos = new MapPoint(start.X + (target.X - start.X) * t, start.Y + (target.Y - start.Y) * t, start.Z + (target.Z - start.Z) * t);
                Drive(total / n / speed, speed, total / n);
            }
            if (stop)
            {
                Drive(0.5, 0, 0);
            }
            AfterStep?.Invoke(this);
        }

        public void DriveTo(PlaceInfo place, double speed = 12) => DriveTo(place.Access, speed);

        public void DriveTo(string placeId, double speed = 12) => DriveTo(World.Get(placeId).Access, speed);

        /// <summary>Stays stopped (speed 0) for the given time.</summary>
        public void Stop(double seconds, double dt = 1)
        {
            for (double t = 0; t < seconds - 1e-9; t += dt)
            {
                Drive(dt, 0, 0);
            }
            AfterStep?.Invoke(this);
        }

        public void Walk(double seconds, double dt = 1)
        {
            for (double t = 0; t < seconds - 1e-9; t += dt)
            {
                Tick(new PlayerSnapshot { Mode = PlayerMode.OnFoot, Position = Pos, Stationary = true }, dt);
            }
            AfterStep?.Invoke(this);
        }

        /// <summary>Building mode: the career clock is frozen, so time does not advance.</summary>
        public void Building(int ticks = 3)
        {
            for (int i = 0; i < ticks; i++)
            {
                Tick(PlayerSnapshot.Building(), 0);
            }
        }

        /// <summary>Simulates save and load: serialize to text, parse, restore. Asserts a clean load.</summary>
        public CareerLoadResult Reload()
        {
            string text = Svc.ToJsonText();
            CareerService restored = CareerService.FromJsonText(text, World, out CareerLoadResult result);
            Assert.True(result.IsOk, result.ToString());
            Assert.NotNull(restored);
            Assert.Equal(text, restored.ToJsonText());
            Assert.Equal(Svc.ClockSeconds, restored.ClockSeconds);
            Svc = restored;
            Reloads++;
            return result;
        }

        public string RentBasicAndBoard()
        {
            CareerResult r = Svc.RentBasic(Now);
            Assert.True(r.IsOk, r.ToString());
            Events.AddRange(r.Events);
            VehicleId = r.Id;
            Drive(0.5, 0, 0);
            return VehicleId;
        }

        public IReadOnlyList<CareerEvent> EventsOf(CareerEventKind kind) => Events.Where(e => e.Kind == kind).ToList();

        public int CountSettlements(string missionId)
        {
            return Svc.GetLedger().Count(e => e.Type == LedgerEntryType.MissionSettlement && e.SourceRef == missionId);
        }

        public long LedgerSum() => Svc.GetLedger().Sum(e => e.AmountCents);
    }

    /// <summary>Test cities.</summary>
    internal static class TestCities
    {
        /// <summary>Two places: exactly one taxi offer res-a -> off-b (planned 2.5 km).</summary>
        public static FakeCareerWorld Taxi()
        {
            var w = new FakeCareerWorld();
            w.Add("res-a", PlaceKind.Residential, 300, 0, "Elm Residence");
            w.Add("off-b", PlaceKind.Office, 2300, 0, "Tower B");
            w.Add("svc-fuel", PlaceKind.ServicePoint, -200, 0, "Fuel stand");
            return w;
        }

        public static FakeCareerWorld Delivery()
        {
            var w = new FakeCareerWorld();
            w.Add("wh-1", PlaceKind.Warehouse, 500, 0, "Warehouse 1");
            w.Add("r-1", PlaceKind.Residential, 2000, 0, "House 1");
            w.Add("r-2", PlaceKind.Residential, 2000, 1500, "House 2");
            w.Add("r-3", PlaceKind.Residential, 3500, 1500, "House 3");
            w.Add("r-4", PlaceKind.Residential, 3500, 0, "House 4");
            w.Add("r-5", PlaceKind.Residential, 5000, 700, "House 5");
            w.Add("r-6", PlaceKind.Residential, 5000, 2200, "House 6");
            w.Add("svc-fuel", PlaceKind.ServicePoint, -200, 0, "Fuel stand");
            return w;
        }

        public static FakeCareerWorld Freight()
        {
            var w = new FakeCareerWorld();
            w.Add("ind-1", PlaceKind.Industrial, 400, 0, "Mill");
            w.Add("com-f", PlaceKind.Commercial, 4400, 0, "Market F");
            w.Add("com-g", PlaceKind.Commercial, 4400, 600, "Market G");
            w.Add("ind-h", PlaceKind.Industrial, 5000, -500, "Plant H");
            return w;
        }

        public static FakeCareerWorld Bus()
        {
            var w = new FakeCareerWorld();
            for (int i = 0; i < 5; i++)
            {
                w.Add("stop-" + i, PlaceKind.TransitStop, i * 600, 1000, "Stop " + i);
            }
            w.Add("depot", PlaceKind.Depot, 0, 1200, "Bus depot");
            w.AddLine("line-1", "Line 1", 1, "stop-0", "stop-1", "stop-2", "stop-3", "stop-4");
            return w;
        }
    }
}
