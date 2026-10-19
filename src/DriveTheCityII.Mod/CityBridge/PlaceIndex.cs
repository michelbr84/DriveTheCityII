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

        /// <summary>Where a vehicle stops: a point on the curb-side road lane in front of the place.</summary>
        public float3 RoadAccess;

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
            int v = m_BuildingQuery.GetCombinedComponentOrderVersion();
            int rv = m_RouteQuery.GetCombinedComponentOrderVersion();
            if (v != m_BuiltVersion)
            {
                BuildPlaces(em, probe, names);
                m_BuiltVersion = v;
                Revision++;
            }
            if (rv != m_RoutesVersion)
            {
                BuildLines(em, probe, names);
                m_RoutesVersion = rv;
                Revision++;
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
