using System;
using DriveTheCityII.Core.Geometry;
using DriveTheCityII.Core.Motion;
using DriveTheCityII.Core.World;
using Xunit;

namespace DriveTheCityII.Core.Tests
{
    public class GroundSolverTests
    {
        [Fact]
        public void BridgeAboveIsIgnoredWhenUnderneath()
        {
            var w = new FakeWorld().AddDeck(0, 0, 10, 50, 12f, SurfaceKind.Elevated, 7);
            var g = new GroundSolver().Solve(w, 0, 0, refY: 0f, stepUp: 0.4f, stepDown: 0.8f, maxWaterDepth: 0.5f);
            Assert.Equal(GroundStatus.Supported, g.Status);
            Assert.Equal(SurfaceKind.Terrain, g.Kind);
            Assert.Equal(0f, g.Height, 3);
        }

        [Fact]
        public void BridgeDeckIsKeptWhenOnTop()
        {
            var w = new FakeWorld().AddDeck(0, 0, 10, 50, 12f, SurfaceKind.Elevated, 7);
            var g = new GroundSolver().Solve(w, 0, 0, refY: 12f, stepUp: 0.4f, stepDown: 0.8f, maxWaterDepth: 0.5f);
            Assert.Equal(GroundStatus.Supported, g.Status);
            Assert.Equal(SurfaceKind.Elevated, g.Kind);
            Assert.Equal(7, g.SourceId);
        }

        [Fact]
        public void TunnelKeepsRoadWhileTerrainIsAbove()
        {
            var w = new FakeWorld { Terrain = (x, z) => 20f }.AddDeck(0, 0, 5, 100, 2f, SurfaceKind.Tunnel, 3);
            var g = new GroundSolver().Solve(w, 0, 10, refY: 2f, stepUp: 0.4f, stepDown: 0.8f, maxWaterDepth: 0.5f);
            Assert.Equal(GroundStatus.Supported, g.Status);
            Assert.Equal(SurfaceKind.Tunnel, g.Kind);
        }

        [Fact]
        public void RisingTerrainIsAWall()
        {
            var w = new FakeWorld { Terrain = (x, z) => x > 5 ? 10f : 0f };
            var g = new GroundSolver().Solve(w, 6, 0, refY: 0f, stepUp: 0.4f, stepDown: 0.8f, maxWaterDepth: 0.5f);
            Assert.Equal(GroundStatus.Blocked, g.Status);
        }

        [Fact]
        public void EdgeOfBridgeIsADrop()
        {
            var w = new FakeWorld().AddDeck(0, 0, 5, 50, 10f, SurfaceKind.Elevated, 1);
            var g = new GroundSolver().Solve(w, 8, 0, refY: 10f, stepUp: 0.4f, stepDown: 0.8f, maxWaterDepth: 0.5f);
            Assert.Equal(GroundStatus.Drop, g.Status);
            Assert.Equal(0f, g.Height, 3);
        }

        [Fact]
        public void DeepWaterIsRefused()
        {
            var w = new FakeWorld { Terrain = (x, z) => -3f };
            w.Water.Add((Obb2.FromYaw(new Vec2(0, 0), 0, 100, 100), 0f));
            var g = new GroundSolver().Solve(w, 0, 0, refY: 0f, stepUp: 0.4f, stepDown: 5f, maxWaterDepth: 0.5f);
            Assert.Equal(GroundStatus.DeepWater, g.Status);
        }

        [Fact]
        public void OutsideMapIsInvalid()
        {
            var w = new FakeWorld { MapHalfSize = 100 };
            var g = new GroundSolver().Solve(w, 150, 0, 0, 0.4f, 0.8f, 0.5f);
            Assert.Equal(GroundStatus.Invalid, g.Status);
        }
    }

    public class AvatarMotorTests
    {
        private static AvatarState PlaceAt(AvatarMotor m, IWorldProbe w, float x, float z, float y = 0f)
        {
            var s = new AvatarState();
            Assert.True(m.TryPlace(w, ref s, new Vec2(x, z), y + 1f));
            return s;
        }

        private static void Run(AvatarMotor m, IWorldProbe w, ref AvatarState s, AvatarInput input, float seconds)
        {
            const float dt = 1f / 60f;
            for (float t = 0; t < seconds; t += dt)
            {
                m.Step(w, ref s, input, dt);
            }
        }

        [Fact]
        public void WalksForwardAtWalkSpeed()
        {
            var w = new FakeWorld();
            var m = new AvatarMotor(new AvatarSpec());
            var s = PlaceAt(m, w, 0, 0);
            Run(m, w, ref s, new AvatarInput { Move = new Vec2(0, 1), CameraYaw = 0 }, 3f);
            Assert.InRange(s.Position.Z, 4.0f, 5.0f);
            Assert.InRange(s.HorizontalSpeed, 1.5f, 1.7f);
            Assert.True(s.Grounded);
        }

        [Fact]
        public void RunningIsFasterThanWalking()
        {
            var w = new FakeWorld();
            var m = new AvatarMotor(new AvatarSpec());
            var s = PlaceAt(m, w, 0, 0);
            Run(m, w, ref s, new AvatarInput { Move = new Vec2(0, 1), Run = true }, 3f);
            Assert.True(s.Position.Z > 11f);
        }

        [Fact]
        public void CameraYawRotatesMovement()
        {
            var w = new FakeWorld();
            var m = new AvatarMotor(new AvatarSpec());
            var s = PlaceAt(m, w, 0, 0);
            Run(m, w, ref s, new AvatarInput { Move = new Vec2(0, 1), CameraYaw = MathX.Pi / 2 }, 2f);
            Assert.True(s.Position.X > 2.5f);
            Assert.InRange(s.Position.Z, -0.05f, 0.05f);
        }

        [Fact]
        public void BuildingBlocksAndAvatarSlidesAlongIt()
        {
            var w = new FakeWorld().AddBuilding(0, 5, 60, 2, 0, 20, 99);
            var m = new AvatarMotor(new AvatarSpec());
            var s = PlaceAt(m, w, 0, 0);
            // Walk diagonally into the wall: Z must stop at the facade, X keeps moving.
            Run(m, w, ref s, new AvatarInput { Move = new Vec2(0.7f, 0.7f), Run = true }, 4f);
            Assert.True(s.Position.Z <= 3f - 0.3f + 0.01f, $"penetrated wall: {s.Position}");
            Assert.True(s.Position.X > 4f);
        }

        [Fact]
        public void ClimbsCurbButNotWall()
        {
            var w = new FakeWorld()
                .AddDeck(0, 10, 50, 5, 0.25f, SurfaceKind.Pedestrian, 1)   // curb + sidewalk
                .AddDeck(0, 30, 50, 5, 3.0f, SurfaceKind.Structure, 2);     // 3 m ledge
            var m = new AvatarMotor(new AvatarSpec());
            var s = PlaceAt(m, w, 0, 0);
            Run(m, w, ref s, new AvatarInput { Move = new Vec2(0, 1) }, 6f);
            Assert.Equal(SurfaceKind.Pedestrian, s.GroundKind);
            Assert.Equal(0.25f, s.Position.Y, 2);
            Run(m, w, ref s, new AvatarInput { Move = new Vec2(0, 1), Run = true }, 8f);
            // The 3 m ledge is a deck above the step height: underneath it the terrain still supports us, so the
            // avatar walks under it (like under a bridge) rather than climbing it.
            Assert.True(s.Position.Y < 1f);
        }

        [Fact]
        public void FallsOffBridgeEdgeAndLands()
        {
            var w = new FakeWorld().AddDeck(0, 0, 3, 50, 8f, SurfaceKind.Elevated, 5);
            var m = new AvatarMotor(new AvatarSpec());
            var s = new AvatarState();
            Assert.True(m.TryPlace(w, ref s, new Vec2(0, 0), 9f));
            Assert.Equal(8f, s.Position.Y, 2);
            bool landed = false;
            const float dt = 1f / 60f;
            for (int i = 0; i < 600; i++)
            {
                var r = m.Step(w, ref s, new AvatarInput { Move = new Vec2(1, 0), Run = true }, dt);
                landed |= r.Landed && s.Position.Y < 1f;
            }
            Assert.True(landed);
            Assert.Equal(0f, s.Position.Y, 2);
        }

        [Fact]
        public void NeverMovesMoreThanMaxStepPerFrame()
        {
            var w = new FakeWorld();
            var spec = new AvatarSpec { RunSpeed = 500f, Acceleration = 1e6f };
            var m = new AvatarMotor(spec);
            var s = PlaceAt(m, w, 0, 0);
            var before = s.Position;
            m.Step(w, ref s, new AvatarInput { Move = new Vec2(0, 1), Run = true }, 0.5f);
            Assert.True(Vec3.Distance(before, s.Position) <= spec.MaxStepPerFrame + 1e-3f);
        }
    }

    public class VehicleControllerTests
    {
        private static VehicleState Spawn(float x = 0, float z = 0, float y = 0, float yaw = 0) =>
            new VehicleState { Position = new Vec3(x, y, z), Yaw = yaw, Grounded = true };

        private static VehicleStepReport Drive(VehicleController c, IWorldProbe w, ref VehicleState s, VehicleInput i, float seconds)
        {
            var total = new VehicleStepReport();
            const float dt = 1f / 60f;
            for (float t = 0; t < seconds; t += dt)
            {
                var r = c.Step(w, ref s, i, dt);
                if (r.ImpactSpeed > total.ImpactSpeed)
                {
                    total.ImpactSpeed = r.ImpactSpeed;
                    total.ImpactKind = r.ImpactKind;
                }
                total.Landed |= r.Landed;
                total.BlockedByGround |= r.BlockedByGround;
            }
            return total;
        }

        [Fact]
        public void AcceleratesAndRespectsTopSpeed()
        {
            var w = new FakeWorld();
            var c = new VehicleController(new VehicleSpec());
            var s = Spawn();
            Drive(c, w, ref s, new VehicleInput { Throttle = 1 }, 5f);
            Assert.InRange(s.Speed, 10f, 20f);
            Drive(c, w, ref s, new VehicleInput { Throttle = 1 }, 120f);
            Assert.True(s.Speed <= c.Spec.MaxSpeed + 1e-3f);
            Assert.True(s.Position.Z > 500f);
        }

        [Fact]
        public void BrakesToStopWithoutReversing()
        {
            var w = new FakeWorld();
            var c = new VehicleController(new VehicleSpec());
            var s = Spawn();
            s.Speed = 20f;
            Drive(c, w, ref s, new VehicleInput { Brake = 1 }, 2.4f);
            Assert.InRange(s.Speed, 0f, 0.5f);
            Assert.Equal(Gear.Forward, s.Gear);
        }

        [Fact]
        public void HoldingBrakeAtStandstillEngagesReverse()
        {
            var w = new FakeWorld();
            var c = new VehicleController(new VehicleSpec());
            var s = Spawn();
            Drive(c, w, ref s, new VehicleInput { Brake = 1 }, 3f);
            Assert.Equal(Gear.Reverse, s.Gear);
            Assert.True(s.Speed < -2f);
            Assert.True(s.Speed >= -c.Spec.MaxReverseSpeed - 1e-3f);
            Assert.True(s.Position.Z < -3f);
            // Throttle while reversing brakes first, then engages forward.
            Drive(c, w, ref s, new VehicleInput { Throttle = 1 }, 3f);
            Assert.Equal(Gear.Forward, s.Gear);
            Assert.True(s.Speed > 0f);
        }

        [Fact]
        public void SteeringTurnsRightForPositiveInput()
        {
            var w = new FakeWorld();
            var c = new VehicleController(new VehicleSpec());
            var s = Spawn();
            s.Speed = 8f;
            Drive(c, w, ref s, new VehicleInput { Throttle = 0.3f, Steer = 1 }, 1.5f);
            Assert.True(s.Yaw > 0.3f);
            Assert.True(s.Position.X > 0.5f);
        }

        [Fact]
        public void SteeringLockShrinksWithSpeed()
        {
            var w = new FakeWorld();
            var spec = new VehicleSpec();
            var slow = Spawn();
            slow.Speed = 5f;
            var fast = Spawn();
            fast.Speed = 33f;
            var c1 = new VehicleController(spec);
            var c2 = new VehicleController(spec);
            Drive(c1, w, ref slow, new VehicleInput { Steer = 1, Throttle = 0.2f }, 1f);
            Drive(c2, w, ref fast, new VehicleInput { Steer = 1, Throttle = 1f }, 1f);
            Assert.True(Math.Abs(slow.SteerAngle) > Math.Abs(fast.SteerAngle) * 2f);
        }

        [Fact]
        public void HeadOnBuildingImpactStopsCarAndReportsImpact()
        {
            var w = new FakeWorld().AddBuilding(0, 30, 10, 3, 0, 15, 42);
            var c = new VehicleController(new VehicleSpec());
            var s = Spawn();
            s.Speed = 15f;
            var r = Drive(c, w, ref s, new VehicleInput { Throttle = 1 }, 3f);
            Assert.True(r.ImpactSpeed > 5f);
            Assert.Equal(ObstacleKind.Building, r.ImpactKind);
            Assert.True(s.Position.Z + c.Spec.Length / 2 <= 27.05f, $"car inside building: {s.Position}");
        }

        [Fact]
        public void ParkingBrakeHoldsCar()
        {
            var w = new FakeWorld();
            var c = new VehicleController(new VehicleSpec());
            var s = Spawn();
            s.ParkingBrake = true;
            Drive(c, w, ref s, new VehicleInput { Throttle = 1 }, 2f);
            Assert.True(Math.Abs(s.Speed) < 0.01f);
            Assert.True(Math.Abs(s.Position.Z) < 0.01f);
        }

        [Fact]
        public void FollowsRampOntoBridgeAndStaysOnDeck()
        {
            // A 10 % ramp from z=0..80 rising to 8 m in 0.25 m slices, then a deck at 8 m; terrain stays at 0 underneath.
            var w = new FakeWorld();
            for (int i = 0; i < 320; i++)
            {
                float zc = i * 0.25f + 0.125f;
                w.AddDeck(0, zc, 4, 0.125f, 8f * zc / 80f, SurfaceKind.Road, 1000 + i);
            }
            w.AddDeck(0, 180, 4, 100, 8f, SurfaceKind.Elevated, 200);
            var c = new VehicleController(new VehicleSpec());
            var s = Spawn(0, 1, 0.1f);
            Drive(c, w, ref s, new VehicleInput { Throttle = 0.6f }, 16f);
            Assert.True(s.Position.Z > 95f, $"z={s.Position.Z}");
            Assert.Equal(8f, s.Position.Y, 1);
            Assert.Equal(SurfaceKind.Elevated, s.GroundKind);
        }

        [Fact]
        public void TerrainWallBlocksCar()
        {
            var w = new FakeWorld { Terrain = (x, z) => z > 20 ? 6f : 0f };
            var c = new VehicleController(new VehicleSpec());
            var s = Spawn();
            s.Speed = 10f;
            var r = Drive(c, w, ref s, new VehicleInput { Throttle = 1 }, 4f);
            Assert.True(r.BlockedByGround);
            Assert.True(s.Position.Z < 20f);
            Assert.Equal(0f, s.Position.Y, 2);
        }

        [Fact]
        public void LargeFrameStallIsBounded()
        {
            var w = new FakeWorld();
            var c = new VehicleController(new VehicleSpec());
            var s = Spawn();
            s.Speed = 30f;
            c.Step(w, ref s, new VehicleInput { Throttle = 1 }, 5f);
            Assert.True(s.Position.Z < 3.5f);
        }
    }
}
