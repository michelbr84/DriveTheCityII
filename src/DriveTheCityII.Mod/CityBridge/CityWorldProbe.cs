using System;
using System.Collections.Generic;
using Colossal.Collections;
using Colossal.Mathematics;
using DriveTheCityII.Core.Geometry;
using DriveTheCityII.Core.World;
using Game.Buildings;
using Game.Common;
using Game.Net;
using Game.Objects;
using Game.Prefabs;
using Game.Simulation;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Edge = Game.Net.Edge;
using CarLane = Game.Net.CarLane;
using PedestrianLane = Game.Net.PedestrianLane;
using PedestrianLaneFlags = Game.Net.PedestrianLaneFlags;
using CarLaneFlags = Game.Net.CarLaneFlags;
using ConnectionLane = Game.Net.ConnectionLane;
using MasterLane = Game.Net.MasterLane;
using EdgeLane = Game.Net.EdgeLane;
using GeometryFlags = Game.Objects.GeometryFlags;
using Transform = Game.Objects.Transform;

namespace DriveTheCityII.CityBridge
{
    /// <summary>
    /// IWorldProbe over the live city (SPEC-CONTACT; refcache note 03). Main-thread only. Call <see cref="BeginFrame"/>
    /// once per frame from the owning system before any query.
    /// Surfaces: terrain, water, and net surfaces using the game's own outline-triangle method, refined to the exact lane
    /// height (road vs sidewalk) when a lane covers the point. Obstacles: buildings as boxes from their mesh bounds (not
    /// the lot), other physical static objects from their geometry, and vehicles near the query.
    /// </summary>
    public sealed class CityWorldProbe : IWorldProbe
    {
        private readonly SystemBase m_System;
        private TerrainSystem m_Terrain;
        private WaterSystem m_Water;
        private Game.Net.SearchSystem m_NetSearch;
        private Game.Objects.SearchSystem m_ObjectSearch;

        private ComponentLookup<Composition> m_Composition;
        private ComponentLookup<EdgeGeometry> m_EdgeGeometry;
        private ComponentLookup<StartNodeGeometry> m_StartNodeGeometry;
        private ComponentLookup<EndNodeGeometry> m_EndNodeGeometry;
        private ComponentLookup<NetCompositionData> m_CompositionData;
        private ComponentLookup<Orphan> m_Orphan;
        private ComponentLookup<Node> m_Node;
        private ComponentLookup<Curve> m_Curve;
        private ComponentLookup<CarLane> m_CarLane;
        private ComponentLookup<PedestrianLane> m_PedLane;
        private ComponentLookup<Game.Net.ParkingLane> m_ParkingLane;
        private ComponentLookup<PrefabRef> m_PrefabRef;
        private ComponentLookup<NetLaneData> m_NetLaneData;
        private BufferLookup<Game.Net.SubLane> m_SubLanes;
        private BufferLookup<LaneObject> m_LaneObjects;
        private ComponentLookup<Transform> m_Transform;
        private ComponentLookup<Game.Rendering.InterpolatedTransform> m_Interpolated;
        private ComponentLookup<Moving> m_Moving;
        private ComponentLookup<ObjectGeometryData> m_ObjectGeometry;
        private ComponentLookup<Building> m_Building;
        private ComponentLookup<Destroyed> m_Destroyed;
        private ComponentLookup<Game.Vehicles.Vehicle> m_Vehicle;
        private ComponentLookup<Game.Creatures.Creature> m_Creature;
        private BufferLookup<Game.Prefabs.SubMesh> m_PrefabSubMesh;
        private ComponentLookup<MeshData> m_MeshData;
        private ComponentLookup<Deleted> m_Deleted;
        private ComponentLookup<EdgeLane> m_EdgeLane;
        private ComponentLookup<ConnectionLane> m_ConnectionLane;
        private ComponentLookup<MasterLane> m_MasterLane;
        private ComponentLookup<Owner> m_Owner;
        private ComponentLookup<Unspawned> m_Unspawned;
        private ComponentLookup<Edge> m_EdgeData;

        private TerrainHeightData m_Height;
        private WaterSurfaceData<SurfaceWater> m_WaterData;
        private bool m_HasWater;
        private NativeQuadTree<Entity, QuadTreeBoundsXZ> m_NetTree;
        private NativeQuadTree<Entity, QuadTreeBoundsXZ> m_StaticTree;
        private NativeQuadTree<Entity, QuadTreeBoundsXZ> m_MovingTree;
        private Bounds3 m_TerrainBounds;
        private bool m_Ready;

        private readonly List<Entity> m_Items = new List<Entity>(64);
        private readonly HashSet<Entity> m_Seen = new HashSet<Entity>();
        private readonly Dictionary<Entity, List<(Obb2 box, float yMin, float yMax)>> m_BuildingCache = new Dictionary<Entity, List<(Obb2, float, float)>>();

        /// <summary>Entities ignored by obstacle queries (the controlled vehicle, the avatar).</summary>
        public readonly HashSet<Entity> IgnoreEntities = new HashSet<Entity>();

        /// <summary>Include pedestrians as obstacles (vehicle queries), not only vehicles.</summary>
        public bool IncludeCreatures;

        /// <summary>Counters for diagnostics.</summary>
        public int QueriesThisFrame;

        public CityWorldProbe(SystemBase system)
        {
            m_System = system;
            m_Composition = system.GetComponentLookup<Composition>(true);
            m_EdgeGeometry = system.GetComponentLookup<EdgeGeometry>(true);
            m_StartNodeGeometry = system.GetComponentLookup<StartNodeGeometry>(true);
            m_EndNodeGeometry = system.GetComponentLookup<EndNodeGeometry>(true);
            m_CompositionData = system.GetComponentLookup<NetCompositionData>(true);
            m_Orphan = system.GetComponentLookup<Orphan>(true);
            m_Node = system.GetComponentLookup<Node>(true);
            m_Curve = system.GetComponentLookup<Curve>(true);
            m_CarLane = system.GetComponentLookup<CarLane>(true);
            m_PedLane = system.GetComponentLookup<PedestrianLane>(true);
            m_ParkingLane = system.GetComponentLookup<Game.Net.ParkingLane>(true);
            m_PrefabRef = system.GetComponentLookup<PrefabRef>(true);
            m_NetLaneData = system.GetComponentLookup<NetLaneData>(true);
            m_SubLanes = system.GetBufferLookup<Game.Net.SubLane>(true);
            m_LaneObjects = system.GetBufferLookup<LaneObject>(true);
            m_Transform = system.GetComponentLookup<Transform>(true);
            m_Interpolated = system.GetComponentLookup<Game.Rendering.InterpolatedTransform>(true);
            m_Moving = system.GetComponentLookup<Moving>(true);
            m_ObjectGeometry = system.GetComponentLookup<ObjectGeometryData>(true);
            m_Building = system.GetComponentLookup<Building>(true);
            m_Destroyed = system.GetComponentLookup<Destroyed>(true);
            m_Vehicle = system.GetComponentLookup<Game.Vehicles.Vehicle>(true);
            m_Creature = system.GetComponentLookup<Game.Creatures.Creature>(true);
            m_PrefabSubMesh = system.GetBufferLookup<Game.Prefabs.SubMesh>(true);
            m_MeshData = system.GetComponentLookup<MeshData>(true);
            m_Deleted = system.GetComponentLookup<Deleted>(true);
            m_EdgeLane = system.GetComponentLookup<EdgeLane>(true);
            m_ConnectionLane = system.GetComponentLookup<ConnectionLane>(true);
            m_MasterLane = system.GetComponentLookup<MasterLane>(true);
            m_Owner = system.GetComponentLookup<Owner>(true);
            m_Unspawned = system.GetComponentLookup<Unspawned>(true);
            m_EdgeData = system.GetComponentLookup<Edge>(true);
        }

        public bool IsReady => m_Ready;

        /// <summary>Refreshes lookups and grabs the search trees for this frame. The caller must have completed its dependencies.</summary>
        public void BeginFrame()
        {
            QueriesThisFrame = 0;
            World w = m_System.World;
            m_Terrain ??= w.GetExistingSystemManaged<TerrainSystem>();
            m_Water ??= w.GetExistingSystemManaged<WaterSystem>();
            m_NetSearch ??= w.GetExistingSystemManaged<Game.Net.SearchSystem>();
            m_ObjectSearch ??= w.GetExistingSystemManaged<Game.Objects.SearchSystem>();
            m_Ready = m_Terrain != null && m_NetSearch != null && m_ObjectSearch != null;
            if (!m_Ready)
            {
                return;
            }

            m_Composition.Update(m_System);
            m_EdgeGeometry.Update(m_System);
            m_StartNodeGeometry.Update(m_System);
            m_EndNodeGeometry.Update(m_System);
            m_CompositionData.Update(m_System);
            m_Orphan.Update(m_System);
            m_Node.Update(m_System);
            m_Curve.Update(m_System);
            m_CarLane.Update(m_System);
            m_PedLane.Update(m_System);
            m_ParkingLane.Update(m_System);
            m_PrefabRef.Update(m_System);
            m_NetLaneData.Update(m_System);
            m_SubLanes.Update(m_System);
            m_LaneObjects.Update(m_System);
            m_Transform.Update(m_System);
            m_Interpolated.Update(m_System);
            m_Moving.Update(m_System);
            m_ObjectGeometry.Update(m_System);
            m_Building.Update(m_System);
            m_Destroyed.Update(m_System);
            m_Vehicle.Update(m_System);
            m_Creature.Update(m_System);
            m_PrefabSubMesh.Update(m_System);
            m_MeshData.Update(m_System);
            m_Deleted.Update(m_System);
            m_EdgeLane.Update(m_System);
            m_ConnectionLane.Update(m_System);
            m_MasterLane.Update(m_System);
            m_Owner.Update(m_System);
            m_Unspawned.Update(m_System);
            m_EdgeData.Update(m_System);

            m_Height = m_Terrain.GetHeightData();
            m_TerrainBounds = TerrainUtils.GetBounds(ref m_Height);
            m_HasWater = false;
            if (m_Water != null && m_Water.Loaded)
            {
                m_WaterData = m_Water.GetSurfaceData(out JobHandle wDeps);
                wDeps.Complete();
                m_HasWater = true;
            }
            m_NetTree = m_NetSearch.GetNetSearchTree(true, out JobHandle nDeps);
            nDeps.Complete();
            m_StaticTree = m_ObjectSearch.GetStaticSearchTree(true, out JobHandle sDeps);
            sDeps.Complete();
            m_MovingTree = m_ObjectSearch.GetMovingSearchTree(true, out JobHandle mDeps);
            mDeps.Complete();

            if (m_BuildingCache.Count > 4096)
            {
                m_BuildingCache.Clear();
            }
        }

        public bool IsInsideMap(float x, float z)
        {
            if (!m_Ready)
            {
                return false;
            }
            return x > m_TerrainBounds.min.x + 2f && x < m_TerrainBounds.max.x - 2f &&
                   z > m_TerrainBounds.min.z + 2f && z < m_TerrainBounds.max.z - 2f;
        }

        public float TerrainHeight(float x, float z) => m_Ready ? TerrainUtils.SampleHeight(ref m_Height, new float3(x, 0f, z)) : 0f;

        public float WaterDepth(float x, float z)
        {
            if (!m_HasWater)
            {
                return 0f;
            }
            return WaterUtils.SampleDepth(ref m_WaterData, new float3(x, 0f, z));
        }

        // ------------------------------------------------------------------ surfaces

        public void SampleSurfaces(float x, float z, float minY, float maxY, List<SurfaceSample> results)
        {
            if (!m_Ready)
            {
                return;
            }
            QueriesThisFrame++;
            float terrain = TerrainUtils.SampleHeight(ref m_Height, new float3(x, 0f, z));
            results.Add(new SurfaceSample(terrain, SurfaceKind.Terrain, 0));
            if (m_HasWater)
            {
                float depth = WaterUtils.SampleDepth(ref m_WaterData, new float3(x, 0f, z));
                if (depth > 0.05f)
                {
                    results.Add(new SurfaceSample(terrain + depth, SurfaceKind.Water, -1));
                }
            }

            var p = new float3(x, 0f, z);
            CollectNet(new Bounds3(new float3(x - 0.5f, minY - 2f, z - 0.5f), new float3(x + 0.5f, maxY + 2f, z + 0.5f)));
            for (int i = 0; i < m_Items.Count; i++)
            {
                Entity e = m_Items[i];
                if (m_Composition.TryGetComponent(e, out Composition comp))
                {
                    if (m_EdgeGeometry.TryGetComponent(e, out EdgeGeometry eg))
                    {
                        if (Contains(eg.m_Bounds, p))
                        {
                            NetCompositionData cd = m_CompositionData[comp.m_Edge];
                            if (UsableSurface(cd))
                            {
                                float h = float.MinValue;
                                h = math.max(h, CheckSegment(eg.m_Start, cd, p));
                                h = math.max(h, CheckSegment(eg.m_End, cd, p));
                                AddNetSample(results, e, cd, p, h, minY, maxY);
                            }
                        }
                        if (m_StartNodeGeometry.TryGetComponent(e, out StartNodeGeometry sg) && Contains(sg.m_Geometry.m_Bounds, p))
                        {
                            NetCompositionData cd = m_CompositionData[comp.m_StartNode];
                            if (UsableSurface(cd))
                            {
                                AddNetSample(results, e, cd, p, CheckNode(sg.m_Geometry, cd, p), minY, maxY);
                            }
                        }
                        if (m_EndNodeGeometry.TryGetComponent(e, out EndNodeGeometry ng) && Contains(ng.m_Geometry.m_Bounds, p))
                        {
                            NetCompositionData cd = m_CompositionData[comp.m_EndNode];
                            if (UsableSurface(cd))
                            {
                                AddNetSample(results, e, cd, p, CheckNode(ng.m_Geometry, cd, p), minY, maxY);
                            }
                        }
                    }
                }
                else if (m_Orphan.TryGetComponent(e, out Orphan orphan) && m_Node.TryGetComponent(e, out Node node))
                {
                    NetCompositionData cd = m_CompositionData[orphan.m_Composition];
                    if (UsableSurface(cd) && math.distance(node.m_Position.xz, p.xz) <= cd.m_Width * 0.5f)
                    {
                        AddNetSample(results, e, cd, p, node.m_Position.y + cd.m_SurfaceHeight.max, minY, maxY);
                    }
                }
            }
        }

        private static bool UsableSurface(NetCompositionData cd) =>
            (cd.m_State & CompositionState.Marker) == 0 && (cd.m_State & CompositionState.HasSurface) != 0;

        private void AddNetSample(List<SurfaceSample> results, Entity e, NetCompositionData cd, float3 p, float outlineHeight, float minY, float maxY)
        {
            if (outlineHeight == float.MinValue)
            {
                return;
            }
            SurfaceKind kind = (cd.m_Flags.m_General & CompositionFlags.General.Tunnel) != 0 ? SurfaceKind.Tunnel
                : (cd.m_Flags.m_General & CompositionFlags.General.Elevated) != 0 ? SurfaceKind.Elevated
                : SurfaceKind.Road;
            float h = outlineHeight;
            // Refine with the lane under the point: car lanes sit below the curb-top outline height.
            if (TryLaneSurface(e, p, outlineHeight, out float laneH, out bool pedestrian))
            {
                h = laneH;
                if (pedestrian && kind == SurfaceKind.Road)
                {
                    kind = SurfaceKind.Pedestrian;
                }
            }
            if (h < minY || h > maxY)
            {
                return;
            }
            results.Add(new SurfaceSample(h, kind, e.Index | ((long)e.Version << 32)));
        }

        private bool TryLaneSurface(Entity owner, float3 p, float nearHeight, out float height, out bool pedestrian)
        {
            height = 0f;
            pedestrian = false;
            if (!m_SubLanes.TryGetBuffer(owner, out DynamicBuffer<Game.Net.SubLane> lanes))
            {
                return false;
            }
            float best = float.MaxValue;
            for (int i = 0; i < lanes.Length; i++)
            {
                Entity lane = lanes[i].m_SubLane;
                bool isCar = m_CarLane.HasComponent(lane);
                bool isPed = !isCar && m_PedLane.HasComponent(lane);
                bool isPark = !isCar && !isPed && m_ParkingLane.HasComponent(lane);
                if (!isCar && !isPed && !isPark)
                {
                    continue;
                }
                if (!m_Curve.TryGetComponent(lane, out Curve curve))
                {
                    continue;
                }
                float width = 3f;
                if (m_PrefabRef.TryGetComponent(lane, out PrefabRef lp) && m_NetLaneData.TryGetComponent(lp.m_Prefab, out NetLaneData nld))
                {
                    width = math.max(0.5f, nld.m_Width);
                }
                float d = MathUtils.Distance(curve.m_Bezier.xz, p.xz, out float t);
                if (d > width * 0.5f + 0.15f)
                {
                    continue;
                }
                float y = MathUtils.Position(curve.m_Bezier, t).y;
                if (math.abs(y - nearHeight) > 1.5f)
                {
                    continue;
                }
                if (d < best)
                {
                    best = d;
                    height = y;
                    pedestrian = isPed;
                }
            }
            return best < float.MaxValue;
        }

        /// <summary>The game's outline-triangle method (VehicleOutOfControlSystem.NetIterator.CheckSegment).</summary>
        private static float CheckSegment(Segment segment, NetCompositionData cd, float3 p)
        {
            float result = float.MinValue;
            float3 l0 = MathUtils.Position(segment.m_Left, 0f);
            float3 r0 = MathUtils.Position(segment.m_Right, 0f);
            l0.y += cd.m_SurfaceHeight.max;
            r0.y += cd.m_SurfaceHeight.max;
            for (int i = 1; i <= 8; i++)
            {
                float t = i / 8f;
                float3 l1 = MathUtils.Position(segment.m_Left, t);
                float3 r1 = MathUtils.Position(segment.m_Right, t);
                l1.y += cd.m_SurfaceHeight.max;
                r1.y += cd.m_SurfaceHeight.max;
                var t1 = new Triangle3(l0, r0, l1);
                if (MathUtils.Intersect(t1.xz, p.xz, out float2 b1))
                {
                    result = math.max(result, MathUtils.Position(t1, b1).y);
                }
                var t2 = new Triangle3(r1, l1, r0);
                if (MathUtils.Intersect(t2.xz, p.xz, out float2 b2))
                {
                    result = math.max(result, MathUtils.Position(t2, b2).y);
                }
                l0 = l1;
                r0 = r1;
            }
            return result;
        }

        private static float CheckNode(EdgeNodeGeometry geo, NetCompositionData cd, float3 p)
        {
            float h = float.MinValue;
            if (geo.m_MiddleRadius > 0f)
            {
                h = math.max(h, CheckSegment(geo.m_Left, cd, p));
                Segment right = geo.m_Right;
                Segment r1 = right;
                Segment r2 = right;
                r1.m_Right = MathUtils.Lerp(right.m_Left, right.m_Right, 0.5f);
                r1.m_Right.d = geo.m_Middle.d;
                r2.m_Left = r1.m_Right;
                h = math.max(h, CheckSegment(r1, cd, p));
                h = math.max(h, CheckSegment(r2, cd, p));
            }
            else
            {
                Segment left = geo.m_Left;
                Segment right = geo.m_Right;
                h = math.max(h, CheckSegment(left, cd, p));
                h = math.max(h, CheckSegment(right, cd, p));
                left.m_Right = geo.m_Middle;
                right.m_Left = geo.m_Middle;
                h = math.max(h, CheckSegment(left, cd, p));
                h = math.max(h, CheckSegment(right, cd, p));
            }
            return h;
        }

        private static bool Contains(Bounds3 b, float3 p) => p.x >= b.min.x && p.x <= b.max.x && p.z >= b.min.z && p.z <= b.max.z;

        // ------------------------------------------------------------------ obstacles

        public void QueryObstacles(Vec2 center, float radius, List<Obstacle> results)
        {
            if (!m_Ready)
            {
                return;
            }
            QueriesThisFrame++;
            var area = new Bounds3(new float3(center.X - radius, -100000f, center.Z - radius), new float3(center.X + radius, 100000f, center.Z + radius));

            // Static objects.
            m_Items.Clear();
            var it = new Collector { Area = area, Items = m_Items, RequireMask = BoundsMask.NotOverridden };
            m_StaticTree.Iterate(ref it);
            for (int i = 0; i < m_Items.Count; i++)
            {
                AddStaticObstacles(m_Items[i], results);
            }

            // Vehicles (and optionally creatures) registered on lanes near the query, plus the moving tree.
            m_Seen.Clear();
            CollectNet(area);
            for (int i = 0; i < m_Items.Count; i++)
            {
                if (!m_SubLanes.TryGetBuffer(m_Items[i], out DynamicBuffer<Game.Net.SubLane> lanes))
                {
                    continue;
                }
                for (int l = 0; l < lanes.Length; l++)
                {
                    if (!m_LaneObjects.TryGetBuffer(lanes[l].m_SubLane, out DynamicBuffer<LaneObject> objs))
                    {
                        continue;
                    }
                    for (int k = 0; k < objs.Length; k++)
                    {
                        AddMovingObstacle(objs[k].m_LaneObject, center, radius, results);
                    }
                }
            }
            var mit = new Collector { Area = area, Items = m_Items };
            m_Items.Clear();
            m_MovingTree.Iterate(ref mit);
            for (int i = 0; i < m_Items.Count; i++)
            {
                AddMovingObstacle(m_Items[i], center, radius, results);
            }
        }

        private void AddMovingObstacle(Entity e, Vec2 center, float radius, List<Obstacle> results)
        {
            // Unspawned objects are not in the world for the player: vehicles stored in garages, cars waiting to spawn. A
            // motorbike in a garage above a kerb stopped a bus at 12 m/s on a straight road (observed in game on 2026-10-20).
            if (!m_Seen.Add(e) || IgnoreEntities.Contains(e) || m_Deleted.HasComponent(e) || m_Unspawned.HasComponent(e))
            {
                return;
            }
            bool vehicle = m_Vehicle.HasComponent(e);
            bool creature = !vehicle && m_Creature.HasComponent(e);
            if (!vehicle && !(creature && IncludeCreatures))
            {
                return;
            }
            float3 pos;
            quaternion rot;
            if (m_Interpolated.TryGetComponent(e, out var it))
            {
                pos = it.m_Position;
                rot = it.m_Rotation;
            }
            else if (m_Transform.TryGetComponent(e, out Transform tr))
            {
                pos = tr.m_Position;
                rot = tr.m_Rotation;
            }
            else
            {
                return;
            }
            if (math.distance(pos.xz, new float2(center.X, center.Z)) > radius + 8f)
            {
                return;
            }
            if (!m_PrefabRef.TryGetComponent(e, out PrefabRef pr) || !m_ObjectGeometry.TryGetComponent(pr.m_Prefab, out ObjectGeometryData geo))
            {
                return;
            }
            float3 fwd = math.mul(rot, new float3(0f, 0f, 1f));
            float3 size = geo.m_Size;
            Vec2 vel = Vec2.Zero;
            if (m_Moving.TryGetComponent(e, out Moving mv))
            {
                vel = new Vec2(mv.m_Velocity.x, mv.m_Velocity.z);
            }
            float3 c = pos + math.mul(rot, MathUtils.Center(geo.m_Bounds));
            // Vehicle prefab bounds include the mirrors; contacts use the body width (see VehicleBodyWidthFactor).
            float bodyFactor = vehicle ? VehicleBodyWidthFactor : 1f;
            // A vehicle without motion data is parked: solid like any static object, but never traffic to queue behind
            // (a bus waited behind a car parked beside its lane, observed in game on 2026-10-19).
            bool parked = vehicle && !m_Moving.HasComponent(e);
            results.Add(new Obstacle
            {
                Footprint = new Obb2(new Vec2(c.x, c.z), new Vec2(fwd.x, fwd.z), math.max(0.2f, (geo.m_Bounds.max.x - geo.m_Bounds.min.x) * 0.5f * bodyFactor), math.max(0.2f, (geo.m_Bounds.max.z - geo.m_Bounds.min.z) * 0.5f)),
                YMin = pos.y + geo.m_Bounds.min.y,
                YMax = pos.y + math.max(geo.m_Bounds.max.y, 1f),
                Kind = parked ? ObstacleKind.StaticObject : vehicle ? ObstacleKind.Vehicle : ObstacleKind.Creature,
                SourceId = e.Index | ((long)e.Version << 32),
                Velocity = vel,
            });
            _ = size;
        }

        private void AddStaticObstacles(Entity e, List<Obstacle> results)
        {
            if (IgnoreEntities.Contains(e) || m_Deleted.HasComponent(e) || m_Unspawned.HasComponent(e))
            {
                return;
            }
            if (!m_Transform.TryGetComponent(e, out Transform tr) || !m_PrefabRef.TryGetComponent(e, out PrefabRef pr))
            {
                return;
            }
            if (!m_ObjectGeometry.TryGetComponent(pr.m_Prefab, out ObjectGeometryData geo))
            {
                return;
            }
            if ((geo.m_Flags & GeometryFlags.Marker) != 0 || (geo.m_Flags & GeometryFlags.Physical) == 0)
            {
                return;
            }
            long sid = e.Index | ((long)e.Version << 32);
            if (m_Building.HasComponent(e))
            {
                if (m_Destroyed.HasComponent(e))
                {
                    return;
                }
                if (!m_BuildingCache.TryGetValue(e, out var boxes))
                {
                    boxes = BuildingBoxes(tr, pr.m_Prefab, geo);
                    m_BuildingCache[e] = boxes;
                }
                for (int i = 0; i < boxes.Count; i++)
                {
                    results.Add(new Obstacle { Footprint = boxes[i].box, YMin = boxes[i].yMin, YMax = boxes[i].yMax, Kind = ObstacleKind.Building, SourceId = sid });
                }
                return;
            }

            float3 fwd = math.mul(tr.m_Rotation, new float3(0f, 0f, 1f));
            if ((geo.m_Flags & GeometryFlags.Standing) != 0 && geo.m_LegSize.y > 0.3f)
            {
                // Poles and similar: the leg is the obstacle at walking height; the upper body is overhead.
                float3 lc = tr.m_Position + math.mul(tr.m_Rotation, new float3(geo.m_LegOffset.x, 0f, geo.m_LegOffset.y));
                float lhx = math.max(0.08f, geo.m_LegSize.x * 0.5f);
                float lhz = math.max(0.08f, geo.m_LegSize.z * 0.5f);
                results.Add(new Obstacle
                {
                    Footprint = new Obb2(new Vec2(lc.x, lc.z), new Vec2(fwd.x, fwd.z), lhx, lhz),
                    YMin = tr.m_Position.y,
                    YMax = tr.m_Position.y + geo.m_LegSize.y,
                    Kind = IsSmallProp(e, math.max(lhx, lhz)) ? ObstacleKind.SmallProp : ObstacleKind.StaticObject,
                    SourceId = sid,
                });
                return;
            }
            float height = geo.m_Bounds.max.y - geo.m_Bounds.min.y;
            if (height < 0.3f)
            {
                return;
            }
            float3 center = tr.m_Position + math.mul(tr.m_Rotation, MathUtils.Center(geo.m_Bounds));
            float hx = (geo.m_Bounds.max.x - geo.m_Bounds.min.x) * 0.5f;
            float hz = (geo.m_Bounds.max.z - geo.m_Bounds.min.z) * 0.5f;
            if ((geo.m_Flags & GeometryFlags.Circular) != 0)
            {
                float r = geo.m_Size.x * 0.5f * 0.85f;
                hx = r;
                hz = r;
            }
            bool small = IsSmallProp(e, math.max(hx, hz));
            results.Add(new Obstacle
            {
                Footprint = new Obb2(new Vec2(center.x, center.z), new Vec2(fwd.x, fwd.z), math.max(0.08f, hx), math.max(0.08f, hz)),
                YMin = tr.m_Position.y + geo.m_Bounds.min.y,
                YMax = tr.m_Position.y + geo.m_Bounds.max.y,
                Kind = small ? ObstacleKind.SmallProp : ObstacleKind.StaticObject,
                SourceId = sid,
            });
        }

        /// <summary>
        /// Largest half-extent (m) of a static object that counts as a small prop wherever it stands: poles, posts, signs,
        /// hydrants, bins and bollards (ISSUE-040).
        /// </summary>
        public const float SmallPropMaxHalfExtent = 0.9f;

        /// <summary>
        /// Largest half-extent (m) of street furniture placed by a road or junction (lamps, signals, signs, stop benches)
        /// that still counts as a small prop. A 0.6 x 2.2 m bench at a stop held a 12.7 m bus in a narrow junction
        /// (observed in game on 2026-10-20). Objects placed by buildings (lot fences, garden furniture) never qualify.
        /// </summary>
        public const float RoadFurnitureMaxHalfExtent = 3f;

        /// <summary>What placed a static object: "road" (an edge or junction), "building", "object", or "none".</summary>
        public string OwnerKind(Entity e)
        {
            Entity cur = e;
            for (int depth = 0; depth < 4 && m_Owner.TryGetComponent(cur, out Owner owner) && owner.m_Owner != Entity.Null; depth++)
            {
                cur = owner.m_Owner;
                if (m_Building.HasComponent(cur))
                {
                    return "building";
                }
                if (m_EdgeData.HasComponent(cur) || m_Node.HasComponent(cur))
                {
                    return "road";
                }
            }
            return cur == e ? "none" : "object";
        }

        /// <summary>
        /// Small props are passed by long vehicles (ISSUE-040): anything this small, or road furniture up to
        /// <see cref="RoadFurnitureMaxHalfExtent"/>. Vehicles in the static tree (parked cars) never are.
        /// </summary>
        private bool IsSmallProp(Entity e, float maxHalfExtent)
        {
            if (m_Vehicle.HasComponent(e))
            {
                return false;
            }
            if (maxHalfExtent <= SmallPropMaxHalfExtent)
            {
                return true;
            }
            return maxHalfExtent <= RoadFurnitureMaxHalfExtent && OwnerKind(e) == "road";
        }

        /// <summary>One box per non-decal building mesh taller than 0.6 m, in world space.</summary>
        private List<(Obb2, float, float)> BuildingBoxes(Transform tr, Entity prefab, ObjectGeometryData geo)
        {
            var list = new List<(Obb2, float, float)>(2);
            float3 fwd = math.mul(tr.m_Rotation, new float3(0f, 0f, 1f));
            if (m_PrefabSubMesh.TryGetBuffer(prefab, out DynamicBuffer<Game.Prefabs.SubMesh> subs))
            {
                for (int i = 0; i < subs.Length; i++)
                {
                    Game.Prefabs.SubMesh sm = subs[i];
                    if (!m_MeshData.TryGetComponent(sm.m_SubMesh, out MeshData md))
                    {
                        continue;
                    }
                    if ((md.m_State & MeshFlags.Decal) != 0)
                    {
                        continue;
                    }
                    Bounds3 b = md.m_Bounds;
                    if (b.max.y - b.min.y < 0.6f || b.max.y < 0.6f)
                    {
                        continue;
                    }
                    // Mesh bounds are in the sub-mesh frame; transform its center into the object frame. Sub-mesh rotations
                    // are almost always about Y, so the box keeps the object's forward rotated by the sub-mesh rotation.
                    float3 localCenter = sm.m_Position + math.mul(sm.m_Rotation, MathUtils.Center(b));
                    float3 world = tr.m_Position + math.mul(tr.m_Rotation, localCenter);
                    float3 smFwd = math.mul(math.mul(tr.m_Rotation, sm.m_Rotation), new float3(0f, 0f, 1f));
                    float hx = (b.max.x - b.min.x) * 0.5f;
                    float hz = (b.max.z - b.min.z) * 0.5f;
                    if (hx < 0.2f || hz < 0.2f)
                    {
                        continue;
                    }
                    float yBase = tr.m_Position.y + sm.m_Position.y;
                    list.Add((new Obb2(new Vec2(world.x, world.z), new Vec2(smFwd.x, smFwd.z), hx, hz), yBase + math.max(b.min.y, 0f), yBase + b.max.y));
                }
            }
            if (list.Count == 0)
            {
                // Fallback: the lot box shrunk, so that yards stay walkable around a core.
                float3 c = tr.m_Position + math.mul(tr.m_Rotation, MathUtils.Center(geo.m_Bounds));
                float hx = (geo.m_Bounds.max.x - geo.m_Bounds.min.x) * 0.35f;
                float hz = (geo.m_Bounds.max.z - geo.m_Bounds.min.z) * 0.35f;
                list.Add((new Obb2(new Vec2(c.x, c.z), new Vec2(fwd.x, fwd.z), hx, hz), tr.m_Position.y, tr.m_Position.y + math.max(3f, geo.m_Bounds.max.y)));
            }
            return list;
        }

        // ------------------------------------------------------------------ lanes (queries used by Player/Vehicles)

        /// <summary>Kinds of lanes the Player and Vehicles modules look for.</summary>
        public enum LaneKind
        {
            Sidewalk,
            Road,
            Parking,
        }

        /// <summary>Compatibility overload: pedestrian = sidewalk, otherwise ordinary road lanes.</summary>
        public bool FindNearestLanePoint(float3 pos, float radius, bool pedestrian, out float3 point, out float3 tangent, out Entity laneEntity) =>
            FindNearestLanePoint(pos, radius, pedestrian ? LaneKind.Sidewalk : LaneKind.Road, out point, out tangent, out laneEntity);

        /// <summary>
        /// Nearest point on a lane of the requested kind. Road lanes are real carriageway lanes on road edges only: no
        /// building/driveway connections, no master lanes, no unsafe, forbidden, bus-only, runway or highway lanes.
        /// </summary>
        public bool FindNearestLanePoint(float3 pos, float radius, LaneKind kind, out float3 point, out float3 tangent, out Entity laneEntity)
        {
            point = default;
            tangent = default;
            laneEntity = Entity.Null;
            if (!m_Ready)
            {
                return false;
            }
            CollectNet(new Bounds3(pos - new float3(radius, 50f, radius), pos + new float3(radius, 50f, radius)));
            float best = float.MaxValue;
            for (int i = 0; i < m_Items.Count; i++)
            {
                if (!m_SubLanes.TryGetBuffer(m_Items[i], out DynamicBuffer<Game.Net.SubLane> lanes))
                {
                    continue;
                }
                for (int l = 0; l < lanes.Length; l++)
                {
                    Entity lane = lanes[l].m_SubLane;
                    if (!LaneMatches(lane, kind) || !m_Curve.TryGetComponent(lane, out Curve c))
                    {
                        continue;
                    }
                    float d = MathUtils.Distance(c.m_Bezier, pos, out float t);
                    if (kind != LaneKind.Sidewalk)
                    {
                        // Keep away from lane ends (intersections).
                        t = math.clamp(t, 0.15f, 0.85f);
                        d = math.distance(MathUtils.Position(c.m_Bezier, t), pos);
                    }
                    if (d < best && d <= radius)
                    {
                        best = d;
                        point = MathUtils.Position(c.m_Bezier, t);
                        tangent = MathUtils.Tangent(c.m_Bezier, t);
                        laneEntity = lane;
                    }
                }
            }
            return best < float.MaxValue;
        }

        private bool LaneMatches(Entity lane, LaneKind kind)
        {
            if (m_ConnectionLane.HasComponent(lane) || m_MasterLane.HasComponent(lane))
            {
                return false;
            }
            switch (kind)
            {
                case LaneKind.Sidewalk:
                    if (!m_PedLane.TryGetComponent(lane, out PedestrianLane pl))
                    {
                        return false;
                    }
                    return (pl.m_Flags & (PedestrianLaneFlags.Crosswalk | PedestrianLaneFlags.SideConnection)) == 0;
                case LaneKind.Parking:
                    return m_ParkingLane.HasComponent(lane) && m_EdgeLane.HasComponent(lane);
                default:
                    if (!m_CarLane.TryGetComponent(lane, out CarLane cl) || !m_EdgeLane.HasComponent(lane))
                    {
                        return false;
                    }
                    const CarLaneFlags bad = CarLaneFlags.Unsafe | CarLaneFlags.SideConnection | CarLaneFlags.Runway |
                                             CarLaneFlags.Forbidden | CarLaneFlags.PublicOnly | CarLaneFlags.Highway;
                    return (cl.m_Flags & bad) == 0;
            }
        }

        /// <summary>
        /// The car lane (edge or intersection lane) under a vehicle, preferring lanes aligned with its heading. Used to
        /// register the player's vehicle in the lane object buffers AI traffic reads (refcache note 04 §5).
        /// </summary>
        /// <summary>
        /// Body width as a share of a vehicle prefab's width. Prefab sizes include the mirrors (a sedan reads 2.0 m, a
        /// city bus 3.3 m, the delivery truck 3.2 m), which made two vehicles in neighbouring 3 m lanes touch even when
        /// both were centred (observed in game on 2026-10-19 as repeated side impacts from passing traffic).
        /// </summary>
        public const float VehicleBodyWidthFactor = 0.82f;

        public bool FindCarLaneUnder(float3 pos, float2 heading, out Entity laneEntity, out float curveT)
        {
            laneEntity = Entity.Null;
            curveT = 0f;
            if (!m_Ready)
            {
                return false;
            }
            CollectNet(new Bounds3(pos - new float3(4f, 6f, 4f), pos + new float3(4f, 6f, 4f)));
            float bestScore = float.MaxValue;
            for (int i = 0; i < m_Items.Count; i++)
            {
                if (!m_SubLanes.TryGetBuffer(m_Items[i], out DynamicBuffer<Game.Net.SubLane> lanes))
                {
                    continue;
                }
                for (int l = 0; l < lanes.Length; l++)
                {
                    Entity lane = lanes[l].m_SubLane;
                    if (!m_CarLane.HasComponent(lane) || m_MasterLane.HasComponent(lane) || m_ConnectionLane.HasComponent(lane))
                    {
                        continue;
                    }
                    if (!m_LaneObjects.HasBuffer(lane) || !m_Curve.TryGetComponent(lane, out Curve c))
                    {
                        continue;
                    }
                    float width = 3f;
                    if (m_PrefabRef.TryGetComponent(lane, out PrefabRef lp) && m_NetLaneData.TryGetComponent(lp.m_Prefab, out NetLaneData nld))
                    {
                        width = math.max(1.5f, nld.m_Width);
                    }
                    float d = MathUtils.Distance(c.m_Bezier.xz, pos.xz, out float t);
                    if (d > width * 0.5f + 0.3f)
                    {
                        continue;
                    }
                    float3 lp3 = MathUtils.Position(c.m_Bezier, t);
                    if (math.abs(lp3.y - pos.y) > 2f)
                    {
                        continue;
                    }
                    float2 tan = math.normalizesafe(MathUtils.Tangent(c.m_Bezier, t).xz);
                    float align = math.dot(tan, math.normalizesafe(heading));
                    float score = d + (1f - align) * 2f;
                    if (score < bestScore)
                    {
                        bestScore = score;
                        laneEntity = lane;
                        curveT = t;
                    }
                }
            }
            return laneEntity != Entity.Null;
        }

        /// <summary>Test helper: the road lane with the most vehicles on it within a radius (and its midpoint).</summary>
        public bool FindBusiestRoadLane(float3 pos, float radius, out float3 midpoint, out float3 tangent, out int count)
        {
            midpoint = default;
            tangent = default;
            count = 0;
            if (!m_Ready)
            {
                return false;
            }
            CollectNet(new Bounds3(pos - new float3(radius, 100f, radius), pos + new float3(radius, 100f, radius)));
            for (int i = 0; i < m_Items.Count; i++)
            {
                if (!m_SubLanes.TryGetBuffer(m_Items[i], out DynamicBuffer<Game.Net.SubLane> lanes))
                {
                    continue;
                }
                for (int l = 0; l < lanes.Length; l++)
                {
                    Entity lane = lanes[l].m_SubLane;
                    if (!LaneMatches(lane, LaneKind.Road) || !m_LaneObjects.TryGetBuffer(lane, out DynamicBuffer<LaneObject> objs) || !m_Curve.TryGetComponent(lane, out Curve c))
                    {
                        continue;
                    }
                    if (objs.Length > count && c.m_Length > 40f)
                    {
                        count = objs.Length;
                        midpoint = MathUtils.Position(c.m_Bezier, 0.5f);
                        tangent = MathUtils.Tangent(c.m_Bezier, 0.5f);
                    }
                }
            }
            return count > 0;
        }

        /// <summary>
        /// Test helper: a road lane with moving traffic, and a point on it about <paramref name="aheadMeters"/> ahead of
        /// the leading moving vehicle, so that approaching traffic must react to a vehicle placed there.
        /// </summary>
        public bool FindLaneAheadOfTraffic(float3 pos, float radius, float aheadMeters, out float3 point, out float3 tangent, out int moving)
        {
            point = default;
            tangent = default;
            moving = 0;
            if (!m_Ready)
            {
                return false;
            }
            CollectNet(new Bounds3(pos - new float3(radius, 100f, radius), pos + new float3(radius, 100f, radius)));
            float bestScore = 0f;
            for (int i = 0; i < m_Items.Count; i++)
            {
                if (!m_SubLanes.TryGetBuffer(m_Items[i], out DynamicBuffer<Game.Net.SubLane> lanes))
                {
                    continue;
                }
                for (int l = 0; l < lanes.Length; l++)
                {
                    Entity lane = lanes[l].m_SubLane;
                    if (!LaneMatches(lane, LaneKind.Road) || !m_LaneObjects.TryGetBuffer(lane, out DynamicBuffer<LaneObject> objs) || !m_Curve.TryGetComponent(lane, out Curve c))
                    {
                        continue;
                    }
                    if (c.m_Length < aheadMeters + 30f)
                    {
                        continue;
                    }
                    int count = 0;
                    float lead = -1f;
                    for (int k = 0; k < objs.Length; k++)
                    {
                        Entity e = objs[k].m_LaneObject;
                        if (m_Moving.TryGetComponent(e, out Moving mv) && math.length(mv.m_Velocity) > 3f)
                        {
                            count++;
                            lead = math.max(lead, objs[k].m_CurvePosition.y);
                        }
                    }
                    if (count == 0 || lead < 0f)
                    {
                        continue;
                    }
                    float tAhead = lead + aheadMeters / c.m_Length;
                    if (tAhead > 0.92f)
                    {
                        continue;
                    }
                    float score = count + (1f - math.distance(MathUtils.Position(c.m_Bezier, tAhead), pos) / radius);
                    if (score > bestScore)
                    {
                        bestScore = score;
                        moving = count;
                        point = MathUtils.Position(c.m_Bezier, tAhead);
                        tangent = MathUtils.Tangent(c.m_Bezier, tAhead);
                    }
                }
            }
            return moving > 0;
        }

        /// <summary>
        /// Counts cars that carry the mod's AI exclusion (a source-less TripSource, which the game never creates) without
        /// being under the mod's control (no DtcActor): leak check after a hand-back or a reload (ISSUE-041).
        /// </summary>
        public int CountForeignExcludedCars(EntityManager em)
        {
            using (EntityQuery q = em.CreateEntityQuery(ComponentType.ReadOnly<TripSource>(), ComponentType.ReadOnly<Game.Vehicles.Car>(), ComponentType.Exclude<DtcActor>()))
            using (Unity.Collections.NativeArray<TripSource> arr = q.ToComponentDataArray<TripSource>(Unity.Collections.Allocator.Temp))
            {
                int n = 0;
                foreach (TripSource ts in arr)
                {
                    if (ts.m_Timer > 1000000 || ts.m_Source == Entity.Null)
                    {
                        n++;
                    }
                }
                return n;
            }
        }

        /// <summary>
        /// Height of the car lane the vehicle is following: the nearest lane running along the heading (either way), within
        /// a lane's width of the position and <paramref name="maxDy"/> of its height. Used as the ground level hint where
        /// road surfaces overlap (VehicleController.LevelHint).
        /// </summary>
        public bool TryLaneLevel(float3 pos, float2 heading, float maxDy, out float laneY)
        {
            laneY = float.NaN;
            if (!m_Ready)
            {
                return false;
            }
            CollectNet(new Bounds3(pos - new float3(4f, maxDy + 2f, 4f), pos + new float3(4f, maxDy + 2f, 4f)));
            float best = float.MaxValue;
            float2 h = math.normalizesafe(heading);
            for (int i = 0; i < m_Items.Count; i++)
            {
                if (!m_SubLanes.TryGetBuffer(m_Items[i], out DynamicBuffer<Game.Net.SubLane> lanes))
                {
                    continue;
                }
                for (int l = 0; l < lanes.Length; l++)
                {
                    Entity lane = lanes[l].m_SubLane;
                    if (!m_CarLane.HasComponent(lane) || m_MasterLane.HasComponent(lane) || !m_Curve.TryGetComponent(lane, out Curve c))
                    {
                        continue;
                    }
                    float width = 3f;
                    if (m_PrefabRef.TryGetComponent(lane, out PrefabRef lp) && m_NetLaneData.TryGetComponent(lp.m_Prefab, out NetLaneData nld))
                    {
                        width = math.max(1.5f, nld.m_Width);
                    }
                    float d = MathUtils.Distance(c.m_Bezier.xz, pos.xz, out float t);
                    if (d > width * 0.5f + 1f)
                    {
                        continue;
                    }
                    float3 lp3 = MathUtils.Position(c.m_Bezier, t);
                    float dy = math.abs(lp3.y - pos.y);
                    // Stacked lanes: the one at the vehicle's own height wins unless another is clearly the one followed.
                    float score = d + 0.7f * dy;
                    if (dy > maxDy || score >= best)
                    {
                        continue;
                    }
                    float2 tan = math.normalizesafe(MathUtils.Tangent(c.m_Bezier, t).xz);
                    if (math.abs(math.dot(tan, h)) < 0.7f)
                    {
                        continue;
                    }
                    best = score;
                    laneY = lp3.y;
                }
            }
            return best < float.MaxValue;
        }

        /// <summary>Diagnostics: car lanes within 8 m of a point (owner, master/slave, nearest point, width).</summary>
        public string DescribeLanesAt(float x, float z)
        {
            if (!m_Ready)
            {
                return "not ready";
            }
            var p = new float3(x, 0f, z);
            CollectNet(new Bounds3(new float3(x - 8f, -10000f, z - 8f), new float3(x + 8f, 10000f, z + 8f)));
            var sb = new System.Text.StringBuilder();
            foreach (Entity owner in m_Items)
            {
                if (!m_SubLanes.TryGetBuffer(owner, out DynamicBuffer<Game.Net.SubLane> lanes))
                {
                    continue;
                }
                for (int l = 0; l < lanes.Length; l++)
                {
                    Entity lane = lanes[l].m_SubLane;
                    if (!m_CarLane.HasComponent(lane) || !m_Curve.TryGetComponent(lane, out Curve c))
                    {
                        continue;
                    }
                    float d = MathUtils.Distance(c.m_Bezier.xz, p.xz, out float t);
                    if (d > 8f)
                    {
                        continue;
                    }
                    float3 q = MathUtils.Position(c.m_Bezier, t);
                    float width = 0f;
                    if (m_PrefabRef.TryGetComponent(lane, out PrefabRef lp) && m_NetLaneData.TryGetComponent(lp.m_Prefab, out NetLaneData nld))
                    {
                        width = nld.m_Width;
                    }
                    sb.Append(owner.Index).Append('/').Append(lane.Index)
                      .Append(m_MasterLane.HasComponent(lane) ? " M" : m_ConnectionLane.HasComponent(lane) ? " C" : " S")
                      .Append(" at (").Append(q.x.ToString("0.0")).Append(',').Append(q.y.ToString("0.0")).Append(',').Append(q.z.ToString("0.0"))
                      .Append(") d=").Append(d.ToString("0.0")).Append(" w=").Append(width.ToString("0.0")).Append(" | ");
                }
            }
            return sb.ToString();
        }

        /// <summary>Diagnostics: the net pieces around a point and why each does or does not give a drivable surface.</summary>
        public string DescribeNetsAt(float x, float z)
        {
            if (!m_Ready)
            {
                return "not ready";
            }
            var p = new float3(x, 0f, z);
            CollectNet(new Bounds3(new float3(x - 0.5f, -10000f, z - 0.5f), new float3(x + 0.5f, 10000f, z + 0.5f)));
            var sb = new System.Text.StringBuilder();
            foreach (Entity e in m_Items)
            {
                sb.Append(e.Index).Append(':');
                if (m_Composition.TryGetComponent(e, out Composition comp) && m_EdgeGeometry.TryGetComponent(e, out EdgeGeometry eg))
                {
                    NetCompositionData cd = m_CompositionData[comp.m_Edge];
                    float h = math.max(CheckSegment(eg.m_Start, cd, p), CheckSegment(eg.m_End, cd, p));
                    sb.Append("edge y").Append(eg.m_Bounds.min.y.ToString("0.0")).Append("..").Append(eg.m_Bounds.max.y.ToString("0.0"))
                      .Append(" in=").Append(Contains(eg.m_Bounds, p)).Append(" usable=").Append(UsableSurface(cd))
                      .Append(" state=").Append(cd.m_State).Append(" gen=").Append(cd.m_Flags.m_General)
                      .Append(" h=").Append(h == float.MinValue ? "none" : h.ToString("0.00"));
                    if (m_StartNodeGeometry.TryGetComponent(e, out StartNodeGeometry sg) && Contains(sg.m_Geometry.m_Bounds, p))
                    {
                        NetCompositionData sc = m_CompositionData[comp.m_StartNode];
                        float hs = CheckNode(sg.m_Geometry, sc, p);
                        sb.Append(" startNode usable=").Append(UsableSurface(sc)).Append(" h=").Append(hs == float.MinValue ? "none" : hs.ToString("0.00"));
                    }
                    if (m_EndNodeGeometry.TryGetComponent(e, out EndNodeGeometry ng) && Contains(ng.m_Geometry.m_Bounds, p))
                    {
                        NetCompositionData nc = m_CompositionData[comp.m_EndNode];
                        float hn = CheckNode(ng.m_Geometry, nc, p);
                        sb.Append(" endNode usable=").Append(UsableSurface(nc)).Append(" h=").Append(hn == float.MinValue ? "none" : hn.ToString("0.00"));
                    }
                }
                else
                {
                    sb.Append("other");
                }
                sb.Append(" | ");
            }
            return sb.ToString();
        }

        private void CollectNet(Bounds3 area)
        {
            m_Items.Clear();
            var it = new Collector { Area = area, Items = m_Items };
            m_NetTree.Iterate(ref it);
        }

        private struct Collector : INativeQuadTreeIterator<Entity, QuadTreeBoundsXZ>
        {
            public Bounds3 Area;
            public List<Entity> Items;
            public BoundsMask RequireMask;

            public bool Intersect(QuadTreeBoundsXZ bounds) => MathUtils.Intersect(bounds.m_Bounds.xz, Area.xz);

            public void Iterate(QuadTreeBoundsXZ bounds, Entity item)
            {
                if (!MathUtils.Intersect(bounds.m_Bounds.xz, Area.xz))
                {
                    return;
                }
                if (RequireMask != 0 && (bounds.m_Mask & RequireMask) == 0)
                {
                    return;
                }
                Items.Add(item);
            }
        }
    }
}
