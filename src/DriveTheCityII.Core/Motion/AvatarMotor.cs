using System;
using System.Collections.Generic;
using DriveTheCityII.Core.Geometry;
using DriveTheCityII.Core.World;

namespace DriveTheCityII.Core.Motion
{
    public sealed class AvatarSpec
    {
        public float WalkSpeed = 1.6f;
        public float RunSpeed = 4.6f;
        public float Acceleration = 12f;
        public float Deceleration = 16f;
        public float AirControl = 0.15f;
        public float TurnSharpness = 12f;
        public float Radius = 0.3f;
        public float Height = 1.8f;
        public float StepUp = 0.45f;
        public float StepDown = 0.6f;
        public float Gravity = 9.81f;
        public float MaxFallSpeed = 40f;
        public float MaxWaterDepth = 0.6f;

        /// <summary>Upper bound of horizontal travel per frame (AP-06), metres.</summary>
        public float MaxStepPerFrame = 1.0f;
    }

    public struct AvatarState
    {
        public Vec3 Position;
        public float Yaw;
        public Vec2 Velocity;
        public float VerticalSpeed;
        public bool Grounded;
        public SurfaceKind GroundKind;
        public long GroundSource;
        public float AirTime;

        public float HorizontalSpeed => Velocity.Length;
    }

    public struct AvatarInput
    {
        /// <summary>Movement in camera space: X = right, Z = forward. Magnitude is clamped to 1.</summary>
        public Vec2 Move;

        public bool Run;

        /// <summary>Yaw of the camera, so that "forward" means away from the camera.</summary>
        public float CameraYaw;
    }

    public struct AvatarStepReport
    {
        public bool HitObstacle;
        public ObstacleKind ObstacleKind;
        public long ObstacleSource;
        public bool BlockedByGround;
        public GroundStatus LastGroundStatus;
        public bool Landed;
        public float FallDistance;
    }

    /// <summary>
    /// Walking and running controller for the visible avatar. It turns input into motion, slides along obstacles,
    /// climbs curbs and stairs within the step allowance, and falls when the ground drops away (SPEC-PLAYER, SPEC-CONTACT).
    /// </summary>
    public sealed class AvatarMotor
    {
        private readonly GroundSolver m_Ground = new GroundSolver();
        private readonly List<Obstacle> m_Obstacles = new List<Obstacle>(32);

        public AvatarSpec Spec { get; }

        public AvatarMotor(AvatarSpec spec)
        {
            Spec = spec ?? new AvatarSpec();
        }

        /// <summary>Places the avatar on the supporting ground at (x, z), searching from <paramref name="refY"/>.</summary>
        public bool TryPlace(IWorldProbe probe, ref AvatarState state, Vec2 xz, float refY)
        {
            GroundResult g = m_Ground.Solve(probe, xz.X, xz.Z, refY, Spec.StepUp, 1000f, Spec.MaxWaterDepth);
            if (g.Status != GroundStatus.Supported)
            {
                return false;
            }
            if (IsPenetrating(probe, xz, g.Height))
            {
                return false;
            }
            state.Position = new Vec3(xz, g.Height);
            state.Velocity = Vec2.Zero;
            state.VerticalSpeed = 0f;
            state.Grounded = true;
            state.GroundKind = g.Kind;
            state.GroundSource = g.SourceId;
            state.AirTime = 0f;
            return true;
        }

        /// <summary>True when a standing avatar at (xz, y) would overlap a solid obstacle.</summary>
        public bool IsPenetrating(IWorldProbe probe, Vec2 xz, float y)
        {
            m_Obstacles.Clear();
            probe.QueryObstacles(xz, Spec.Radius + 0.5f, m_Obstacles);
            for (int i = 0; i < m_Obstacles.Count; i++)
            {
                Obstacle o = m_Obstacles[i];
                if (!Overlap.RangesOverlap(y + Spec.StepUp, y + Spec.Height, o.YMin, o.YMax))
                {
                    continue;
                }
                if (Overlap.CircleVsBox(xz, Spec.Radius, o.Footprint).Overlaps)
                {
                    return true;
                }
            }
            return false;
        }

        public AvatarStepReport Step(IWorldProbe probe, ref AvatarState s, AvatarInput input, float dt)
        {
            var report = new AvatarStepReport();
            if (dt <= 0f)
            {
                return report;
            }
            dt = Math.Min(dt, 0.1f);

            // 1. Desired horizontal velocity, relative to the camera.
            Vec2 move = input.Move;
            float mag = move.Length;
            if (mag > 1f)
            {
                move /= mag;
                mag = 1f;
            }
            Vec2 camFwd = Vec2.FromYaw(input.CameraYaw);
            Vec2 camRight = camFwd.RightPerp;
            Vec2 desiredDir = camRight * move.X + camFwd * move.Z;
            float topSpeed = input.Run ? Spec.RunSpeed : Spec.WalkSpeed;
            Vec2 targetVel = desiredDir * topSpeed;

            float rate = targetVel.LengthSq >= s.Velocity.LengthSq ? Spec.Acceleration : Spec.Deceleration;
            if (!s.Grounded)
            {
                rate *= Spec.AirControl;
            }
            Vec2 dv = targetVel - s.Velocity;
            float maxDv = rate * dt;
            if (dv.Length > maxDv)
            {
                dv = dv.Normalized * maxDv;
            }
            s.Velocity += dv;

            // 2. Face the direction of travel.
            if (mag > 0.05f)
            {
                float targetYaw = desiredDir.ToYaw();
                float delta = MathX.WrapAngle(targetYaw - s.Yaw);
                s.Yaw = MathX.WrapAngle(s.Yaw + delta * MathX.SmoothFactor(Spec.TurnSharpness, dt));
            }

            // 3. Horizontal move with obstacle sliding and ground validation.
            Vec2 disp = s.Velocity * dt;
            if (disp.Length > Spec.MaxStepPerFrame)
            {
                disp = disp.Normalized * Spec.MaxStepPerFrame;
            }

            Vec2 start = s.Position.XZ;
            Vec2 target = start + disp;
            target = ResolveObstacles(probe, target, s.Position.Y, ref report);

            GroundResult g = m_Ground.Solve(probe, target.X, target.Z, s.Position.Y, Spec.StepUp, Spec.StepDown, Spec.MaxWaterDepth);
            if (!Acceptable(g))
            {
                // Try sliding along each axis separately before giving up.
                report.BlockedByGround = true;
                bool moved = false;
                Vec2[] tries = { new Vec2(target.X, start.Z), new Vec2(start.X, target.Z) };
                for (int i = 0; i < tries.Length && !moved; i++)
                {
                    Vec2 t = ResolveObstacles(probe, tries[i], s.Position.Y, ref report);
                    GroundResult g2 = m_Ground.Solve(probe, t.X, t.Z, s.Position.Y, Spec.StepUp, Spec.StepDown, Spec.MaxWaterDepth);
                    if (Acceptable(g2))
                    {
                        target = t;
                        g = g2;
                        moved = true;
                    }
                }
                if (!moved)
                {
                    target = start;
                    g = m_Ground.Solve(probe, start.X, start.Z, s.Position.Y, Spec.StepUp, Spec.StepDown, Spec.MaxWaterDepth);
                    s.Velocity = Vec2.Zero;
                }
            }

            // Velocity follows the actual displacement so that sliding does not accumulate speed into walls.
            Vec2 actual = target - start;
            if (dt > 0f)
            {
                Vec2 actualVel = actual / dt;
                if (actualVel.LengthSq < s.Velocity.LengthSq)
                {
                    s.Velocity = actualVel;
                }
            }

            report.LastGroundStatus = g.Status;
            float y = s.Position.Y;

            // 4. Vertical: stick to ground within the step allowance, otherwise fall.
            if (g.Status == GroundStatus.Supported && s.Grounded)
            {
                y = g.Height;
                s.VerticalSpeed = 0f;
                s.GroundKind = g.Kind;
                s.GroundSource = g.SourceId;
                s.AirTime = 0f;
            }
            else if (g.Status == GroundStatus.Supported || g.Status == GroundStatus.Drop)
            {
                s.Grounded = false;
                s.AirTime += dt;
                s.VerticalSpeed = Math.Max(s.VerticalSpeed - Spec.Gravity * dt, -Spec.MaxFallSpeed);
                float newY = y + s.VerticalSpeed * dt;
                if (newY <= g.Height)
                {
                    report.Landed = true;
                    report.FallDistance = Math.Max(0f, -s.VerticalSpeed * s.AirTime * 0.5f);
                    newY = g.Height;
                    s.VerticalSpeed = 0f;
                    s.Grounded = true;
                    s.AirTime = 0f;
                    s.GroundKind = g.Kind;
                    s.GroundSource = g.SourceId;
                }
                y = newY;
            }

            s.Position = new Vec3(target, y);
            return report;
        }

        private static bool Acceptable(GroundResult g) => g.Status == GroundStatus.Supported || g.Status == GroundStatus.Drop;

        private Vec2 ResolveObstacles(IWorldProbe probe, Vec2 p, float y, ref AvatarStepReport report)
        {
            m_Obstacles.Clear();
            probe.QueryObstacles(p, Spec.Radius + 1.0f, m_Obstacles);
            if (m_Obstacles.Count == 0)
            {
                return p;
            }
            for (int iter = 0; iter < 4; iter++)
            {
                bool any = false;
                for (int i = 0; i < m_Obstacles.Count; i++)
                {
                    Obstacle o = m_Obstacles[i];
                    // Obstacles entirely below the step height (curbs, low props) are stepped over.
                    if (!Overlap.RangesOverlap(y + Spec.StepUp, y + Spec.Height, o.YMin, o.YMax))
                    {
                        continue;
                    }
                    Penetration pen = Overlap.CircleVsBox(p, Spec.Radius, o.Footprint);
                    if (!pen.Overlaps)
                    {
                        continue;
                    }
                    p += pen.Normal * (pen.Depth + 1e-3f);
                    any = true;
                    report.HitObstacle = true;
                    report.ObstacleKind = o.Kind;
                    report.ObstacleSource = o.SourceId;
                }
                if (!any)
                {
                    break;
                }
            }
            return p;
        }
    }
}
