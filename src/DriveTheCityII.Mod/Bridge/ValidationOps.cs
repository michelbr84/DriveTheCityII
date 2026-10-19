using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using DriveTheCityII.CityBridge;
using DriveTheCityII.Core.Control;
using DriveTheCityII.Core.Geometry;
using DriveTheCityII.Core.World;
using DriveTheCityII.Player;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace DriveTheCityII.Bridge
{
    /// <summary>Frame-time and memory statistics for benchmark and soak reports (BENCHMARK_PROTOCOL).</summary>
    public static class PerfMonitor
    {
        private static readonly List<float> s_Frames = new List<float>(200000);
        private static float s_Start;
        private static long s_GcStart;

        public static void Reset()
        {
            s_Frames.Clear();
            s_Start = Time.realtimeSinceStartup;
            s_GcStart = GC.GetTotalMemory(false);
        }

        public static void Sample()
        {
            if (s_Frames.Count < 2000000)
            {
                s_Frames.Add(Time.unscaledDeltaTime * 1000f);
            }
        }

        public static string Report()
        {
            if (s_Frames.Count == 0)
            {
                return "no samples";
            }
            var sorted = s_Frames.OrderBy(f => f).ToList();
            float avg = sorted.Average();
            float p50 = sorted[sorted.Count / 2];
            float p95 = sorted[(int)(sorted.Count * 0.95)];
            float p99 = sorted[(int)(sorted.Count * 0.99)];
            float max = sorted[sorted.Count - 1];
            int stalls = sorted.Count(f => f > 100f);
            long gc = GC.GetTotalMemory(false);
            int entities = World.DefaultGameObjectInjectionWorld.EntityManager.UniversalQuery.CalculateEntityCount();
            return string.Format(CultureInfo.InvariantCulture,
                "frames={0} elapsed={1:0.0}s avgMs={2:0.0} (fps {3:0}) p50={4:0.0} p95={5:0.0} p99={6:0.0} max={7:0.0} stalls>100ms={8} managedHeapMB={9:0.0} (start {10:0.0}) entities={11}",
                sorted.Count, Time.realtimeSinceStartup - s_Start, avg, 1000f / avg, p50, p95, p99, max, stalls, gc / 1048576.0, s_GcStart / 1048576.0, entities);
        }
    }

    /// <summary>
    /// Test autopilot for validation runs: SIMULATED input that follows the road lane ahead (never used in normal play).
    /// </summary>
    public static class TestAutopilot
    {
        public static InputFrame Drive(PlayerSystem p, float targetSpeed)
        {
            var f = new InputFrame();
            ControlledVehicle v = p.Vehicle;
            if (v == null)
            {
                return f;
            }
            float2 heading = new float2(math.sin(v.State.Yaw), math.cos(v.State.Yaw));
            float3 pos = v.State.Position.ToF3();
            float3 ahead = pos + new float3(heading.x, 0f, heading.y) * 10f;
            if (p.Probe.FindCarLaneUnder(ahead, heading, out Entity lane, out float t) || p.Probe.FindNearestLanePoint(ahead, 12f, CityWorldProbe.LaneKind.Road, out _, out _, out lane))
            {
                if (p.Probe.FindNearestLanePoint(ahead, 8f, CityWorldProbe.LaneKind.Road, out float3 point, out float3 tan, out _))
                {
                    float3 toPoint = point - pos;
                    float desired = math.atan2(toPoint.x, toPoint.z);
                    float err = MathX.WrapAngle(desired - v.State.Yaw);
                    f.Steer = math.clamp(err * 2.2f, -1f, 1f);
                }
            }
            float speed = v.State.Speed;
            if (speed < targetSpeed)
            {
                f.Throttle = math.clamp((targetSpeed - speed) * 0.4f, 0.2f, 1f);
            }
            else
            {
                f.Brake = math.clamp((speed - targetSpeed) * 0.3f, 0f, 1f);
            }
            return f;
        }
    }

    /// <summary>
    /// cycles N: repeated enter → drive → stop → exit → walk cycles with invariant checks (PHASE-05 gate:
    /// TEST-TRANS-101 campaign). Uses the test autopilot (simulated input) and a career vehicle near the avatar.
    /// </summary>
    public sealed class TransitionCyclesOp : IBridgeOp
    {
        private enum Step { Setup, Approach, Entering, Driving, Stopping, Exiting, Walking, Done }

        private readonly int m_Total;
        private int m_Done;
        private Step m_Step = Step.Setup;
        private float m_StepStart;
        private readonly List<string> m_Failures = new List<string>();
        private int m_ExitsOnSidewalk;
        private float m_TotalDriven;
        private readonly float m_Start;
        private int m_StartActors;

        public TransitionCyclesOp(int cycles)
        {
            m_Total = cycles;
            m_Start = Time.realtimeSinceStartup;
        }

        public bool Ok { get; private set; }
        public string Result { get; private set; } = string.Empty;

        private static PlayerSystem P => PlayerSystem.Instance;

        private void Go(Step s)
        {
            m_Step = s;
            m_StepStart = Time.realtimeSinceStartup;
        }

        private float InStep => Time.realtimeSinceStartup - m_StepStart;

        private void Fail(string why)
        {
            m_Failures.Add($"cycle {m_Done + 1}: {why}");
            P.Input.Injected = null;
            if (P.State == PlayerState.Driving)
            {
                P.Input.Injected = new InputFrame { Brake = 1f };
            }
            Go(Step.Stopping);
        }

        public bool Update()
        {
            PlayerSystem p = P;
            if (p == null || !HostFlow.IsCityReady)
            {
                Result = "no player/city";
                return true;
            }
            switch (m_Step)
            {
                case Step.Setup:
                    if (p.State == PlayerState.Building && !p.EnterCharacterMode())
                    {
                        Result = "could not enter character mode";
                        return true;
                    }
                    if (p.ParkedVehicles.Count == 0 && p.Vehicle == null)
                    {
                        float3 near = p.Avatar.Position.ToF3();
                        if (!p.Probe.FindNearestLanePoint(near, 80f, CityWorldProbe.LaneKind.Road, out float3 lane, out float3 tan, out _) ||
                            !p.SpawnCareerVehicle("car", lane, math.atan2(tan.x, tan.z), "TEST", out _))
                        {
                            Result = "no vehicle could be placed";
                            return true;
                        }
                    }
                    m_StartActors = -1;
                    Go(Step.Approach);
                    return false;
                case Step.Approach:
                {
                    if (InStep < 0.6f)
                    {
                        return false;
                    }
                    if (m_StartActors < 0)
                    {
                        m_StartActors = p.ActorCount;
                    }
                    ControlledVehicle v = p.ParkedVehicles.FirstOrDefault();
                    if (v == null || v.Entity == Entity.Null)
                    {
                        Fail("no parked vehicle to enter");
                        return false;
                    }
                    Vec2 fwd = Vec2.FromYaw(v.State.Yaw);
                    Vec2 side = v.State.Position.XZ - fwd.RightPerp * (v.Spec.Width * 0.5f + 0.9f);
                    p.TeleportAvatar(new float3(side.X, v.State.Position.Y, side.Z));
                    p.TryInteract();
                    Go(Step.Entering);
                    return false;
                }
                case Step.Entering:
                    if (p.State == PlayerState.Driving)
                    {
                        Go(Step.Driving);
                    }
                    else if (InStep > 8f)
                    {
                        Fail("did not reach Driving (" + p.State + ", '" + p.LastMessage + "')");
                    }
                    return false;
                case Step.Driving:
                    p.Input.Injected = TestAutopilot.Drive(p, 9f);
                    if (InStep > 6f)
                    {
                        p.Input.Injected = new InputFrame { Brake = 1f };
                        Go(Step.Stopping);
                    }
                    return false;
                case Step.Stopping:
                    if (p.Vehicle != null && Math.Abs(p.Vehicle.State.Speed) < 0.2f)
                    {
                        p.Input.Injected = new InputFrame();
                        m_TotalDriven = p.Vehicle.State.Odometer;
                        p.TryExit();
                        Go(Step.Exiting);
                    }
                    else if (p.Vehicle == null)
                    {
                        p.Input.Injected = null;
                        Go(Step.Walking);
                    }
                    else if (InStep > 6f)
                    {
                        Fail("vehicle did not stop");
                    }
                    return false;
                case Step.Exiting:
                    if (p.State == PlayerState.OnFoot)
                    {
                        // Invariants (INV-CTRL-11, TEST-TRANS-002): placed on supported ground, never inside a solid.
                        float3 a = p.Avatar.Position.ToF3();
                        var samples = new List<SurfaceSample>();
                        p.Probe.SampleSurfaces(a.x, a.z, a.y - 2f, a.y + 2f, samples);
                        if (!samples.Any(s => Math.Abs(s.Height - a.y) < 0.35f && s.Kind != SurfaceKind.Water))
                        {
                            m_Failures.Add($"cycle {m_Done + 1}: exit point not on a sampled surface at {a}");
                        }
                        var obs = new List<Obstacle>();
                        p.Probe.QueryObstacles(new Vec2(a.x, a.z), 1f, obs);
                        if (obs.Any(o => o.Kind != ObstacleKind.Vehicle && o.Kind != ObstacleKind.Creature && a.y + 0.5f > o.YMin && a.y + 0.5f < o.YMax && Overlap.CircleVsBox(new Vec2(a.x, a.z), 0.25f, o.Footprint).Overlaps))
                        {
                            m_Failures.Add($"cycle {m_Done + 1}: exit point inside an obstacle at {a}");
                        }
                        if (p.Avatar.GroundKind == SurfaceKind.Pedestrian)
                        {
                            m_ExitsOnSidewalk++;
                        }
                        p.Input.Injected = new InputFrame { MoveZ = 1f };
                        Go(Step.Walking);
                    }
                    else if (p.State == PlayerState.Driving && InStep > 0.5f)
                    {
                        // Exit refused (blocked): drive a little and try again.
                        p.Input.Injected = TestAutopilot.Drive(p, 4f);
                        Go(Step.Driving);
                    }
                    else if (InStep > 8f)
                    {
                        Fail("did not reach OnFoot (" + p.State + ")");
                    }
                    return false;
                case Step.Walking:
                    if (InStep > 1.5f)
                    {
                        p.Input.Injected = null;
                        m_Done++;
                        if (m_Done >= m_Total)
                        {
                            Go(Step.Done);
                        }
                        else
                        {
                            Go(Step.Approach);
                        }
                    }
                    return false;
                case Step.Done:
                default:
                    p.Input.Injected = null;
                    int actors = p.ActorCount;
                    Ok = m_Failures.Count == 0;
                    Result = string.Format(CultureInfo.InvariantCulture,
                        "cycles={0}/{1} failures={2} exitsOnSidewalk={3} elapsed={4:0}s actorsStart={5} actorsEnd={6} odometer={7:0}m state={8} [simulated input via test autopilot] {9}",
                        m_Done, m_Total, m_Failures.Count, m_ExitsOnSidewalk, Time.realtimeSinceStartup - m_Start, m_StartActors, actors, m_TotalDriven, p.State,
                        string.Join("; ", m_Failures.Take(8)));
                    return true;
            }
        }
    }
}
