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
            if (!P.Probe.FindNearestLanePoint(near, 40f, CityWorldProbe.LaneKind.Parking, out float3 lane, out float3 tan, out _) &&
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
