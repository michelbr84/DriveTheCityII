using System;
using System.Collections.Generic;
using System.Globalization;
using Colossal.Mathematics;
using DriveTheCityII.Diagnostics;
using Game.Buildings;
using Game.Common;
using Game.Objects;
using Game.Prefabs;
using Game.Routes;
using Game.Tools;
using Game.UI;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Transform = Game.Objects.Transform;
using TransportStop = Game.Routes.TransportStop;
using ServiceUpgrade = Game.Buildings.ServiceUpgrade;

namespace DriveTheCityII.CityBridge
{
    /// <summary>Place categories used by careers (mirrors Core.Career.PlaceKind by name).</summary>
    public enum CityPlaceKind
    {
        Residential,
        Commercial,
        Office,
        Industrial,
        Warehouse,
        Service,
        Leisure,
        TransitStop,
        ServicePoint,
        Home,
        Depot,
        Other,
    }

    /// <summary>A usable mission place: a real building or stop with a roadside access point (vehicle stop point).</summary>
    public sealed class CityPlace
    {
        public string Id;
        public CityPlaceKind Kind;
        public string Name;
        public Entity Entity;
        public float3 BuildingPos;

        /// <summary>Where a vehicle stops: a point on the curb-side road lane in front of the place (refined lazily).</summary>
        public float3 RoadAccess;

        /// <summary>
        /// The place's stable location for Career: the point on the car lane of the building's road edge that is closest
        /// to the building (the curb-side lane), next to the building's connection. It is computed from the edge's own
        /// lanes when the index is built, so it does not change while the building and road exist, and it lies on a
        /// drivable lane (arrival radii are as small as 8 m for freight docks and bus stops).
        /// </summary>
        public float3 BaseAccess;

        /// <summary>Where a person waits: the nearest sidewalk point.</summary>
        public float3 WalkAccess;

        public bool HasRoadAccess;
        public string TransportKind; // depots: "taxi"/"bus"; service points: "parking"
    }

    public sealed class CityBusLine
    {
        public string Id;
        public string Name;
        public int Number;
        public Entity Route;
        public readonly List<CityPlace> Stops = new List<CityPlace>();
    }

    /// <summary>
    /// Index of real city places for careers (CityBridge city-query port). Built lazily, refreshed when buildings change.
    /// Ids are stable across save/load: prefab name plus rounded position (entity handles are never persisted).
    /// CS2 has no fuel stations; parking facilities serve as the mod's service points (garage, fuel, repair, rental).
    /// </summary>
    public sealed class PlaceIndex
    {
        private readonly List<CityPlace> m_Places = new List<CityPlace>(8192);
        private readonly Dictionary<string, CityPlace> m_ById = new Dictionary<string, CityPlace>(8192);
        private readonly List<CityBusLine> m_Lines = new List<CityBusLine>();
        private EntityQuery m_BuildingQuery;
        private EntityQuery m_RouteQuery;
        private bool m_HasQueries;
        private int m_BuiltVersion = -1;
        private int m_RoutesVersion = -1;
        private float m_NextCheck;
        private int m_BuiltCount = -1;
        private float m_BuiltTime;
        private int m_RoutesCount = -1;
        private float m_RoutesTime;

        public int Revision { get; private set; }
        public IReadOnlyList<CityPlace> Places => m_Places;
        public IReadOnlyList<CityBusLine> BusLines => m_Lines;

        public bool TryGet(string id, out CityPlace place) => m_ById.TryGetValue(id, out place);

        public void EnsureBuilt(EntityManager em, CityWorldProbe probe, NameSystem names, float now)
        {
            if (!m_HasQueries)
            {
                m_BuildingQuery = em.CreateEntityQuery(new EntityQueryDesc
                {
                    All = new[] { ComponentType.ReadOnly<Building>(), ComponentType.ReadOnly<Transform>(), ComponentType.ReadOnly<PrefabRef>() },
                    None = new[]
                    {
                        ComponentType.ReadOnly<Abandoned>(), ComponentType.ReadOnly<Condemned>(), ComponentType.ReadOnly<Destroyed>(),
                        ComponentType.ReadOnly<UnderConstruction>(), ComponentType.ReadOnly<Deleted>(), ComponentType.ReadOnly<Temp>(),
                        ComponentType.ReadOnly<ServiceUpgrade>(),
                    },
                });
                m_RouteQuery = em.CreateEntityQuery(new EntityQueryDesc
                {
                    All = new[] { ComponentType.ReadOnly<Route>(), ComponentType.ReadOnly<TransportLine>(), ComponentType.ReadOnly<RouteWaypoint>(), ComponentType.ReadOnly<PrefabRef>() },
                    None = new[] { ComponentType.ReadOnly<Deleted>(), ComponentType.ReadOnly<Temp>() },
                });
                m_HasQueries = true;
            }
            if (now < m_NextCheck && m_BuiltVersion >= 0)
            {
                return;
            }
            m_NextCheck = now + 5f;
            // Same policy as the road graph: rebuild on a real change in the number of buildings/lines, or at most
            // every 3 minutes when only transient tags changed the query version.
            int v = m_BuildingQuery.GetCombinedComponentOrderVersion();
            int rv = m_RouteQuery.GetCombinedComponentOrderVersion();
            int count = m_BuildingQuery.CalculateEntityCount();
            int rcount = m_RouteQuery.CalculateEntityCount();
            if (v != m_BuiltVersion && (m_BuiltVersion < 0 || count != m_BuiltCount || now - m_BuiltTime > 180f))
            {
                BuildPlaces(em, probe, names);
                m_BuiltVersion = v;
                m_BuiltTime = now;
                if (count != m_BuiltCount)
                {
                    Revision++;
                }
                m_BuiltCount = count;
            }
            if (rv != m_RoutesVersion && (m_RoutesVersion < 0 || rcount != m_RoutesCount || now - m_RoutesTime > 60f))
            {
                BuildLines(em, probe, names);
                m_RoutesVersion = rv;
                m_RoutesTime = now;
                if (rcount != m_RoutesCount)
                {
                    Revision++;
                }
                m_RoutesCount = rcount;
            }
        }

        private void BuildPlaces(EntityManager em, CityWorldProbe probe, NameSystem names)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            m_BuildingQuery.CompleteDependency();
            m_Places.RemoveAll(p => p.Kind != CityPlaceKind.TransitStop);
            var keep = new Dictionary<string, CityPlace>();
            foreach (KeyValuePair<string, CityPlace> kv in m_ById)
            {
                if (kv.Value.Kind == CityPlaceKind.TransitStop)
                {
                    keep[kv.Key] = kv.Value;
                }
            }
            m_ById.Clear();
            foreach (KeyValuePair<string, CityPlace> kv in keep)
            {
                m_ById[kv.Key] = kv.Value;
            }
            PrefabSystem prefabs = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<PrefabSystem>();
            int skipped = 0;
            using (NativeArray<Entity> ents = m_BuildingQuery.ToEntityArray(Allocator.Temp))
            {
                foreach (Entity e in ents)
                {
                    Building b = em.GetComponentData<Building>(e);
                    if (b.m_RoadEdge == Entity.Null)
                    {
                        skipped++;
                        continue;
                    }
                    CityPlaceKind kind = Classify(em, e, out string transport);
                    if (kind == CityPlaceKind.Other)
                    {
                        continue;
                    }
                    Transform tr = em.GetComponentData<Transform>(e);
                    Entity prefab = em.GetComponentData<PrefabRef>(e).m_Prefab;
                    string prefabName = prefabs != null && prefabs.TryGetPrefab(em.GetComponentData<PrefabData>(prefab), out PrefabBase pb) ? pb.name : "p" + prefab.Index;
                    string id = "bld:" + prefabName + ":" + ((int)math.round(tr.m_Position.x)).ToString(CultureInfo.InvariantCulture) + ":" + ((int)math.round(tr.m_Position.z)).ToString(CultureInfo.InvariantCulture);
                    if (m_ById.ContainsKey(id))
                    {
                        continue;
                    }
                    var place = new CityPlace
                    {
                        Id = id,
                        Kind = kind,
                        Entity = e,
                        BuildingPos = tr.m_Position,
                        TransportKind = transport,
                        Name = Address(em, names, e) ?? prefabName,
                    };
                    // Access: the road point at the building's connection, refined to lanes when queried (lazily).
                    if (em.HasComponent<Game.Net.Curve>(b.m_RoadEdge))
                    {
                        Bezier4x3 edge = em.GetComponentData<Game.Net.Curve>(b.m_RoadEdge).m_Bezier;
                        place.RoadAccess = MathUtils.Position(edge, b.m_CurvePosition);
                        place.BaseAccess = CurbAccess(em, b.m_RoadEdge, place.RoadAccess, tr.m_Position);
                        place.WalkAccess = place.RoadAccess;
                    }
                    m_Places.Add(place);
                    m_ById[id] = place;
                }
            }
            DtcLog.Info($"PlaceIndex: {m_Places.Count} places indexed ({skipped} unconnected skipped) in {sw.ElapsedMilliseconds} ms");
        }

        /// <summary>Refines a place's access points to real lanes (curb-side road lane and sidewalk); cached.</summary>
        public void RefineAccess(CityWorldProbe probe, CityPlace p)
        {
            if (p.HasRoadAccess || !probe.IsReady)
            {
                return;
            }
            float3 center = p.RoadAccess;
            float3 toBuilding = math.normalizesafe(new float3(p.BuildingPos.x - center.x, 0f, p.BuildingPos.z - center.z));
            // The curb-side lane is between the centreline and the building: probe a few metres towards the building.
            float3 probePoint = center + toBuilding * 3f;
            if (probe.FindNearestLanePoint(probePoint, 12f, CityWorldProbe.LaneKind.Road, out float3 road, out _, out _))
            {
                p.RoadAccess = road;
                p.HasRoadAccess = true;
            }
            if (probe.FindNearestLanePoint(center + toBuilding * 7f, 20f, CityWorldProbe.LaneKind.Sidewalk, out float3 walk, out _, out _))
            {
                p.WalkAccess = walk;
            }
        }

        /// <summary>
        /// Closest point to the building on the car lanes of its road edge, near the edge connection point (individual
        /// lanes, not master lanes; unsafe, side-connection, runway and forbidden lanes are skipped). Falls back to the
        /// edge point when the edge has no usable car lane.
        /// </summary>
        private static float3 CurbAccess(EntityManager em, Entity edge, float3 edgePoint, float3 buildingPos)
        {
            if (!em.HasBuffer<Game.Net.SubLane>(edge))
            {
                return edgePoint;
            }
            DynamicBuffer<Game.Net.SubLane> subs = em.GetBuffer<Game.Net.SubLane>(edge, true);
            const Game.Net.CarLaneFlags bad = Game.Net.CarLaneFlags.Unsafe | Game.Net.CarLaneFlags.SideConnection |
                                               Game.Net.CarLaneFlags.Runway | Game.Net.CarLaneFlags.Forbidden;
            float best = float.MaxValue;
            float3 result = edgePoint;
            for (int i = 0; i < subs.Length; i++)
            {
                Entity lane = subs[i].m_SubLane;
                if (!em.HasComponent<Game.Net.CarLane>(lane) || em.HasComponent<Game.Net.MasterLane>(lane) ||
                    em.HasComponent<Game.Net.ConnectionLane>(lane) || !em.HasComponent<Game.Net.Curve>(lane))
                {
                    continue;
                }
                if ((em.GetComponentData<Game.Net.CarLane>(lane).m_Flags & bad) != 0)
                {
                    continue;
                }
                Bezier4x3 curve = em.GetComponentData<Game.Net.Curve>(lane).m_Bezier;
                MathUtils.Distance(curve, edgePoint, out float t);
                float3 p = MathUtils.Position(curve, math.clamp(t, 0.05f, 0.95f));
                float d = math.distancesq(p.xz, buildingPos.xz);
                if (d < best)
                {
                    best = d;
                    result = p;
                }
            }
            return result;
        }

        private static string Address(EntityManager em, NameSystem names, Entity building)
        {
            try
            {
                if (names != null && BuildingUtils.GetAddress(em, building, out Entity road, out int number) && road != Entity.Null)
                {
                    string street = names.GetRenderedLabelName(road);
                    if (!string.IsNullOrEmpty(street))
                    {
                        return number.ToString(CultureInfo.InvariantCulture) + " " + street;
                    }
                }
            }
            catch (Exception)
            {
            }
            return null;
        }

        private static CityPlaceKind Classify(EntityManager em, Entity e, out string transport)
        {
            transport = null;
            if (em.HasComponent<Game.Buildings.ParkingFacility>(e))
            {
                transport = "parking";
                return CityPlaceKind.ServicePoint;
            }
            if (em.HasComponent<Game.Buildings.TransportDepot>(e))
            {
                Entity prefab = em.GetComponentData<PrefabRef>(e).m_Prefab;
                if (em.HasComponent<TransportDepotData>(prefab))
                {
                    TransportType tt = em.GetComponentData<TransportDepotData>(prefab).m_TransportType;
                    transport = tt == TransportType.Taxi ? "taxi" : tt == TransportType.Bus ? "bus" : tt.ToString().ToLowerInvariant();
                }
                return CityPlaceKind.Depot;
            }
            if (em.HasComponent<ResidentialProperty>(e))
            {
                return CityPlaceKind.Residential;
            }
            if (em.HasComponent<OfficeProperty>(e))
            {
                return CityPlaceKind.Office;
            }
            if (em.HasComponent<CommercialProperty>(e))
            {
                return CityPlaceKind.Commercial;
            }
            if (em.HasComponent<IndustrialProperty>(e))
            {
                return em.HasComponent<StorageProperty>(e) ? CityPlaceKind.Warehouse : CityPlaceKind.Industrial;
            }
            if (em.HasComponent<Game.Buildings.Park>(e))
            {
                return CityPlaceKind.Leisure;
            }
            if (em.HasComponent<Game.Buildings.School>(e) || em.HasComponent<Game.Buildings.Hospital>(e) ||
                em.HasComponent<Game.Buildings.PoliceStation>(e) || em.HasComponent<Game.Buildings.FireStation>(e) ||
                em.HasComponent<Game.Buildings.AdminBuilding>(e) || em.HasComponent<Game.Buildings.PostFacility>(e))
            {
                return CityPlaceKind.Service;
            }
            return CityPlaceKind.Other;
        }

        private void BuildLines(EntityManager em, CityWorldProbe probe, NameSystem names)
        {
            m_RouteQuery.CompleteDependency();
            m_Lines.Clear();
            m_Places.RemoveAll(p => p.Kind == CityPlaceKind.TransitStop);
            var stopIds = new List<string>();
            foreach (KeyValuePair<string, CityPlace> kv in m_ById)
            {
                if (kv.Value.Kind == CityPlaceKind.TransitStop)
                {
                    stopIds.Add(kv.Key);
                }
            }
            foreach (string s in stopIds)
            {
                m_ById.Remove(s);
            }
            using (NativeArray<Entity> routes = m_RouteQuery.ToEntityArray(Allocator.Temp))
            {
                foreach (Entity r in routes)
                {
                    Entity prefab = em.GetComponentData<PrefabRef>(r).m_Prefab;
                    if (!em.HasComponent<TransportLineData>(prefab) || em.GetComponentData<TransportLineData>(prefab).m_TransportType != TransportType.Bus)
                    {
                        continue;
                    }
                    int number = em.HasComponent<RouteNumber>(r) ? em.GetComponentData<RouteNumber>(r).m_Number : 0;
                    string lineName = SafeName(names, r);
                    if (string.IsNullOrEmpty(lineName) || lineName.IndexOf("Tool", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        lineName = "Line " + number.ToString(CultureInfo.InvariantCulture);
                    }
                    var line = new CityBusLine
                    {
                        Id = "line:" + number.ToString(CultureInfo.InvariantCulture) + ":" + lineName,
                        Name = lineName,
                        Number = number,
                        Route = r,
                    };
                    DynamicBuffer<RouteWaypoint> wps = em.GetBuffer<RouteWaypoint>(r, true);
                    for (int i = 0; i < wps.Length; i++)
                    {
                        Entity wp = wps[i].m_Waypoint;
                        if (!em.HasComponent<Connected>(wp))
                        {
                            continue;
                        }
                        Entity stop = em.GetComponentData<Connected>(wp).m_Connected;
                        if (stop == Entity.Null || !em.HasComponent<TransportStop>(stop))
                        {
                            continue;
                        }
                        float3 pos = em.HasComponent<Position>(wp) ? em.GetComponentData<Position>(wp).m_Position :
                            em.HasComponent<Transform>(stop) ? em.GetComponentData<Transform>(stop).m_Position : float3.zero;
                        if (em.HasComponent<RouteLane>(wp))
                        {
                            RouteLane rl = em.GetComponentData<RouteLane>(wp);
                            if (rl.m_EndLane != Entity.Null && em.HasComponent<Game.Net.Curve>(rl.m_EndLane))
                            {
                                pos = MathUtils.Position(em.GetComponentData<Game.Net.Curve>(rl.m_EndLane).m_Bezier, rl.m_EndCurvePos);
                            }
                        }
                        var sp = new CityPlace
                        {
                            Id = "stop:" + number.ToString(CultureInfo.InvariantCulture) + ":" + line.Stops.Count.ToString(CultureInfo.InvariantCulture) + ":" + ((int)math.round(pos.x)).ToString(CultureInfo.InvariantCulture) + ":" + ((int)math.round(pos.z)).ToString(CultureInfo.InvariantCulture),
                            Kind = CityPlaceKind.TransitStop,
                            Entity = stop,
                            BuildingPos = pos,
                            RoadAccess = pos,
                            BaseAccess = pos,
                            WalkAccess = pos,
                            HasRoadAccess = true,
                            Name = SafeName(names, stop) ?? ("Stop " + (line.Stops.Count + 1).ToString(CultureInfo.InvariantCulture)),
                            TransportKind = "bus",
                        };
                        line.Stops.Add(sp);
                        m_Places.Add(sp);
                        m_ById[sp.Id] = sp;
                    }
                    if (line.Stops.Count >= 2)
                    {
                        m_Lines.Add(line);
                    }
                }
            }
            DtcLog.Info($"PlaceIndex: {m_Lines.Count} bus lines indexed");
        }

        private static string SafeName(NameSystem names, Entity e)
        {
            try
            {
                string n = names?.GetRenderedLabelName(e);
                return string.IsNullOrEmpty(n) ? null : n;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
