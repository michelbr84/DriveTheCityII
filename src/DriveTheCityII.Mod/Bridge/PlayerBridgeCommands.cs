using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using DriveTheCityII.CityBridge;
using DriveTheCityII.Core.Control;
using DriveTheCityII.Core.Geometry;
using DriveTheCityII.Core.World;
using DriveTheCityII.Player;
using Unity.Mathematics;
using UnityEngine;

namespace DriveTheCityII.Bridge
{
    /// <summary>
    /// Bridge commands for the Player module. "input" injects SIMULATED input into our controller (not keyboard or mouse
    /// events); results obtained with it are host execution with simulated input, and are reported as such.
    /// </summary>
    public static class PlayerBridgeCommands
    {
        private static float F(string s) => float.Parse(s, CultureInfo.InvariantCulture);

        public static void Register()
        {
            TestBridge.Handlers["player"] = Player;
            TestBridge.Handlers["input"] = Input;
            TestBridge.Handlers["spawncar"] = SpawnCar;
            TestBridge.Handlers["waitstate"] = WaitState;
            TestBridge.Handlers["probe"] = Probe;
            TestBridge.Handlers["cars"] = a => new InstantOp(true, string.Join(" | ", P.Catalog.Cars.Select(c => $"{c.Name}:{c.Category}:{c.Size.z:0.0}m")));
            TestBridge.Handlers["look"] = a =>
            {
                P.CameraYaw = F(a[1]) * MathX.Deg2Rad;
                if (a.Length > 2)
                {
                    P.CameraPitch = F(a[2]);
                }
                return new InstantOp(true, "look set");
            };
            TestBridge.Handlers["prefabs"] = a => new InstantOp(true, PrefabCatalog.Dump(Unity.Entities.World.DefaultGameObjectInjectionWorld.EntityManager,
                Unity.Entities.World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<Game.Prefabs.PrefabSystem>(), a.Length > 1 ? a[1] : "car"));
            TestBridge.Handlers["leakcheck"] = a => new InstantOp(true, "excluded-signature cars=" + P.Probe.CountForeignExcludedCars(Unity.Entities.World.DefaultGameObjectInjectionWorld.EntityManager) + " actors=" + P.ActorCount);
            TestBridge.Handlers["hud"] = a => new InstantOp(UI.HudUISystem.Instance != null, UI.HudUISystem.Instance == null ? "no hud" : UI.HudUISystem.Instance.DebugJson());
            TestBridge.Handlers["ui"] = a =>
            {
                UI.HudUISystem.Instance?.InvokeAction(a[1]);
                return new InstantOp(UI.HudUISystem.Instance != null, "ui action " + a[1]);
            };
            TestBridge.Handlers["places"] = a =>
            {
                CityServices.Refresh(Unity.Entities.World.DefaultGameObjectInjectionWorld.EntityManager, P.Probe, 0f);
                string kinds = string.Join(" ", CityServices.Places.Places.GroupBy(pl => pl.Kind).Select(g => g.Key + "=" + g.Count()));
                return new InstantOp(true, $"lanes={CityServices.Router.LaneCount} places={CityServices.Places.Places.Count} [{kinds}] busLines={CityServices.Places.BusLines.Count} " +
                    string.Join(" | ", CityServices.Places.BusLines.Take(5).Select(l => $"{l.Name}#{l.Number}:{l.Stops.Count} stops")));
            };
            TestBridge.Handlers["route"] = a =>
            {
                var em = Unity.Entities.World.DefaultGameObjectInjectionWorld.EntityManager;
                CityServices.Refresh(em, P.Probe, 0f);
                string kind = a.Length > 1 ? a[1] : "Commercial";
                float minDist = a.Length > 2 ? F(a[2]) : 300f;
                float3 from = P.Vehicle != null ? P.Vehicle.State.Position.ToF3() : P.Avatar.Position.ToF3();
                float2 heading = P.Vehicle != null ? new float2(math.sin(P.Vehicle.State.Yaw), math.cos(P.Vehicle.State.Yaw)) : float2.zero;
                CityPlace target = CityServices.Places.Places
                    .Where(pl => pl.Kind.ToString().Equals(kind, StringComparison.OrdinalIgnoreCase) && math.distance(pl.RoadAccess, from) >= minDist)
                    .OrderBy(pl => math.distance(pl.RoadAccess, from)).FirstOrDefault();
                if (target == null)
                {
                    return new InstantOp(false, "no place of kind " + kind);
                }
                CityServices.Places.RefineAccess(P.Probe, target);
                RouteResult r = CityServices.Timed(() => CityServices.Router.FindRoute(em, from, heading, target.RoadAccess, 15f), out long ms);
                MarkerBoard.Clear();
                MarkerBoard.Markers.Add(new WorldMarker { Position = target.RoadAccess, Style = MarkerStyle.Dropoff, Radius = 4f });
                MarkerBoard.Route.AddRange(r.Polyline);
                return new InstantOp(r.Status == RouteStatusCode.Reachable,
                    $"to '{target.Name}' ({target.Kind}, id={target.Id}) straight={math.distance(from, target.RoadAccess):0}m status={r.Status} dist={r.DistanceMeters:0}m time={r.TimeSeconds:0}s expansions={r.Expansions} points={r.Polyline.Count} ms={ms}");
            };
            TestBridge.Handlers["camto"] = a =>
            {
                // camto x z zoom [yawDeg]: moves the city-building camera (test setup for baselines).
                var cus = Unity.Entities.World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<Game.Rendering.CameraUpdateSystem>();
                if (cus?.gamePlayController == null)
                {
                    return new InstantOp(false, "no gameplay camera");
                }
                float x = F(a[1]), z = F(a[2]);
                var terrain = Unity.Entities.World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<Game.Simulation.TerrainSystem>();
                var hd = terrain.GetHeightData();
                float y = Game.Simulation.TerrainUtils.SampleHeight(ref hd, new float3(x, 0f, z));
                cus.gamePlayController.pivot = new UnityEngine.Vector3(x, y, z);
                cus.gamePlayController.zoom = a.Length > 3 ? F(a[3]) : 40f;
                if (a.Length > 4)
                {
                    cus.gamePlayController.rotation = new UnityEngine.Vector3(35f, F(a[4]), 0f);
                }
                return new InstantOp(true, $"camera pivot {x},{y},{z}");
            };
            TestBridge.Handlers["track"] = a => new TrackOp(a.Length > 1 ? F(a[1]) : 3f);
            TestBridge.Handlers["cycles"] = a => new TransitionCyclesOp(a.Length > 1 ? int.Parse(a[1], CultureInfo.InvariantCulture) : 10);
            TestBridge.Handlers["perf"] = a =>
            {
                if (a.Length > 1 && a[1] == "reset")
                {
                    PerfMonitor.Reset();
                    return new InstantOp(true, "perf reset");
                }
                return new InstantOp(true, PerfMonitor.Report());
            };
            TestBridge.Handlers["traffic"] = Traffic;
            TestBridge.Handlers["trafficwatch"] = a => new TrafficWatchOp(a.Length > 1 ? F(a[1]) : 20f);
            TestBridge.Handlers["trafficsetup"] = a =>
            {
                float r = a.Length > 1 ? F(a[1]) : 1500f;
                if (!P.Probe.FindLaneAheadOfTraffic(P.Avatar.Position.ToF3(), r, 45f, out float3 pt, out float3 tan, out int n))
                {
                    return new InstantOp(false, "no lane with moving traffic found");
                }
                float yaw = math.atan2(tan.x, tan.z);
                if (!P.SpawnCareerVehicle("car", pt, yaw, "TEST", out ControlledVehicle v))
                {
                    return new InstantOp(false, P.LastMessage);
                }
                Vec2 fwd = Vec2.FromYaw(yaw);
                Vec2 side = new Vec2(pt.x, pt.z) - fwd.RightPerp * (v.Spec.Width * 0.5f + 0.9f);
                P.TeleportAvatar(new float3(side.X, pt.y, side.Z));
                return new InstantOp(true, $"car placed ahead of {n} moving vehicles at {pt}");
            };
            TestBridge.Handlers["busyroad"] = a =>
            {
                float r = a.Length > 1 ? F(a[1]) : 400f;
                if (!P.Probe.FindBusiestRoadLane(P.Avatar.Position.ToF3(), r, out float3 mid, out float3 tan, out int n))
                {
                    return new InstantOp(false, "no busy lane");
                }
                if (!P.Probe.FindNearestLanePoint(mid, 30f, CityWorldProbe.LaneKind.Sidewalk, out float3 walk, out _, out _))
                {
                    walk = mid;
                }
                P.TeleportAvatar(walk);
                P.CameraYaw = math.atan2(tan.x, tan.z);
                return new InstantOp(true, $"busiest lane has {n} vehicles; avatar at {P.Avatar.Position}");
            };
            TestBridge.Handlers["gotoroad"] = GotoRoad;
            TestBridge.Handlers["gotowalk"] = GotoWalk;
            TestBridge.Handlers["view"] = a =>
            {
                return new InstantOp(true, "view " + P.ActiveView);
            };
            TestStatus.Register();
        }

        private static PlayerSystem P => PlayerSystem.Instance;

        private static IBridgeOp Player(string[] a)
        {
            if (P == null)
            {
                return new InstantOp(false, "player system missing");
            }
            string sub = a.Length > 1 ? a[1].ToLowerInvariant() : "state";
            switch (sub)
            {
                case "enter":
                    return new InstantOp(P.EnterCharacterMode(), "enter -> " + P.State + " :: " + P.LastMessage);
                case "building":
                    P.RequestReturnToBuilding();
                    return new InstantOp(P.State == PlayerState.Building, "building -> " + P.State + " :: " + P.LastMessage);
                case "interact":
                    P.TryInteract();
                    return new InstantOp(true, "interact -> " + P.State + " :: " + P.LastMessage);
                case "exit":
                    P.TryExit();
                    return new InstantOp(P.State == PlayerState.ExitingVehicle, "exit -> " + P.State + " :: " + P.LastMessage);
                case "recover":
                    P.EnterRecovery("bridge request");
                    return new InstantOp(true, "recover -> " + P.State);
                case "sandbox":
                    P.Sandbox = a.Length < 3 || a[2] != "off";
                    return new InstantOp(true, "sandbox=" + P.Sandbox);
                case "state":
                default:
                    return new InstantOp(true, TestStatus.PlayerLine());
            }
        }

        /// <summary>input [fwd=..] [side=..] [throttle=..] [brake=..] [steer=..] [run=1] [hand=1] [t=seconds]</summary>
        private static IBridgeOp Input(string[] a)
        {
            if (P == null)
            {
                return new InstantOp(false, "player system missing");
            }
            var f = new InputFrame();
            float seconds = 1f;
            for (int i = 1; i < a.Length; i++)
            {
                string[] kv = a[i].Split('=');
                if (kv.Length != 2)
                {
                    continue;
                }
                float v = F(kv[1]);
                switch (kv[0].ToLowerInvariant())
                {
                    case "fwd": f.MoveZ = v; break;
                    case "side": f.MoveX = v; break;
                    case "throttle": f.Throttle = v; break;
                    case "brake": f.Brake = v; break;
                    case "steer": f.Steer = v; break;
                    case "run": f.Run = v > 0.5f; break;
                    case "hand": f.Handbrake = v > 0.5f; break;
                    case "t": seconds = v; break;
                }
            }
            P.Input.Injected = f;
            float start = Time.realtimeSinceStartup;
            return new InputOp(seconds, start);
        }

        private sealed class InputOp : IBridgeOp
        {
            private readonly float m_End;
            private readonly float m_Start;

            public InputOp(float seconds, float start)
            {
                m_Start = start;
                m_End = start + seconds;
            }

            public bool Ok { get; private set; } = true;
            public string Result { get; private set; } = string.Empty;

            public bool Update()
            {
                if (Time.realtimeSinceStartup < m_End)
                {
                    return false;
                }
                PlayerSystem.Instance.Input.Injected = null;
                Result = $"simulated input for {Time.realtimeSinceStartup - m_Start:0.00}s :: " + TestStatus.PlayerLine();
                return true;
            }
        }

        private static IBridgeOp WaitState(string[] a)
        {
            if (!Enum.TryParse(a[1], true, out PlayerState target))
            {
                return new InstantOp(false, "unknown state " + a[1]);
            }
            float timeout = a.Length > 2 ? F(a[2]) : 15f;
            return new WaitOp(() => P != null && P.State == target, timeout, "state " + target, 3);
        }

        /// <summary>spawncar [category|prefab] [ahead=meters]: spawns a career vehicle on the nearest road lane.</summary>
        private static IBridgeOp SpawnCar(string[] a)
        {
            string cat = a.Length > 1 ? a[1] : "car";
            float3 near = P.Avatar.Position.ToF3();
            bool roadOnly = a.Length > 2 && a[2] == "road";
            float3 lane = default;
            float3 tan = default;
            if ((roadOnly || !P.Probe.FindNearestLanePoint(near, 40f, CityWorldProbe.LaneKind.Parking, out lane, out tan, out _)) &&
                !P.Probe.FindNearestLanePoint(near, 80f, CityWorldProbe.LaneKind.Road, out lane, out tan, out _))
            {
                return new InstantOp(false, "no road lane within 80 m");
            }
            float yaw = math.atan2(tan.x, tan.z);
            bool ok = P.SpawnCareerVehicle(cat, lane, yaw, "TEST", out ControlledVehicle v);
            return new InstantOp(ok, ok ? $"spawned {v.Prefab.Name} ({v.Prefab.Category}) at {lane} dist={math.distance(near, lane):0.0}m" : P.LastMessage);
        }

        /// <summary>gotoroad: moves the avatar next to the nearest parked career vehicle (test setup only).</summary>
        private static IBridgeOp GotoRoad(string[] a)
        {
            ControlledVehicle v = P.ParkedVehicles.FirstOrDefault();
            if (v == null)
            {
                return new InstantOp(false, "no parked vehicle");
            }
            Vec2 fwd = Vec2.FromYaw(v.State.Yaw);
            Vec2 side = v.State.Position.XZ - fwd.RightPerp * (v.Spec.Width * 0.5f + 0.9f);
            P.TeleportAvatar(new float3(side.X, v.State.Position.Y, side.Z));
            return new InstantOp(true, "avatar at " + P.Avatar.Position);
        }

        /// <summary>gotowalk x z: places the avatar on the ground at x,z (test setup only).</summary>
        private static IBridgeOp GotoWalk(string[] a)
        {
            float x = F(a[1]);
            float z = F(a[2]);
            float y = P.Probe.TerrainHeight(x, z);
            P.TeleportAvatar(new float3(x, y + 30f, z));
            return new InstantOp(true, "avatar at " + P.Avatar.Position);
        }

        /// <summary>traffic: AI vehicles within 40 m of the controlled vehicle, relative to its heading.</summary>
        private static IBridgeOp Traffic(string[] a)
        {
            ControlledVehicle v = P.Vehicle ?? P.ParkedVehicles.FirstOrDefault();
            if (v == null)
            {
                return new InstantOp(false, "no vehicle");
            }
            var em = Unity.Entities.World.DefaultGameObjectInjectionWorld.EntityManager;
            Unity.Entities.Entity myLane = em.Exists(v.Entity) && em.HasComponent<Game.Vehicles.CarCurrentLane>(v.Entity)
                ? em.GetComponentData<Game.Vehicles.CarCurrentLane>(v.Entity).m_Lane : Unity.Entities.Entity.Null;
            var obs = new List<Obstacle>();
            P.Probe.QueryObstacles(v.State.Position.XZ, 40f, obs);
            Vec2 fwd = Vec2.FromYaw(v.State.Yaw);
            var parts = new List<string>();
            int behindStopped = 0;
            foreach (Obstacle o in obs.Where(o => o.Kind == ObstacleKind.Vehicle))
            {
                Vec2 d = o.Footprint.Center - v.State.Position.XZ;
                float along = Vec2.Dot(d, fwd);
                float side = Vec2.Dot(d, fwd.RightPerp);
                float sp = o.Velocity.Length;
                if (along < -2f && along > -30f && Math.Abs(side) < 2.5f && sp < 1f)
                {
                    behindStopped++;
                }
                parts.Add($"[{along:0} {side:0} {sp:0.0}]");
            }
            return new InstantOp(true, $"myLane={myLane} vehicles={parts.Count} behindStoppedSameLine={behindStopped} along/side/speed: {string.Join(" ", parts.Take(20))}");
        }

        private static IBridgeOp Probe(string[] a)
        {
            float3 p = P.State == PlayerState.Driving && P.Vehicle != null ? P.Vehicle.State.Position.ToF3() : P.Avatar.Position.ToF3();
            var samples = new List<SurfaceSample>();
            P.Probe.SampleSurfaces(p.x, p.z, p.y - 60f, p.y + 60f, samples);
            var obs = new List<Obstacle>();
            P.Probe.QueryObstacles(new Vec2(p.x, p.z), 12f, obs);
            return new InstantOp(true, $"at {p} surfaces=[{string.Join(", ", samples)}] obstacles={obs.Count} " +
                $"({string.Join(", ", obs.GroupBy(o => o.Kind).Select(g => g.Key + ":" + g.Count()))}) queries={P.Probe.QueriesThisFrame}");
        }
    }

    /// <summary>
    /// Samples AI cars registered in the same lane as the player's stopped vehicle: counts those that stop behind it
    /// and any that pass through it (ghosting). Host execution; the player's vehicle is held by its handbrake.
    /// </summary>
    public sealed class TrafficWatchOp : IBridgeOp
    {
        private readonly float m_End;
        private float m_Next;
        private readonly Dictionary<Unity.Entities.Entity, float> m_LastAlong = new Dictionary<Unity.Entities.Entity, float>();
        private readonly HashSet<Unity.Entities.Entity> m_StoppedBehind = new HashSet<Unity.Entities.Entity>();
        private readonly HashSet<Unity.Entities.Entity> m_Seen = new HashSet<Unity.Entities.Entity>();
        private int m_Ghosts;
        private float m_MinGap = float.MaxValue;
        private int m_Samples;

        public TrafficWatchOp(float seconds)
        {
            m_End = UnityEngine.Time.realtimeSinceStartup + seconds;
        }

        public bool Ok { get; private set; } = true;
        public string Result { get; private set; } = string.Empty;

        public bool Update()
        {
            float now = UnityEngine.Time.realtimeSinceStartup;
            PlayerSystem p = PlayerSystem.Instance;
            if (p?.Vehicle == null)
            {
                Ok = false;
                Result = "no controlled vehicle";
                return true;
            }
            if (now >= m_Next)
            {
                m_Next = now + 0.5f;
                Sample(p);
            }
            if (now < m_End)
            {
                return false;
            }
            Result = $"samples={m_Samples} sameLaneSeen={m_Seen.Count} stoppedBehind={m_StoppedBehind.Count} ghostPasses={m_Ghosts} minGapBehind={(m_MinGap == float.MaxValue ? -1 : m_MinGap):0.0}m";
            Ok = m_Ghosts == 0;
            return true;
        }

        private void Sample(PlayerSystem p)
        {
            var em = Unity.Entities.World.DefaultGameObjectInjectionWorld.EntityManager;
            ControlledVehicle v = p.Vehicle;
            if (!em.Exists(v.Entity) || !em.HasComponent<Game.Vehicles.CarCurrentLane>(v.Entity))
            {
                return;
            }
            Unity.Entities.Entity lane = em.GetComponentData<Game.Vehicles.CarCurrentLane>(v.Entity).m_Lane;
            if (lane == Unity.Entities.Entity.Null || !em.HasBuffer<Game.Net.LaneObject>(lane))
            {
                return;
            }
            m_Samples++;
            Vec2 fwd = Vec2.FromYaw(v.State.Yaw);
            float half = v.Spec.Length * 0.5f;
            var objs = em.GetBuffer<Game.Net.LaneObject>(lane, true);
            for (int i = 0; i < objs.Length; i++)
            {
                Unity.Entities.Entity e = objs[i].m_LaneObject;
                if (e == v.Entity || !em.Exists(e) || !em.HasComponent<Game.Objects.Transform>(e))
                {
                    continue;
                }
                m_Seen.Add(e);
                float3 pos = em.HasComponent<Game.Rendering.InterpolatedTransform>(e) ? em.GetComponentData<Game.Rendering.InterpolatedTransform>(e).m_Position : em.GetComponentData<Game.Objects.Transform>(e).m_Position;
                Vec2 d = new Vec2(pos.x, pos.z) - v.State.Position.XZ;
                float along = Vec2.Dot(d, fwd);
                float speed = em.HasComponent<Game.Objects.Moving>(e) ? math.length(em.GetComponentData<Game.Objects.Moving>(e).m_Velocity) : 0f;
                if (along < 0f)
                {
                    m_MinGap = Math.Min(m_MinGap, -along - half);
                    if (speed < 0.5f && along > -25f)
                    {
                        m_StoppedBehind.Add(e);
                    }
                }
                if (m_LastAlong.TryGetValue(e, out float last) && last < -half && along > half)
                {
                    m_Ghosts++;
                }
                m_LastAlong[e] = along;
            }
        }
    }

    public static class TestStatus
    {
        public static void Register()
        {
            TestBridgeSystem.StatusProviders.Add(PlayerLine);
        }

        public static string PlayerLine()
        {
            PlayerSystem p = PlayerSystem.Instance;
            if (p == null)
            {
                return "player=none";
            }
            string veh = p.Vehicle == null ? "none" :
                $"{p.Vehicle.Kind}:{p.Vehicle.Prefab?.Name} pos={p.Vehicle.State.Position} yaw={p.Vehicle.State.Yaw * MathX.Rad2Deg:0}deg speed={p.Vehicle.State.Speed:0.00} gear={p.Vehicle.State.Gear} ground={p.Vehicle.State.GroundKind} grounded={p.Vehicle.State.Grounded}";
            return $"player={p.State} overlays={p.StateMachine.Overlays} avatar={p.Avatar.Position} aSpeed={p.Avatar.HorizontalSpeed:0.00} aGround={p.Avatar.GroundKind} " +
                   $"avatarEntity={p.AvatarEntity} vehicle={veh} parked={p.ParkedVehicles.Count} actors={p.ActorCount} view={p.ActiveView} msg='{p.LastMessage}'";
        }
    }
}
