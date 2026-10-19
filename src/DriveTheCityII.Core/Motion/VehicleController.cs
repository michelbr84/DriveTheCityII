using System;
using System.Collections.Generic;
using DriveTheCityII.Core.Geometry;
using DriveTheCityII.Core.World;

namespace DriveTheCityII.Core.Motion
{
    /// <summary>Handling parameters of one vehicle class (SPEC-DRIVING). Units: metres, seconds, radians.</summary>
    public sealed class VehicleSpec
    {
        public string Name = "Car";
        public float Length = 4.4f;
        public float Width = 1.8f;
        public float Height = 1.5f;
        public float WheelBase = 2.7f;
        public float MaxSpeed = 38f;
        public float MaxReverseSpeed = 7f;
        public float Acceleration = 3.6f;
        public float BrakeDeceleration = 8.5f;
        public float HandbrakeDeceleration = 6f;
        public float CoastDeceleration = 0.5f;
        public float DragCoefficient = 0.0011f;
        public float MaxSteerAngle = 34f * MathX.Deg2Rad;

        /// <summary>Fraction of the steering lock left at <see cref="SteerReferenceSpeed"/> and above.</summary>
        public float HighSpeedSteerFactor = 0.22f;

        public float SteerReferenceSpeed = 30f;
        public float SteerRate = 2.2f;
        public float SteerReturnRate = 3.0f;
        public float StepUp = 0.32f;
        public float StepDown = 0.9f;
        public float Gravity = 9.81f;
        public float GradeFactor = 0.6f;
        public float MaxWaterDepth = 0.45f;
        public float Restitution = 0.15f;

        /// <summary>Speed below which the vehicle counts as stationary (SPEC-CONTROL TR-03, TR-06).</summary>
        public float StationaryThreshold = 0.3f;

        /// <summary>Seconds the brake must be held at a standstill before reverse engages.</summary>
        public float ReverseEngageDelay = 0.35f;

        public VehicleSpec Clone() => (VehicleSpec)MemberwiseClone();
    }

    public enum Gear
    {
        Forward = 0,
        Reverse = 1,
    }

    public struct VehicleState
    {
        /// <summary>Ground-level center of the footprint.</summary>
        public Vec3 Position;

        public float Yaw;

        /// <summary>Signed speed along the vehicle's forward axis (m/s, negative when reversing).</summary>
        public float Speed;

        public float SteerAngle;
        public float VerticalSpeed;
        public bool Grounded;
        public float Pitch;
        public float Roll;
        public Gear Gear;
        public float ReverseHold;
        public bool ParkingBrake;
        public SurfaceKind GroundKind;
        public long GroundSource;
        public float Odometer;

        public Vec2 Forward => Vec2.FromYaw(Yaw);
        public Vec2 VelocityXZ => Vec2.FromYaw(Yaw) * Speed;
    }

    public struct VehicleInput
    {
        public float Throttle;
        public float Brake;
        public float Steer;
        public bool Handbrake;

        public static VehicleInput Idle => default;
    }

    public struct VehicleStepReport
    {
        public float ImpactSpeed;
        public ObstacleKind ImpactKind;
        public long ImpactSource;
        public bool BlockedByGround;
        public bool Airborne;
        public bool Landed;
        public GroundStatus WorstGround;
        public float Distance;
    }

    /// <summary>
    /// Arcade vehicle model: kinematic bicycle steering with speed-dependent lock, a longitudinal model with
    /// throttle, brakes, reverse, parking brake, drag and grade, ground following on four wheel contacts, and
    /// box-based collision response against solid obstacles. The controller is the only writer of the controlled
    /// vehicle's pose (AP-02); the CityBridge applies the resulting pose to the game entity.
    /// </summary>
    public sealed class VehicleController
    {
        private readonly GroundSolver m_Ground = new GroundSolver();
        private readonly List<Obstacle> m_Obstacles = new List<Obstacle>(64);
        private readonly GroundResult[] m_Wheels = new GroundResult[4];

        /// <summary>Optional filter: obstacles with this source id are ignored (the controlled vehicle itself).</summary>
        public long IgnoreSourceId;

        public VehicleSpec Spec { get; private set; }

        public VehicleController(VehicleSpec spec)
        {
            Spec = spec ?? new VehicleSpec();
        }

        public void SetSpec(VehicleSpec spec)
        {
            Spec = spec ?? Spec;
        }

        public static bool IsStationary(in VehicleState s, VehicleSpec spec) => Math.Abs(s.Speed) < spec.StationaryThreshold;

        public VehicleStepReport Step(IWorldProbe probe, ref VehicleState s, VehicleInput input, float dt)
        {
            var report = new VehicleStepReport { WorstGround = GroundStatus.Supported };
            if (dt <= 0f)
            {
                return report;
            }
            // Bounded recovery after frame stalls: never integrate more than 0.1 s per frame.
            dt = Math.Min(dt, 0.1f);

            // Sub-step so that no step travels more than half a metre (prevents tunnelling through thin obstacles).
            int steps = Math.Max(1, Math.Min(8, (int)Math.Ceiling(Math.Abs(s.Speed) * dt / 0.5f)));
            float h = dt / steps;
            for (int i = 0; i < steps; i++)
            {
                SubStep(probe, ref s, input, h, ref report);
            }
            return report;
        }

        private void SubStep(IWorldProbe probe, ref VehicleState s, VehicleInput input, float dt, ref VehicleStepReport report)
        {
            VehicleSpec spec = Spec;
            float throttle = MathX.Clamp01(input.Throttle);
            float brake = MathX.Clamp01(input.Brake);
            float steerIn = MathX.Clamp(input.Steer, -1f, 1f);

            // --- Gear selection: brake held at a standstill engages reverse; throttle at a standstill engages forward.
            bool stationary = Math.Abs(s.Speed) < spec.StationaryThreshold;
            if (stationary && brake > 0.1f && throttle < 0.1f && s.Gear == Gear.Forward)
            {
                s.ReverseHold += dt;
                if (s.ReverseHold >= spec.ReverseEngageDelay)
                {
                    s.Gear = Gear.Reverse;
                }
            }
            else
            {
                s.ReverseHold = 0f;
            }
            if (stationary && throttle > 0.1f && brake < 0.1f && s.Gear == Gear.Reverse)
            {
                s.Gear = Gear.Forward;
            }

            float drive;
            float braking;
            if (s.Gear == Gear.Forward)
            {
                drive = throttle;
                braking = brake;
            }
            else
            {
                drive = -brake;
                braking = throttle;
            }
            if (input.Handbrake || s.ParkingBrake)
            {
                drive = 0f;
            }

            // --- Longitudinal forces.
            float v = s.Speed;
            float accel = 0f;
            if (drive > 0f)
            {
                float ratio = v > 0f ? v / spec.MaxSpeed : 0f;
                accel += spec.Acceleration * drive * Math.Max(0f, 1f - ratio * ratio);
            }
            else if (drive < 0f)
            {
                float ratio = v < 0f ? -v / spec.MaxReverseSpeed : 0f;
                accel += spec.Acceleration * 0.7f * drive * Math.Max(0f, 1f - ratio * ratio);
            }

            // Grade: climbing slows, descending speeds up.
            accel -= spec.Gravity * (float)Math.Sin(s.Pitch) * spec.GradeFactor;

            float resist = spec.CoastDeceleration + spec.DragCoefficient * v * v;
            float brakeDecel = spec.BrakeDeceleration * braking;
            if (input.Handbrake || s.ParkingBrake)
            {
                brakeDecel = Math.Max(brakeDecel, spec.HandbrakeDeceleration);
            }

            float vNew = v + accel * dt;
            // Resistance and brakes always oppose motion and never reverse it on their own.
            float oppose = (resist + brakeDecel) * dt;
            if (vNew > 0f)
            {
                vNew = Math.Max(0f, vNew - oppose);
            }
            else if (vNew < 0f)
            {
                vNew = Math.Min(0f, vNew + oppose);
            }
            if (!s.Grounded)
            {
                // No traction in the air: keep momentum, only drag.
                vNew = v - Math.Sign(v) * spec.DragCoefficient * v * v * dt;
            }
            vNew = MathX.Clamp(vNew, -spec.MaxReverseSpeed, spec.MaxSpeed);
            s.Speed = vNew;

            // --- Steering with speed-dependent lock.
            float speedRatio = MathX.Clamp01(Math.Abs(vNew) / spec.SteerReferenceSpeed);
            float lockAngle = spec.MaxSteerAngle * MathX.Lerp(1f, spec.HighSpeedSteerFactor, speedRatio);
            float targetSteer = steerIn * lockAngle;
            float rate = Math.Abs(targetSteer) < Math.Abs(s.SteerAngle) ? spec.SteerReturnRate : spec.SteerRate;
            s.SteerAngle = MathX.MoveTowards(s.SteerAngle, targetSteer, rate * dt);

            // --- Kinematic bicycle integration.
            float yawRate = s.Grounded ? vNew / spec.WheelBase * (float)Math.Tan(s.SteerAngle) : 0f;
            float newYaw = MathX.WrapAngle(s.Yaw + yawRate * dt);
            Vec2 fwd = Vec2.FromYaw(newYaw);
            Vec2 start = s.Position.XZ;
            Vec2 target = start + fwd * (vNew * dt);

            // --- Obstacles.
            target = ResolveObstacles(probe, ref s, target, newYaw, ref report);

            // --- Ground under the four wheel contacts.
            if (!SampleWheels(probe, target, newYaw, s.Position.Y, s.Pitch, s.Roll, out float groundY, out float pitch, out float roll, out GroundStatus worst, out SurfaceKind kind, out long source))
            {
                // A wheel hit a wall of terrain or an invalid area: stop like an impact.
                report.BlockedByGround = true;
                report.WorstGround = worst;
                report.ImpactSpeed = Math.Max(report.ImpactSpeed, Math.Abs(s.Speed));
                report.ImpactKind = ObstacleKind.MapEdge;
                s.Speed = -s.Speed * spec.Restitution;
                return;
            }
            report.WorstGround = worst;

            float y = s.Position.Y;
            bool supported = worst == GroundStatus.Supported;
            if (supported && s.Grounded)
            {
                y = groundY;
                s.VerticalSpeed = 0f;
            }
            else
            {
                // Airborne or dropping: fall towards the support.
                s.Grounded = false;
                report.Airborne = true;
                s.VerticalSpeed -= spec.Gravity * dt;
                float ny = y + s.VerticalSpeed * dt;
                if (ny <= groundY)
                {
                    ny = groundY;
                    s.VerticalSpeed = 0f;
                    s.Grounded = true;
                    report.Landed = true;
                }
                y = ny;
            }

            float moved = Vec2.Distance(start, target);
            s.Odometer += moved;
            report.Distance += moved;
            s.Yaw = newYaw;
            s.Pitch = s.Grounded ? pitch : s.Pitch;
            s.Roll = s.Grounded ? roll : s.Roll;
            s.GroundKind = kind;
            s.GroundSource = source;
            s.Position = new Vec3(target, y);
        }

        private Vec2 ResolveObstacles(IWorldProbe probe, ref VehicleState s, Vec2 target, float yaw, ref VehicleStepReport report)
        {
            VehicleSpec spec = Spec;
            float hw = spec.Width * 0.5f;
            float hl = spec.Length * 0.5f;
            m_Obstacles.Clear();
            probe.QueryObstacles(target, (float)Math.Sqrt(hw * hw + hl * hl) + 2f, m_Obstacles);
            if (m_Obstacles.Count == 0)
            {
                return target;
            }
            float yLow = s.Position.Y + spec.StepUp;
            float yHigh = s.Position.Y + spec.Height;
            for (int iter = 0; iter < 4; iter++)
            {
                bool any = false;
                var box = Obb2.FromYaw(target, yaw, hw, hl);
                for (int i = 0; i < m_Obstacles.Count; i++)
                {
                    Obstacle o = m_Obstacles[i];
                    if (IgnoreSourceId != 0 && o.SourceId == IgnoreSourceId)
                    {
                        continue;
                    }
                    if (!Overlap.RangesOverlap(yLow, yHigh, o.YMin, o.YMax))
                    {
                        continue;
                    }
                    Penetration pen = Overlap.BoxVsBox(box, o.Footprint);
                    if (!pen.Overlaps)
                    {
                        continue;
                    }
                    target += pen.Normal * (pen.Depth + 1e-3f);
                    box = Obb2.FromYaw(target, yaw, hw, hl);
                    any = true;

                    // Velocity response: remove the approaching component (with a little bounce), keep the rest
                    // projected onto the heading because the car cannot slide sideways in this model.
                    Vec2 fwd = Vec2.FromYaw(yaw);
                    Vec2 vel = fwd * s.Speed - o.Velocity;
                    float approach = -Vec2.Dot(vel, pen.Normal);
                    if (approach > 0f)
                    {
                        Vec2 responded = vel + pen.Normal * (approach * (1f + spec.Restitution));
                        float newSpeed = Vec2.Dot(responded + o.Velocity, fwd);
                        if (approach > report.ImpactSpeed)
                        {
                            report.ImpactSpeed = approach;
                            report.ImpactKind = o.Kind;
                            report.ImpactSource = o.SourceId;
                        }
                        s.Speed = newSpeed;
                    }
                }
                if (!any)
                {
                    break;
                }
            }
            return target;
        }

        private bool SampleWheels(IWorldProbe probe, Vec2 center, float yaw, float refY, float pitch0, float roll0,
            out float groundY, out float pitch, out float roll, out GroundStatus worst, out SurfaceKind kind, out long source)
        {
            VehicleSpec spec = Spec;
            Vec2 fwd = Vec2.FromYaw(yaw);
            Vec2 right = fwd.RightPerp;
            float halfTrack = spec.Width * 0.4f;
            float halfBase = spec.WheelBase * 0.5f;
            Vec2[] pts =
            {
                center + fwd * halfBase - right * halfTrack,
                center + fwd * halfBase + right * halfTrack,
                center - fwd * halfBase - right * halfTrack,
                center - fwd * halfBase + right * halfTrack,
            };
            worst = GroundStatus.Supported;
            kind = SurfaceKind.Terrain;
            source = 0;
            float maxY = float.MinValue;
            // Predict each wheel's height from the current pitch and roll so that ramps keep their deck.
            float tp = (float)Math.Tan(pitch0);
            float tr = (float)Math.Tan(roll0);
            float[] along = { halfBase, halfBase, -halfBase, -halfBase };
            float[] side = { 1f, -1f, 1f, -1f }; // +1 = left side (left wheel higher when roll > 0)
            for (int i = 0; i < 4; i++)
            {
                float wheelRef = refY + along[i] * tp + side[i] * halfTrack * tr;
                m_Wheels[i] = m_Ground.Solve(probe, pts[i].X, pts[i].Z, wheelRef, spec.StepUp, spec.StepDown, spec.MaxWaterDepth);
                GroundStatus st = m_Wheels[i].Status;
                if (st == GroundStatus.Blocked || st == GroundStatus.Invalid || st == GroundStatus.DeepWater)
                {
                    worst = st;
                    groundY = refY;
                    pitch = 0f;
                    roll = 0f;
                    return false;
                }
                if (st == GroundStatus.Drop)
                {
                    worst = GroundStatus.Drop;
                }
                if (m_Wheels[i].Height > maxY)
                {
                    maxY = m_Wheels[i].Height;
                    kind = m_Wheels[i].Kind;
                    source = m_Wheels[i].SourceId;
                }
            }

            float front = (m_Wheels[0].Height + m_Wheels[1].Height) * 0.5f;
            float rear = (m_Wheels[2].Height + m_Wheels[3].Height) * 0.5f;
            float left = (m_Wheels[0].Height + m_Wheels[2].Height) * 0.5f;
            float rightH = (m_Wheels[1].Height + m_Wheels[3].Height) * 0.5f;
            groundY = (front + rear) * 0.5f;
            pitch = (float)Math.Atan2(front - rear, spec.WheelBase);
            roll = (float)Math.Atan2(left - rightH, halfTrack * 2f);
            // Only drop when the whole car has lost support; a single wheel over a curb edge keeps it supported.
            if (worst == GroundStatus.Drop)
            {
                int drops = 0;
                for (int i = 0; i < 4; i++)
                {
                    if (m_Wheels[i].Status == GroundStatus.Drop)
                    {
                        drops++;
                    }
                }
                if (drops < 3)
                {
                    worst = GroundStatus.Supported;
                    groundY = Math.Max(groundY, maxY - spec.StepDown);
                }
            }
            return true;
        }
    }
}
