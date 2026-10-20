using System;
using System.Collections.Generic;
using DriveTheCityII.Diagnostics;
using Game;
using Game.Common;
using Game.Creatures;
using Game.Objects;
using Game.Prefabs;
using Game.Vehicles;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Transform = Game.Objects.Transform;
using Resident = Game.Creatures.Resident;
using Temp = Game.Tools.Temp;
using PersonalCar = Game.Vehicles.PersonalCar;

namespace DriveTheCityII.CityBridge
{
    /// <summary>Entity categories of SPEC-ENTITY.</summary>
    public enum ActorKind : byte
    {
        Avatar = 1,
        CareerVehicle = 2,
        BorrowedNativeVehicle = 3,
        MissionObject = 4,
        CollisionProxy = 5,
    }

    /// <summary>
    /// Tag on every entity the mod spawned or borrowed (registry link). Deliberately NOT serializable: mod entities are
    /// removed before every save (see <see cref="ActorOpsSystem"/>), and records, not entities, are persisted (AP-04).
    /// </summary>
    public struct DtcActor : IComponentData
    {
        public ActorKind Kind;
        public int RecordId;
    }

    /// <summary>Describes a car prefab available for career vehicles.</summary>
    public sealed class CarPrefabInfo
    {
        public Entity Prefab;
        public string Name;
        public string Category; // car, taxi, van, truck, bus
        public float3 Size;
        public float MaxSpeed;
        public float Acceleration;
        public float Braking;
        public int Capacity;
    }

    /// <summary>Prefab lookups for humans and cars (refcache notes 04 and 05).</summary>
    public sealed class PrefabCatalog
    {
        private readonly List<Entity> m_Humans = new List<Entity>();
        private readonly List<CarPrefabInfo> m_Cars = new List<CarPrefabInfo>();
        private bool m_Built;

        public IReadOnlyList<CarPrefabInfo> Cars => m_Cars;
        public int HumanCount => m_Humans.Count;

        public void Invalidate()
        {
            m_Built = false;
        }

        public void EnsureBuilt(EntityManager em, PrefabSystem prefabs)
        {
            if (m_Built)
            {
                return;
            }
            m_Humans.Clear();
            m_Cars.Clear();
            using (EntityQuery hq = em.CreateEntityQuery(ComponentType.ReadOnly<ObjectData>(), ComponentType.ReadOnly<HumanData>(), ComponentType.ReadOnly<ResidentData>(), ComponentType.ReadOnly<PrefabData>()))
            using (NativeArray<Entity> hs = hq.ToEntityArray(Allocator.Temp))
            {
                foreach (Entity h in hs)
                {
                    ResidentData rd = em.GetComponentData<ResidentData>(h);
                    if ((rd.m_Age & AgeMask.Adult) != 0)
                    {
                        m_Humans.Add(h);
                    }
                }
            }
            using (EntityQuery cq = em.CreateEntityQuery(new EntityQueryDesc
            {
                All = new[] { ComponentType.ReadOnly<CarData>(), ComponentType.ReadOnly<ObjectData>(), ComponentType.ReadOnly<MovingObjectData>(), ComponentType.ReadOnly<PrefabData>(), ComponentType.ReadOnly<ObjectGeometryData>() },
                None = new[] { ComponentType.ReadOnly<CarTrailerData>(), ComponentType.ReadOnly<BicycleData>() },
            }))
            using (NativeArray<Entity> cs = cq.ToEntityArray(Allocator.Temp))
            {
                foreach (Entity c in cs)
                {
                    if (em.HasComponent<Locked>(c) && em.IsComponentEnabled<Locked>(c))
                    {
                        continue;
                    }
                    string category = null;
                    int capacity = 0;
                    string pname = prefabs.TryGetPrefab(em.GetComponentData<PrefabData>(c), out PrefabBase pbn) ? pbn.name : string.Empty;
                    if (em.HasComponent<PersonalCarData>(c))
                    {
                        category = pname.IndexOf("Van", StringComparison.OrdinalIgnoreCase) >= 0 ? "van" : "car";
                        capacity = em.GetComponentData<PersonalCarData>(c).m_PassengerCapacity;
                    }
                    else if (em.HasComponent<TaxiData>(c))
                    {
                        category = "taxi";
                        capacity = em.GetComponentData<TaxiData>(c).m_PassengerCapacity;
                    }
                    else if (em.HasComponent<PostVanData>(c))
                    {
                        category = "van";
                    }
                    else if (em.HasComponent<DeliveryTruckData>(c))
                    {
                        // Rigid trucks only: semi-tractor units are articulated vehicles, out of v1.0 scope.
                        if (pname.IndexOf("Tractor", StringComparison.OrdinalIgnoreCase) >= 0 || em.HasComponent<CarTractorData>(c))
                        {
                            continue;
                        }
                        capacity = em.GetComponentData<DeliveryTruckData>(c).m_CargoCapacity;
                        category = "truck";
                    }
                    else if (em.HasComponent<PublicTransportVehicleData>(c))
                    {
                        PublicTransportVehicleData pt = em.GetComponentData<PublicTransportVehicleData>(c);
                        // Line buses only: prison vans and evacuation buses are "bus" transport too (a prison van was handed
                        // out as the operator's bus, observed in game on 2026-10-19).
                        if (pt.m_TransportType == TransportType.Bus && (pt.m_PurposeMask & PublicTransportPurpose.TransportLine) != 0
                            && (pt.m_PurposeMask & PublicTransportPurpose.PrisonerTransport) == 0)
                        {
                            category = "bus";
                            capacity = pt.m_PassengerCapacity;
                        }
                    }
                    if (category == null)
                    {
                        continue;
                    }
                    CarData cd = em.GetComponentData<CarData>(c);
                    ObjectGeometryData g = em.GetComponentData<ObjectGeometryData>(c);
                    if (g.m_Size.x < 1.3f || g.m_Size.z < 2.8f)
                    {
                        continue; // two-wheelers and other tiny vehicles have no seated driver model here
                    }
                    string name = prefabs.TryGetPrefab(em.GetComponentData<PrefabData>(c), out PrefabBase pb) ? pb.name : c.ToString();
                    m_Cars.Add(new CarPrefabInfo
                    {
                        Prefab = c,
                        Name = name,
                        Category = category,
                        Size = g.m_Size,
                        MaxSpeed = cd.m_MaxSpeed,
                        Acceleration = cd.m_Acceleration,
                        Braking = cd.m_Braking,
                        Capacity = capacity,
                    });
                }
            }
            m_Cars.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));
            m_Built = m_Humans.Count > 0;
            DtcLog.Info($"PrefabCatalog: {m_Humans.Count} adult human prefabs, {m_Cars.Count} car prefabs " +
                        $"(car={Count("car")}, taxi={Count("taxi")}, van={Count("van")}, truck={Count("truck")}, bus={Count("bus")})");
        }

        private int Count(string cat)
        {
            int n = 0;
            foreach (CarPrefabInfo c in m_Cars)
            {
                if (c.Category == cat)
                {
                    n++;
                }
            }
            return n;
        }

        /// <summary>Diagnostic dump of raw prefab data (test bridge "prefabs" command).</summary>
        public static string Dump(EntityManager em, PrefabSystem prefabs, string what)
        {
            var sb = new System.Text.StringBuilder();
            ComponentType filter = what == "human" ? ComponentType.ReadOnly<HumanData>() : ComponentType.ReadOnly<CarData>();
            using (EntityQuery q = em.CreateEntityQuery(filter, ComponentType.ReadOnly<PrefabData>()))
            using (NativeArray<Entity> all = q.ToEntityArray(Allocator.Temp))
            {
                sb.Append(all.Length).Append(" prefabs: ");
                foreach (Entity e in all)
                {
                    string name = prefabs.TryGetPrefab(em.GetComponentData<PrefabData>(e), out PrefabBase pb) ? pb.name : e.ToString();
                    sb.Append(name);
                    if (em.HasComponent<ObjectGeometryData>(e))
                    {
                        float3 s = em.GetComponentData<ObjectGeometryData>(e).m_Size;
                        sb.Append($"[{s.x:0.0}x{s.y:0.0}x{s.z:0.0}]");
                    }
                    if (em.HasComponent<PersonalCarData>(e)) sb.Append("P");
                    if (em.HasComponent<BicycleData>(e)) sb.Append("B");
                    if (em.HasComponent<Locked>(e) && em.IsComponentEnabled<Locked>(e)) sb.Append("L");
                    if (em.HasComponent<ResidentData>(e)) sb.Append("age=").Append((int)em.GetComponentData<ResidentData>(e).m_Age);
                    if (!em.HasComponent<ObjectData>(e)) sb.Append("noOD");
                    if (!em.HasComponent<MovingObjectData>(e)) sb.Append("noMOD");
                    sb.Append("; ");
                }
            }
            return sb.ToString();
        }

        public Entity PickHuman(int seed) => m_Humans.Count == 0 ? Entity.Null : m_Humans[Math.Abs(seed) % m_Humans.Count];

        public CarPrefabInfo FindCar(string nameOrCategory, int seed = 0)
        {
            foreach (CarPrefabInfo c in m_Cars)
            {
                if (string.Equals(c.Name, nameOrCategory, StringComparison.OrdinalIgnoreCase))
                {
                    return c;
                }
            }
            var list = new List<CarPrefabInfo>();
            foreach (CarPrefabInfo c in m_Cars)
            {
                if (string.Equals(c.Category, nameOrCategory, StringComparison.OrdinalIgnoreCase))
                {
                    list.Add(c);
                }
            }
            return list.Count == 0 ? null : list[Math.Abs(seed) % list.Count];
        }
    }

    /// <summary>
    /// Runs structural entity operations at SystemUpdatePhase.Modification1, where Created/Deleted tags are seen by the
    /// game's systems in the same frame (refcache note 04, timing trap). Also removes every mod actor right before the
    /// game serializes a save so that no mod entity is ever written into a save.
    /// </summary>
    public partial class ActorOpsSystem : GameSystemBase, Game.Serialization.IPreSerialize
    {
        private static readonly Queue<Action<EntityManager>> s_Ops = new Queue<Action<EntityManager>>();
        private EntityQuery m_Actors;

        /// <summary>Raised (main thread) after actors were removed for a save; the Player module respawns them.</summary>
        public static event Action ActorsRemovedForSave { add { } remove { } }

        public static void Enqueue(Action<EntityManager> op) => s_Ops.Enqueue(op);

        public static int Pending => s_Ops.Count;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_Actors = GetEntityQuery(new EntityQueryDesc
            {
                All = new[] { ComponentType.ReadOnly<DtcActor>() },
                None = new[] { ComponentType.ReadOnly<Deleted>() },
            });
        }

        protected override void OnUpdate()
        {
            int guard = 0;
            while (s_Ops.Count > 0 && guard++ < 64)
            {
                Action<EntityManager> op = s_Ops.Dequeue();
                try
                {
                    op(EntityManager);
                }
                catch (Exception ex)
                {
                    DtcLog.Error("Actor operation failed", ex);
                }
            }
        }

        private readonly List<Entity> m_HiddenForSave = new List<Entity>();

        /// <summary>
        /// Runs in the Serialize phase before the game's SerializerSystem. Mod actors are tagged Temp, which the save
        /// query excludes, so no mod entity is ever written into a save. They are untagged again by
        /// <see cref="ActorPostSerializeSystem"/> inside the same save call, so they stay alive with all their
        /// references (deleting them here would bypass the game's reference cleanup; observed crash on 2026-10-19).
        /// Borrowed native cars stay in the save as ordinary city traffic; only their AI-exclusion timer is shortened
        /// for the saved copy, so a later load returns them to the AI.
        /// </summary>
        public void PreSerialize(Colossal.Serialization.Entities.Context context)
        {
            m_HiddenForSave.Clear();
            using (NativeArray<Entity> all = m_Actors.ToEntityArray(Allocator.Temp))
            {
                foreach (Entity e in all)
                {
                    DtcActor a = EntityManager.GetComponentData<DtcActor>(e);
                    if (a.Kind == ActorKind.BorrowedNativeVehicle)
                    {
                        EntityManager.SetComponentData(e, new TripSource(Entity.Null) { m_Timer = 64 });
                        continue;
                    }
                    if (!EntityManager.HasComponent<Temp>(e))
                    {
                        EntityManager.AddComponent<Temp>(e);
                        m_HiddenForSave.Add(e);
                    }
                }
            }
            if (m_HiddenForSave.Count > 0)
            {
                DtcLog.Info($"Save: {m_HiddenForSave.Count} mod actor(s) excluded from the saved city");
            }
        }

        /// <summary>Called after the save was written: restores the actors excluded in PreSerialize.</summary>
        public void RestoreAfterSave()
        {
            int n = 0;
            foreach (Entity e in m_HiddenForSave)
            {
                if (EntityManager.Exists(e) && EntityManager.HasComponent<Temp>(e))
                {
                    EntityManager.RemoveComponent<Temp>(e);
                    n++;
                }
            }
            m_HiddenForSave.Clear();
            if (n > 0)
            {
                DtcLog.Info($"Save: {n} mod actor(s) restored after serialization");
            }
        }

        /// <summary>
        /// Immediately tags every mod actor Deleted. Borrowed native vehicles are handed back, except for a save: then
        /// only their AI-exclusion timer is shortened, so the saved copy returns to the AI shortly after a later load
        /// while the live car stays under player control (the timer is refreshed every frame while driving).
        /// </summary>
        public int RemoveAllActors(bool forSave)
        {
            using (NativeArray<Entity> all = m_Actors.ToEntityArray(Allocator.Temp))
            {
                int n = 0;
                foreach (Entity e in all)
                {
                    DtcActor a = EntityManager.GetComponentData<DtcActor>(e);
                    if (a.Kind == ActorKind.BorrowedNativeVehicle)
                    {
                        ActorWriter.HandBackNative(EntityManager, e, 0f);
                    }
                    else
                    {
                        if (a.Kind == ActorKind.CareerVehicle)
                        {
                            ActorWriter.ClearLanePresence(EntityManager, e);
                        }
                        EntityManager.AddComponent<Deleted>(e);
                    }
                    n++;
                }
                return n;
            }
        }

        public int CountActors() => m_Actors.CalculateEntityCount();
    }

    /// <summary>Runs after the game's WriteSystem in the Serialize phase: undoes the save-time exclusion.</summary>
    public partial class ActorPostSerializeSystem : GameSystemBase
    {
        protected override void OnUpdate()
        {
            World.GetExistingSystemManaged<ActorOpsSystem>()?.RestoreAfterSave();
        }
    }

    /// <summary>Entity creation and per-frame pose writes for mod actors (CityBridge; main thread).</summary>
    public static class ActorWriter
    {
        public static Entity SpawnHuman(EntityManager em, Entity prefab, float3 pos, quaternion rot, int recordId, ushort seed)
        {
            ObjectData od = em.GetComponentData<ObjectData>(prefab);
            Entity e = em.CreateEntity(od.m_Archetype);
            em.SetComponentData(e, new Transform { m_Position = pos, m_Rotation = rot });
            em.SetComponentData(e, new PrefabRef { m_Prefab = prefab });
            em.SetComponentData(e, new PseudoRandomSeed(seed));
            if (em.HasComponent<Resident>(e))
            {
                em.RemoveComponent<Resident>(e);
            }
            if (em.HasComponent<HumanNavigation>(e))
            {
                em.SetComponentData(e, new HumanNavigation { m_TargetPosition = pos, m_MaxSpeed = 0f });
            }
            em.AddComponentData(e, new DtcActor { Kind = ActorKind.Avatar, RecordId = recordId });
            return e;
        }

        public static Entity SpawnCar(EntityManager em, Entity prefab, float3 pos, quaternion rot, int recordId, ushort seed)
        {
            ObjectData od = em.GetComponentData<ObjectData>(prefab);
            Entity e = em.CreateEntity(od.m_Archetype);
            em.SetComponentData(e, new Transform { m_Position = pos, m_Rotation = rot });
            em.SetComponentData(e, new PrefabRef { m_Prefab = prefab });
            em.SetComponentData(e, new PseudoRandomSeed(seed));
            // The mod's vehicles have no owner (household, depot, company). Game systems assume role vehicles always
            // have one: PersonalCarOwnerSystem deletes owner-less personal cars, and TransportPathfindSetup reads the
            // Owner array of every taxi without a check (native crash observed 2026-10-19). Career vehicles are
            // therefore role-neutral cars: every service-role component is removed; the model and rendering remain.
            StripRoles(em, e);
            em.AddComponentData(e, new TripSource(Entity.Null) { m_Timer = int.MaxValue });
            em.AddComponentData(e, new DtcActor { Kind = ActorKind.CareerVehicle, RecordId = recordId });
            return e;
        }

        private static readonly ComponentType[] s_RoleComponents =
        {
            ComponentType.ReadWrite<PersonalCar>(),
            ComponentType.ReadWrite<Game.Vehicles.Taxi>(),
            ComponentType.ReadWrite<Game.Vehicles.PublicTransport>(),
            ComponentType.ReadWrite<Game.Vehicles.CargoTransport>(),
            ComponentType.ReadWrite<Game.Vehicles.DeliveryTruck>(),
            ComponentType.ReadWrite<Game.Vehicles.PostVan>(),
            ComponentType.ReadWrite<Game.Vehicles.GarbageTruck>(),
            ComponentType.ReadWrite<Game.Vehicles.Ambulance>(),
            ComponentType.ReadWrite<Game.Vehicles.FireEngine>(),
            ComponentType.ReadWrite<Game.Vehicles.PoliceCar>(),
            ComponentType.ReadWrite<Game.Vehicles.Hearse>(),
            ComponentType.ReadWrite<Game.Vehicles.MaintenanceVehicle>(),
            ComponentType.ReadWrite<Game.Vehicles.WorkVehicle>(),
            ComponentType.ReadWrite<Game.Vehicles.PrisonerTransport>(),
            ComponentType.ReadWrite<Game.Vehicles.RoadMaintenanceVehicle>(),
            ComponentType.ReadWrite<Game.Vehicles.Bicycle>(),
            ComponentType.ReadWrite<Game.Simulation.ServiceDispatch>(),
            ComponentType.ReadWrite<Game.Vehicles.Passenger>(),
            ComponentType.ReadWrite<Game.Pathfind.PathInformation>(),
        };

        public static void StripRoles(EntityManager em, Entity e)
        {
            foreach (ComponentType ct in s_RoleComponents)
            {
                if (em.HasComponent(e, ct))
                {
                    em.RemoveComponent(e, ct);
                }
            }
        }

        /// <summary>
        /// Sandbox takeover of a native car: excluded from AI with TripSource (refcache note 04 §3a). The car's lane-change
        /// lane and blocked lanes are unregistered first: the game keeps a car registered on each of them (CarNavigationHelpers
        /// .CurrentLaneCache), only its navigation system removes those entries, and that system no longer runs for the
        /// car. Left in place they are ghosts that native traffic queues behind (ISSUE-033).
        /// </summary>
        public static void BorrowNative(EntityManager em, Entity car, int recordId)
        {
            ClearExtraLaneRegistrations(em, car);
            if (!em.HasComponent<TripSource>(car))
            {
                em.AddComponentData(car, new TripSource(Entity.Null) { m_Timer = int.MaxValue });
            }
            else
            {
                em.SetComponentData(car, new TripSource(Entity.Null) { m_Timer = int.MaxValue });
            }
            em.AddComponentData(car, new DtcActor { Kind = ActorKind.BorrowedNativeVehicle, RecordId = recordId });
        }

        /// <summary>Removes a car's lane-object entries on its lane-change lane and blocked lanes (not its current lane).</summary>
        public static int ClearExtraLaneRegistrations(EntityManager em, Entity car)
        {
            if (!em.Exists(car) || !em.HasComponent<CarCurrentLane>(car))
            {
                return 0;
            }
            int removed = 0;
            CarCurrentLane cl = em.GetComponentData<CarCurrentLane>(car);
            if (cl.m_ChangeLane != Entity.Null && cl.m_ChangeLane != cl.m_Lane && em.Exists(cl.m_ChangeLane) && em.HasBuffer<Game.Net.LaneObject>(cl.m_ChangeLane))
            {
                Game.Net.NetUtils.RemoveLaneObject(em.GetBuffer<Game.Net.LaneObject>(cl.m_ChangeLane), car);
                removed++;
            }
            if (cl.m_ChangeLane != Entity.Null)
            {
                cl.m_ChangeLane = Entity.Null;
                em.SetComponentData(car, cl);
            }
            if (em.HasBuffer<BlockedLane>(car))
            {
                var lanes = new List<Entity>();
                foreach (BlockedLane b in em.GetBuffer<BlockedLane>(car, true))
                {
                    lanes.Add(b.m_Lane);
                }
                foreach (Entity lane in lanes)
                {
                    if (lane != cl.m_Lane && em.Exists(lane) && em.HasBuffer<Game.Net.LaneObject>(lane))
                    {
                        Game.Net.NetUtils.RemoveLaneObject(em.GetBuffer<Game.Net.LaneObject>(lane), car);
                        removed++;
                    }
                }
                em.GetBuffer<BlockedLane>(car).Clear();
            }
            return removed;
        }

        /// <summary>
        /// Adds or removes a mod car's motion data (structural change; run from the actor-ops system). Native traffic treats
        /// a car without it as a stationary obstacle (changes lane or edges past), and one with it as traffic to queue
        /// behind (CarNavigationSystem.CheckBlocker; ISSUE-033).
        /// </summary>
        public static void SetMovingPresence(EntityManager em, Entity car, bool moving)
        {
            if (!em.Exists(car) || em.HasComponent<Deleted>(car))
            {
                return;
            }
            bool has = em.HasComponent<Moving>(car);
            if (moving && !has)
            {
                em.AddComponentData(car, new Moving());
            }
            else if (!moving && has)
            {
                em.RemoveComponent<Moving>(car);
            }
        }

        /// <summary>
        /// Writes the game's "blocked by" link of a mod-driven car (ISSUE-033): the vehicle it waits behind, or none.
        /// Speed is stored like the game does (m/s x 2.295 in a byte; under 6 counts as blocked).
        /// </summary>
        public static void SetBlocker(EntityManager em, Entity car, Entity blocker, float speed)
        {
            if (!em.Exists(car) || !em.HasComponent<Blocker>(car))
            {
                return;
            }
            Blocker want = blocker == Entity.Null
                ? new Blocker { m_Blocker = Entity.Null, m_Type = BlockerType.None, m_MaxSpeed = byte.MaxValue }
                : new Blocker { m_Blocker = blocker, m_Type = BlockerType.Continuing, m_MaxSpeed = (byte)math.clamp((int)math.round(math.abs(speed) * 2.295f), 0, 255) };
            Blocker have = em.GetComponentData<Blocker>(car);
            if (have.m_Blocker != want.m_Blocker || have.m_Type != want.m_Type || have.m_MaxSpeed != want.m_MaxSpeed)
            {
                em.SetComponentData(car, want);
            }
        }

        /// <summary>
        /// Hand back a borrowed native car to the AI: lane marked obsolete so navigation re-finds a lane and re-paths,
        /// velocity and target set, TripSource and our tag removed (refcache note 04 §3c).
        /// </summary>
        public static void HandBackNative(EntityManager em, Entity car, float speed)
        {
            if (!em.Exists(car))
            {
                return;
            }
            if (em.HasComponent<CarCurrentLane>(car))
            {
                CarCurrentLane cl = em.GetComponentData<CarCurrentLane>(car);
                cl.m_LaneFlags |= Game.Vehicles.CarLaneFlags.Obsolete;
                em.SetComponentData(car, cl);
            }
            if (em.HasComponent<Transform>(car))
            {
                Transform t = em.GetComponentData<Transform>(car);
                float3 fwd = math.mul(t.m_Rotation, new float3(0, 0, 1));
                if (em.HasComponent<CarNavigation>(car))
                {
                    em.SetComponentData(car, new CarNavigation { m_TargetPosition = t.m_Position + fwd * 4f, m_TargetRotation = default, m_MaxSpeed = math.max(0f, speed) });
                }
                if (em.HasComponent<Moving>(car))
                {
                    em.SetComponentData(car, new Moving { m_Velocity = fwd * math.max(0f, speed) });
                }
            }
            if (em.HasComponent<Blocker>(car))
            {
                em.SetComponentData(car, default(Blocker));
            }
            if (em.HasComponent<Game.Pathfind.PathOwner>(car))
            {
                var po = em.GetComponentData<Game.Pathfind.PathOwner>(car);
                po.m_State |= Game.Pathfind.PathFlags.Obsolete;
                // The game's deadlock check may have marked the car stuck while the player drove it (it reports what it
                // waits behind, ISSUE-033); the AI would then park it at once.
                po.m_State &= ~Game.Pathfind.PathFlags.Stuck;
                em.SetComponentData(car, po);
            }
            if (em.HasComponent<TripSource>(car))
            {
                em.RemoveComponent<TripSource>(car);
            }
            if (em.HasComponent<DtcActor>(car))
            {
                em.RemoveComponent<DtcActor>(car);
            }
        }

        /// <summary>Writes the rendered pose: Transform, Moving, and all four TransformFrame slots (no interpolation lag).</summary>
        public static void WritePose(EntityManager em, Entity e, float3 pos, quaternion rot, float3 velocity, TransformFlags flags, TransformState state, byte activity = 0)
        {
            em.SetComponentData(e, new Transform { m_Position = pos, m_Rotation = rot });
            if (em.HasComponent<Moving>(e))
            {
                em.SetComponentData(e, new Moving { m_Velocity = velocity });
            }
            if (!em.HasBuffer<TransformFrame>(e))
            {
                return;
            }
            DynamicBuffer<TransformFrame> frames = em.GetBuffer<TransformFrame>(e);
            if (frames.Length < 4)
            {
                frames.ResizeUninitialized(4);
            }
            var f = new TransformFrame
            {
                m_Position = pos,
                m_Velocity = float3.zero,
                m_Rotation = rot,
                m_Flags = flags,
                m_State = state,
                m_Activity = activity,
            };
            for (int i = 0; i < 4; i++)
            {
                f.m_StateTimer = frames[i].m_StateTimer;
                frames[i] = f;
            }
        }

        /// <summary>Keeps the avatar's own navigation target on its pose so HumanMoveSystem never pulls it elsewhere.</summary>
        public static void PinHumanNavigation(EntityManager em, Entity e, float3 pos, float2 dir, bool running)
        {
            if (em.HasComponent<HumanNavigation>(e))
            {
                em.SetComponentData(e, new HumanNavigation { m_TargetPosition = pos, m_TargetDirection = dir, m_MaxSpeed = 0f });
            }
            if (em.HasComponent<Human>(e))
            {
                Human h = em.GetComponentData<Human>(e);
                HumanFlags flags = running ? (h.m_Flags | HumanFlags.Run) : (h.m_Flags & ~HumanFlags.Run);
                if (flags != h.m_Flags)
                {
                    h.m_Flags = flags;
                    em.SetComponentData(e, h);
                }
            }
        }

        /// <summary>
        /// Keeps a mod-controlled car registered in the lane object buffer of the lane it occupies, with a consistent
        /// CarCurrentLane, so native traffic sees it (brakes, queues). Off-lane the registration is removed.
        /// </summary>
        public static void UpdateLanePresence(EntityManager em, Entity car, Entity lane, float t)
        {
            if (!em.Exists(car) || !em.HasComponent<CarCurrentLane>(car))
            {
                return;
            }
            CarCurrentLane cl = em.GetComponentData<CarCurrentLane>(car);
            if (cl.m_Lane != lane)
            {
                if (cl.m_Lane != Entity.Null && em.Exists(cl.m_Lane) && em.HasBuffer<Game.Net.LaneObject>(cl.m_Lane))
                {
                    Game.Net.NetUtils.RemoveLaneObject(em.GetBuffer<Game.Net.LaneObject>(cl.m_Lane), car);
                }
                if (lane != Entity.Null && em.HasBuffer<Game.Net.LaneObject>(lane))
                {
                    Game.Net.NetUtils.AddLaneObject(em.GetBuffer<Game.Net.LaneObject>(lane), car, new float2(t, t));
                }
                cl.m_Lane = lane;
                cl.m_ChangeLane = Entity.Null;
            }
            else if (lane != Entity.Null && em.HasBuffer<Game.Net.LaneObject>(lane))
            {
                Game.Net.NetUtils.UpdateLaneObject(em.GetBuffer<Game.Net.LaneObject>(lane), car, new float2(t, t));
            }
            cl.m_CurvePosition = new float3(t, t, t);
            em.SetComponentData(car, cl);
        }

        /// <summary>Removes the lane registration (call before deleting or handing back a car).</summary>
        public static void ClearLanePresence(EntityManager em, Entity car) => UpdateLanePresence(em, car, Entity.Null, 0f);

        public static void RefreshTripSource(EntityManager em, Entity car)
        {
            if (em.HasComponent<TripSource>(car))
            {
                em.SetComponentData(car, new TripSource(Entity.Null) { m_Timer = int.MaxValue });
            }
        }
    }
}
