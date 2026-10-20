using System.Collections.Generic;
using DriveTheCityII.Core.Geometry;
using DriveTheCityII.Core.World;
using Xunit;

namespace DriveTheCityII.Core.Tests
{
    public class TurnPenaltiesTests
    {
        [Theory]
        [InlineData(4.5f, 30f, 0f)]   // car
        [InlineData(8.5f, 120f, 0f)]  // rigid truck
        [InlineData(12.7f, 400f, 400f)] // line bus
        public void PenaltiesByLength(float length, float wrongWay, float uTurn)
        {
            Assert.Equal(wrongWay, TurnPenalties.WrongWayStart(length));
            Assert.Equal(uTurn, TurnPenalties.UTurnLane(length));
        }

        [Fact]
        public void ABusPrefersRoundTheBlockToTurningInTheStreet()
        {
            // ISSUE-034: a four-minute drive round the block must cost less than turning a 12.7 m bus in the street.
            Assert.True(TurnPenalties.WrongWayStart(12.7f) > 240f);
            // ISSUE-026: a truck is still not sent on a detour of more than a few minutes to avoid turning.
            Assert.True(TurnPenalties.WrongWayStart(8.5f) <= 120f);
        }
    }

    public class WaitingBehindTests
    {
        // Our car: 4.6 m x 2 m at the origin, facing +Z.
        private static readonly Obb2 Self = Obb2.FromYaw(Vec2.Zero, 0f, 1f, 2.3f);

        private static Obstacle Car(float x, float z, float yawDeg = 0f, ObstacleKind kind = ObstacleKind.Vehicle) => new Obstacle
        {
            Footprint = Obb2.FromYaw(new Vec2(x, z), yawDeg * MathX.Deg2Rad, 0.9f, 2.2f),
            Kind = kind,
            SourceId = (long)(x * 1000 + z),
        };

        [Fact]
        public void ReportsTheCarStoppedJustAhead()
        {
            var obs = new List<Obstacle> { Car(0f, 6f) };
            Assert.Equal(0, WaitingBehind.Pick(Self, 0f, 6f, obs));
        }

        [Fact]
        public void NothingWhileMovingOrJustStopped()
        {
            var obs = new List<Obstacle> { Car(0f, 6f) };
            Assert.Equal(-1, WaitingBehind.Pick(Self, 3f, 30f, obs));
            Assert.Equal(-1, WaitingBehind.Pick(Self, 0f, 2f, obs));
        }

        [Fact]
        public void IgnoresTheNextLaneBehindAndFarAhead()
        {
            var obs = new List<Obstacle>
            {
                Car(3.5f, 6f),   // the next lane
                Car(0f, -6f),    // behind us
                Car(0f, 15f),    // 10.5 m gap
            };
            Assert.Equal(-1, WaitingBehind.Pick(Self, 0f, 10f, obs));
        }

        [Fact]
        public void PicksTheNearestVehicleAndSkipsParkedOnes()
        {
            var obs = new List<Obstacle>
            {
                Car(0f, 9f),
                Car(0.2f, 5.2f, 0f, ObstacleKind.StaticObject), // parked: an obstacle, not traffic to wait for
                Car(-0.3f, 6.5f),
            };
            Assert.Equal(2, WaitingBehind.Pick(Self, 0.2f, 10f, obs));
        }

        [Fact]
        public void ReportsACarStoppedAcrossTheFrontOfABus()
        {
            // Observed in game on 2026-10-20: a car stopped across the front half of a 12.7 m bus, its centre 3 m ahead of
            // the bus's centre, the bodies touching; each waited for the other.
            Obb2 bus = Obb2.FromYaw(Vec2.Zero, 0f, 1.35f, 6.35f);
            var obs = new List<Obstacle> { Car(1.8f, 2.7f, 75f) };
            Assert.Equal(0, WaitingBehind.Pick(bus, 0f, 40f, obs));
        }

        [Fact]
        public void ACarBesideTheBusIsNotWhatItWaitsFor()
        {
            // The same car parallel in the next lane, level with the bus's front: not in its path.
            Obb2 bus = Obb2.FromYaw(Vec2.Zero, 0f, 1.35f, 6.35f);
            var obs = new List<Obstacle> { Car(3.4f, 2.7f) };
            Assert.Equal(-1, WaitingBehind.Pick(bus, 0f, 40f, obs));
        }

        [Fact]
        public void ReportsACarCrossingInFront()
        {
            // At a junction: a car stopped across our path, its side 1 m from our front.
            var obs = new List<Obstacle> { Car(1.5f, 4.2f, 90f) };
            Assert.Equal(0, WaitingBehind.Pick(Self, 0f, 8f, obs));
        }
    }
}
