using System;
using System.Collections.Generic;
using Colossal.Mathematics;
using DriveTheCityII.Diagnostics;
using Game.Common;
using Game.Net;
using Game.Pathfind;
using Game.Tools;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using CarLane = Game.Net.CarLane;
using CarLaneFlags = Game.Net.CarLaneFlags;
using SlaveLane = Game.Net.SlaveLane;
using ConnectionLane = Game.Net.ConnectionLane;

namespace DriveTheCityII.CityBridge
{
    public enum RouteStatusCode
    {
        Reachable = 0,
        Unreachable = 1,
        Unknown = 2,
    }

    public sealed class RouteResult
    {
        public RouteStatusCode Status = RouteStatusCode.Unknown;
        public double DistanceMeters;
        public double TimeSeconds;
        public readonly List<float3> Polyline = new List<float3>();
        public int Expansions;
        public float3 GoalPoint;
    }

    /// <summary>
    /// Driving routes over the city's car-lane graph (CityBridge route-planning port, SPEC-MODULES 4.2). Lanes connect
    /// when one lane's end PathNode equals the next lane's start PathNode, which encodes one-way streets and lane
    /// connections at intersections (refcache note 05 §C3). Slave lanes are not part of the graph, as in the game's own
    /// pathfinder. The graph is rebuilt lazily when the road network changes.
    /// </summary>
    public sealed class RoadRouter
    {
        private struct LaneNode
        {
            public Entity Lane;
            public PathNode Start;
            public PathNode End;
            public float Length;
            public float Speed;
            public Bezier4x3 Curve;
            public bool Forbidden;
        }

        private const float Cell = 64f;
        private readonly List<LaneNode> m_Lanes = new List<LaneNode>(65536);
        private readonly Dictionary<PathNode, List<int>> m_ByStart = new Dictionary<PathNode, List<int>>(65536);
        private readonly Dictionary<long, List<int>> m_Grid = new Dictionary<long, List<int>>(16384);
        private EntityQuery m_Query;
        private bool m_HasQuery;
        private int m_BuiltVersion = -1;
        private int m_BuiltCount = -1;
        private float m_BuiltTime;
        private int m_LastRevisionLaneCount = -1;

        /// <summary>Changes whenever the road network changed (used as the city revision for career revalidation).</summary>
        public int Revision { get; private set; }

        public int LaneCount => m_Lanes.Count;

        public void EnsureBuilt(EntityManager em)
        {
            if (!m_HasQuery)
            {
                m_Query = em.CreateEntityQuery(new EntityQueryDesc
                {
                    All = new[] { ComponentType.ReadOnly<Lane>(), ComponentType.ReadOnly<CarLane>(), ComponentType.ReadOnly<Curve>() },
                    None = new[] { ComponentType.ReadOnly<SlaveLane>(), ComponentType.ReadOnly<ConnectionLane>(), ComponentType.ReadOnly<Deleted>(), ComponentType.ReadOnly<Temp>() },
                });
                m_HasQuery = true;
            }
            // Lanes receive transient tags constantly in a live city, so the order version alone would trigger a rebuild
            // nearly every frame (observed: 2,589 rebuilds in 3 minutes). Rebuild when the number of car lanes changes
            // (a real road edit), or at most every 2 minutes when only the version changed.
            float now = UnityEngine.Time.realtimeSinceStartup;
            int version = m_Query.GetCombinedComponentOrderVersion();
            if (version == m_BuiltVersion)
            {
                return;
            }
            int count = m_Query.CalculateEntityCount();
            if (m_BuiltVersion >= 0 && count == m_BuiltCount && now - m_BuiltTime < 120f)
            {
                return;
            }
            m_BuiltCount = count;
            m_BuiltTime = now;
            var sw = System.Diagnostics.Stopwatch.StartNew();
            m_Query.CompleteDependency();
            m_Lanes.Clear();
            m_ByStart.Clear();
            m_Grid.Clear();
            using (NativeArray<Entity> ents = m_Query.ToEntityArray(Allocator.Temp))
            using (NativeArray<Lane> lanes = m_Query.ToComponentDataArray<Lane>(Allocator.Temp))
            using (NativeArray<CarLane> cars = m_Query.ToComponentDataArray<CarLane>(Allocator.Temp))
            using (NativeArray<Curve> curves = m_Query.ToComponentDataArray<Curve>(Allocator.Temp))
            {
                for (int i = 0; i < ents.Length; i++)
                {
                    CarLane cl = cars[i];
                    if ((cl.m_Flags & (CarLaneFlags.Runway)) != 0)
                    {
                        continue;
                    }
                    var node = new LaneNode
                    {
                        Lane = ents[i],
                        Start = lanes[i].m_StartNode,
                        End = lanes[i].m_EndNode,
                        Length = math.max(0.5f, curves[i].m_Length),
                        Speed = math.max(5f, cl.m_SpeedLimit),
                        Curve = curves[i].m_Bezier,
                        Forbidden = (cl.m_Flags & CarLaneFlags.Forbidden) != 0,
                    };
                    int idx = m_Lanes.Count;
                    m_Lanes.Add(node);
                    if (!m_ByStart.TryGetValue(node.Start, out List<int> list))
                    {
                        list = new List<int>(3);
                        m_ByStart[node.Start] = list;
                    }
                    list.Add(idx);
                    AddToGrid(idx, node.Curve);
                }
            }
            m_BuiltVersion = version;
            if (m_Lanes.Count != m_LastRevisionLaneCount)
            {
                m_LastRevisionLaneCount = m_Lanes.Count;
                Revision++;
            }
            DtcLog.Info($"RoadRouter: graph built with {m_Lanes.Count} car lanes in {sw.ElapsedMilliseconds} ms (revision {Revision})");
        }

        private void AddToGrid(int idx, Bezier4x3 c)
        {
            float2 min = math.min(math.min(c.a.xz, c.b.xz), math.min(c.c.xz, c.d.xz));
            float2 max = math.max(math.max(c.a.xz, c.b.xz), math.max(c.c.xz, c.d.xz));
            int x0 = (int)math.floor(min.x / Cell), x1 = (int)math.floor(max.x / Cell);
            int z0 = (int)math.floor(min.y / Cell), z1 = (int)math.floor(max.y / Cell);
            for (int x = x0; x <= x1; x++)
            {
                for (int z = z0; z <= z1; z++)
                {
                    long key = Key(x, z);
                    if (!m_Grid.TryGetValue(key, out List<int> l))
                    {
                        l = new List<int>(8);
                        m_Grid[key] = l;
                    }
                    l.Add(idx);
                }
            }
        }

        private static long Key(int x, int z) => ((long)x << 32) ^ (uint)z;

        /// <summary>Lanes whose curve passes within <paramref name="radius"/> of a point, with the curve parameter.</summary>
        private void LanesNear(float3 p, float radius, List<(int idx, float t, float d)> result)
        {
            result.Clear();
            int x0 = (int)math.floor((p.x - radius) / Cell), x1 = (int)math.floor((p.x + radius) / Cell);
            int z0 = (int)math.floor((p.z - radius) / Cell), z1 = (int)math.floor((p.z + radius) / Cell);
            var seen = new HashSet<int>();
            for (int x = x0; x <= x1; x++)
            {
                for (int z = z0; z <= z1; z++)
                {
                    if (!m_Grid.TryGetValue(Key(x, z), out List<int> l))
                    {
                        continue;
                    }
                    foreach (int idx in l)
                    {
                        if (!seen.Add(idx))
                        {
                            continue;
                        }
                        float d = MathUtils.Distance(m_Lanes[idx].Curve.xz, p.xz, out float t);
                        if (d <= radius && math.abs(MathUtils.Position(m_Lanes[idx].Curve, t).y - p.y) < 12f)
                        {
                            result.Add((idx, t, d));
                        }
                    }
                }
            }
        }

        /// <summary>
        /// A* from the lane nearest to <paramref name="from"/> (aligned with <paramref name="heading"/> when given) to any lane
        /// passing within <paramref name="goalRadius"/> of <paramref name="to"/>. Unknown is returned when the search budget
        /// runs out, never a guess (REQ-CAREER-002).
        /// </summary>
        public RouteResult FindRoute(EntityManager em, float3 from, float2 heading, float3 to, float goalRadius, int maxExpansions = 120000)
        {
            EnsureBuilt(em);
            var res = new RouteResult();
            var starts = new List<(int idx, float t, float d)>();
            LanesNear(from, 25f, starts);
            var goals = new List<(int idx, float t, float d)>();
            LanesNear(to, goalRadius, goals);
            if (starts.Count == 0 || goals.Count == 0)
            {
                res.Status = starts.Count == 0 || goals.Count == 0 ? RouteStatusCode.Unreachable : RouteStatusCode.Unknown;
                return res;
            }
            var goalT = new Dictionary<int, float>();
            foreach ((int idx, float t, float d) g in goals)
            {
                if (!goalT.TryGetValue(g.idx, out float old) || g.d < 0)
                {
                    goalT[g.idx] = g.t;
                }
            }
            bool useHeading = math.lengthsq(heading) > 0.01f;
            var gScore = new Dictionary<int, float>(4096);
            var cameFrom = new Dictionary<int, int>(4096);
            var startT = new Dictionary<int, float>();
            var open = new MinHeap();
            const float maxSpeed = 30f;
            foreach ((int idx, float t, float d) s in starts)
            {
                LaneNode n = m_Lanes[s.idx];
                float penalty = s.d * 0.5f;
                if (useHeading)
                {
                    float2 tan = math.normalizesafe(MathUtils.Tangent(n.Curve, s.t).xz);
                    float align = math.dot(tan, math.normalizesafe(heading));
                    if (align < 0f)
                    {
                        penalty += 30f; // allow U-turn-like starts but prefer the lane we face
                    }
                }
                float cost = (1f - s.t) * n.Length / n.Speed + penalty;
                if (!gScore.TryGetValue(s.idx, out float old) || cost < old)
                {
                    gScore[s.idx] = cost;
                    startT[s.idx] = s.t;
                    open.Push(cost + math.distance(n.Curve.d, to) / maxSpeed, s.idx);
                }
            }
            int found = -1;
            var closed = new HashSet<int>();
            while (open.Count > 0)
            {
                int cur = open.Pop();
                if (!closed.Add(cur))
                {
                    continue;
                }
                res.Expansions++;
                if (goalT.ContainsKey(cur))
                {
                    found = cur;
                    break;
                }
                if (res.Expansions > maxExpansions)
                {
                    res.Status = RouteStatusCode.Unknown;
                    return res;
                }
                LaneNode cn = m_Lanes[cur];
                if (!m_ByStart.TryGetValue(cn.End, out List<int> next))
                {
                    continue;
                }
                float gc = gScore[cur];
                foreach (int nx in next)
                {
                    if (closed.Contains(nx))
                    {
                        continue;
                    }
                    LaneNode nn = m_Lanes[nx];
                    float step = nn.Length / nn.Speed * (nn.Forbidden ? 5f : 1f);
                    float cand = gc + step;
                    if (!gScore.TryGetValue(nx, out float old) || cand < old)
                    {
                        gScore[nx] = cand;
                        cameFrom[nx] = cur;
                        open.Push(cand + math.distance(nn.Curve.d, to) / maxSpeed, nx);
                    }
                }
            }
            if (found < 0)
            {
                res.Status = RouteStatusCode.Unreachable;
                return res;
            }
            // Reconstruct.
            var chain = new List<int>();
            int c = found;
            chain.Add(c);
            while (cameFrom.TryGetValue(c, out int p))
            {
                c = p;
                chain.Add(c);
            }
            chain.Reverse();
            double dist = 0, time = 0;
            int junctions = 0;
            for (int i = 0; i < chain.Count; i++)
            {
                LaneNode n = m_Lanes[chain[i]];
                if (i > 0 && n.Length < 25f)
                {
                    junctions++; // short connecting lanes are intersection movements
                }
                float t0 = i == 0 && startT.TryGetValue(chain[i], out float st) ? st : 0f;
                float t1 = i == chain.Count - 1 ? goalT[chain[i]] : 1f;
                if (t1 < t0)
                {
                    t1 = t0;
                }
                float frac = t1 - t0;
                dist += n.Length * frac;
                time += n.Length * frac / n.Speed;
                int samples = math.max(2, (int)(n.Length * frac / 8f));
                for (int k = 0; k <= samples; k++)
                {
                    float t = math.lerp(t0, t1, k / (float)samples);
                    float3 pt = MathUtils.Position(n.Curve, t);
                    if (res.Polyline.Count == 0 || math.distancesq(res.Polyline[res.Polyline.Count - 1], pt) > 1f)
                    {
                        res.Polyline.Add(pt);
                    }
                }
            }
            res.DistanceMeters = dist;
            // Free-flow time at the speed limit is unrealistically short for a driver: calibrate for acceleration,
            // corners and traffic (x1.3) and for slowing down at intersections (3 s each). Observed on 2026-10-19:
            // the uncalibrated estimate made delivery deadlines expire before an on-route driver could finish.
            res.TimeSeconds = time * 1.3 + junctions * 3.0;
            res.GoalPoint = res.Polyline.Count > 0 ? res.Polyline[res.Polyline.Count - 1] : to;
            res.Status = RouteStatusCode.Reachable;
            return res;
        }

        /// <summary>Small binary min-heap of (priority, lane index).</summary>
        private sealed class MinHeap
        {
            private readonly List<(float p, int v)> m_Items = new List<(float, int)>(4096);

            public int Count => m_Items.Count;

            public void Push(float p, int v)
            {
                m_Items.Add((p, v));
                int i = m_Items.Count - 1;
                while (i > 0)
                {
                    int parent = (i - 1) / 2;
                    if (m_Items[parent].p <= m_Items[i].p)
                    {
                        break;
                    }
                    (m_Items[parent], m_Items[i]) = (m_Items[i], m_Items[parent]);
                    i = parent;
                }
            }

            public int Pop()
            {
                int result = m_Items[0].v;
                int last = m_Items.Count - 1;
                m_Items[0] = m_Items[last];
                m_Items.RemoveAt(last);
                int i = 0;
                while (true)
                {
                    int l = i * 2 + 1, r = l + 1, s = i;
                    if (l < m_Items.Count && m_Items[l].p < m_Items[s].p)
                    {
                        s = l;
                    }
                    if (r < m_Items.Count && m_Items[r].p < m_Items[s].p)
                    {
                        s = r;
                    }
                    if (s == i)
                    {
                        break;
                    }
                    (m_Items[s], m_Items[i]) = (m_Items[i], m_Items[s]);
                    i = s;
                }
                return result;
            }
        }
    }
}
