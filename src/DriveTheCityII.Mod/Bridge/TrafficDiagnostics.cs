using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Colossal.Mathematics;
using DriveTheCityII.CityBridge;
using Game.Common;
using Game.Net;
using Game.Objects;
using Game.Vehicles;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Transform = Game.Objects.Transform;

namespace DriveTheCityII.Bridge
{
    /// <summary>
    /// Traffic-jam diagnostics for ISSUE-033 (test tooling, read-only). Chains are followed the way the game's
    /// StuckMovingObjectSystem follows them: a blocking trailer stands for its tractor (Controller), and a chain ends at a
    /// vehicle with no blocker, a "temporary" blocker (a pedestrian), or a blocker it is not slowed below 6 by.
    /// </summary>
    public static class TrafficDiagnostics
    {
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        /// <summary>The next link: the blocker, or the tractor of a blocking trailer. Null when the chain ends here.</summary>
        public static Entity Next(EntityManager em, Entity e, out Blocker b, out bool hasBlocker)
        {
            hasBlocker = em.Exists(e) && em.HasComponent<Blocker>(e);
            b = hasBlocker ? em.GetComponentData<Blocker>(e) : default;
            Entity n = hasBlocker ? b.m_Blocker : Entity.Null;
            if (n != Entity.Null && em.Exists(n) && em.HasComponent<Controller>(n))
            {
                n = em.GetComponentData<Controller>(n).m_Controller;
            }
            return n;
        }

        public static string Who(EntityManager em, Entity e, Entity me)
        {
            if (e == me)
            {
                return "ME";
            }
            if (!em.Exists(e))
            {
                return "gone" + e.Index.ToString(Inv);
            }
            if (em.HasComponent<DtcActor>(e))
            {
                return "MOD" + e.Index.ToString(Inv);
            }
            string k = em.HasComponent<CarTrailer>(e) ? "T" : em.HasComponent<Game.Creatures.Creature>(e) ? "H" : em.HasComponent<ParkedCar>(e) ? "P" : string.Empty;
            return k + e.Index.ToString(Inv);
        }

        private static float Speed(EntityManager em, Entity e) =>
            em.Exists(e) && em.HasComponent<Moving>(e) ? math.length(em.GetComponentData<Moving>(e).m_Velocity) : -1f;

        private static float3 Pos(EntityManager em, Entity e) =>
            em.Exists(e) && em.HasComponent<Transform>(e) ? em.GetComponentData<Transform>(e).m_Position : float3.zero;

        /// <summary>One link: who, where, speed, blocker type and the speed the blocker allows (game units: m/s x 2.295).</summary>
        public static string Link(EntityManager em, Entity e, Entity me)
        {
            float3 p = Pos(em, e);
            string s = string.Format(Inv, "{0}@({1:0},{2:0}) v{3:0.0}", Who(em, e, me), p.x, p.z, Speed(em, e));
            if (em.Exists(e) && em.HasComponent<Blocker>(e))
            {
                Blocker b = em.GetComponentData<Blocker>(e);
                s += " " + b.m_Type + "/" + b.m_MaxSpeed.ToString(Inv);
                if (b.m_Blocker != Entity.Null && em.Exists(b.m_Blocker) && em.HasComponent<Controller>(b.m_Blocker))
                {
                    s += "(trailer of " + Who(em, em.GetComponentData<Controller>(b.m_Blocker).m_Controller, me) + ")";
                }
            }
            else
            {
                s += " noBlocker";
            }
            return s;
        }

        /// <summary>Lane, lane flags, path state and car flags of a vehicle (why a queue's head stands).</summary>
        public static string Detail(EntityManager em, Entity e)
        {
            if (!em.Exists(e))
            {
                return "gone";
            }
            var parts = new List<string>();
            if (em.HasComponent<CarCurrentLane>(e))
            {
                CarCurrentLane cl = em.GetComponentData<CarCurrentLane>(e);
                parts.Add(string.Format(Inv, "lane={0} t={1:0.00} change={2} flags={3}", cl.m_Lane.Index, cl.m_CurvePosition.x, cl.m_ChangeLane.Index, cl.m_LaneFlags));
                if (cl.m_Lane != Entity.Null && em.Exists(cl.m_Lane))
                {
                    if (em.HasComponent<Game.Net.CarLane>(cl.m_Lane))
                    {
                        parts.Add("laneFlags=" + em.GetComponentData<Game.Net.CarLane>(cl.m_Lane).m_Flags);
                    }
                    if (em.HasComponent<LaneSignal>(cl.m_Lane))
                    {
                        LaneSignal ls = em.GetComponentData<LaneSignal>(cl.m_Lane);
                        parts.Add("signal=" + ls.m_Signal + "/blocker" + ls.m_Blocker.Index);
                    }
                }
            }
            if (em.HasComponent<Game.Pathfind.PathOwner>(e))
            {
                parts.Add("path=" + em.GetComponentData<Game.Pathfind.PathOwner>(e).m_State);
            }
            if (em.HasComponent<Car>(e))
            {
                parts.Add("car=" + em.GetComponentData<Car>(e).m_Flags);
            }
            if (em.HasBuffer<CarNavigationLane>(e))
            {
                DynamicBuffer<CarNavigationLane> nav = em.GetBuffer<CarNavigationLane>(e, true);
                var next = new List<string>();
                for (int i = 0; i < nav.Length && i < 3; i++)
                {
                    Entity l = nav[i].m_Lane;
                    string r = string.Empty;
                    if (em.Exists(l) && em.HasComponent<LaneReservation>(l))
                    {
                        LaneReservation lr = em.GetComponentData<LaneReservation>(l);
                        r = string.Format(Inv, "[res {0:0.00}/{1} by {2}]", lr.GetOffset(), lr.GetPriority(), lr.m_Blocker.Index);
                    }
                    next.Add(l.Index.ToString(Inv) + ":" + nav[i].m_Flags + r);
                }
                parts.Add("next=" + string.Join(",", next));
            }
            return string.Join(" ", parts);
        }

        /// <summary>The chain from <paramref name="start"/>, link by link, with the details of its last vehicle.</summary>
        public static string Chain(EntityManager em, Entity start, Entity me, int maxHops = 80)
        {
            var parts = new List<string>();
            var seen = new HashSet<Entity>();
            Entity cur = start;
            Entity last = start;
            string end = "end";
            for (int hop = 0; hop < maxHops && cur != Entity.Null; hop++)
            {
                if (!seen.Add(cur))
                {
                    end = "LOOP back to " + Who(em, cur, me);
                    break;
                }
                parts.Add(Link(em, cur, me));
                last = cur;
                cur = Next(em, cur, out _, out _);
            }
            return string.Format(Inv, "{0} links ({1}): {2} || head {3}", parts.Count, end, string.Join(" > ", parts), Detail(em, last));
        }

        /// <summary>
        /// Every stopped native car in the city and what its chain ends at: one of our vehicles, a loop, or a vehicle
        /// that is not blocked by another (with the queue size behind each such head).
        /// </summary>
        public static string Jams(EntityManager em, Entity me, int top = 5)
        {
            using (EntityQuery q = em.CreateEntityQuery(new EntityQueryDesc
            {
                All = new[] { ComponentType.ReadOnly<Car>(), ComponentType.ReadOnly<Blocker>(), ComponentType.ReadOnly<Moving>() },
                None = new[] { ComponentType.ReadOnly<Deleted>(), ComponentType.ReadOnly<Game.Tools.Temp>() },
            }))
            using (NativeArray<Entity> cars = q.ToEntityArray(Allocator.Temp))
            {
                int stopped = 0, toOurs = 0, inLoop = 0;
                var heads = new Dictionary<Entity, int>();
                var headEnd = new Dictionary<Entity, string>();
                var ours = new Dictionary<Entity, int>();
                var chain = new List<Entity>(64);
                foreach (Entity e in cars)
                {
                    if (em.HasComponent<DtcActor>(e) || Speed(em, e) > 0.5f)
                    {
                        continue;
                    }
                    stopped++;
                    chain.Clear();
                    Entity cur = e;
                    Entity head = e;
                    string end = "free";
                    bool reachedOurs = false;
                    for (int hop = 0; hop < 300; hop++)
                    {
                        chain.Add(cur);
                        Entity nx = Next(em, cur, out Blocker b, out bool hb);
                        if (!hb)
                        {
                            end = "noBlocker";
                            break;
                        }
                        if (nx == Entity.Null)
                        {
                            end = "free:" + b.m_Type;
                            break;
                        }
                        if (b.m_Type == BlockerType.Temporary)
                        {
                            end = "pedestrian";
                            break;
                        }
                        if (b.m_MaxSpeed >= 6)
                        {
                            end = "slowedOnly";
                            break;
                        }
                        if (em.HasComponent<DtcActor>(nx) && !reachedOurs)
                        {
                            reachedOurs = true;
                            ours[nx] = ours.TryGetValue(nx, out int n0) ? n0 + 1 : 1;
                        }
                        if (chain.Contains(nx))
                        {
                            end = "loop";
                            break;
                        }
                        cur = nx;
                    }
                    head = cur;
                    if (reachedOurs)
                    {
                        toOurs++;
                    }
                    if (end == "loop")
                    {
                        inLoop++;
                    }
                    heads[head] = heads.TryGetValue(head, out int n) ? n + 1 : 1;
                    headEnd[head] = end;
                }
                var tops = heads.Where(kv => kv.Value >= 3).OrderByDescending(kv => kv.Value).Take(top)
                    .Select(kv => string.Format(Inv, "[{0} behind {1} end={2} {3}]", kv.Value, Link(em, kv.Key, me), headEnd[kv.Key], Detail(em, kv.Key)));
                string oursText = string.Join(" ", ours.Select(kv => Who(em, kv.Key, me) + ":" + kv.Value.ToString(Inv)));
                return string.Format(Inv, "cars={0} stopped={1} chainsThroughOurs={2} ({3}) chainsInLoop={4} queues>=3: {5}",
                    cars.Length, stopped, toOurs, oursText, inLoop, string.Join(" ", tops));
            }
        }

        /// <summary>How many stopped native cars have a "blocked by" chain that reaches <paramref name="target"/>.</summary>
        public static int ChainsThrough(EntityManager em, Entity target)
        {
            using (EntityQuery q = em.CreateEntityQuery(new EntityQueryDesc
            {
                All = new[] { ComponentType.ReadOnly<Car>(), ComponentType.ReadOnly<Blocker>(), ComponentType.ReadOnly<Moving>() },
                None = new[] { ComponentType.ReadOnly<Deleted>(), ComponentType.ReadOnly<Game.Tools.Temp>() },
            }))
            using (NativeArray<Entity> cars = q.ToEntityArray(Allocator.Temp))
            {
                int n = 0;
                var seen = new HashSet<Entity>();
                foreach (Entity e in cars)
                {
                    if (e == target || em.HasComponent<DtcActor>(e) || Speed(em, e) > 0.5f)
                    {
                        continue;
                    }
                    seen.Clear();
                    Entity cur = Next(em, e, out _, out _);
                    while (cur != Entity.Null && seen.Add(cur) && seen.Count < 300)
                    {
                        if (cur == target)
                        {
                            n++;
                            break;
                        }
                        cur = Next(em, cur, out _, out _);
                    }
                }
                return n;
            }
        }

        /// <summary>
        /// Lane-object entries that do not match where their vehicle is (more than 25 m from the registered lane point),
        /// entries for vehicles that no longer exist, and every entry for one of our vehicles.
        /// </summary>
        public static string Ghosts(EntityManager em, Entity me, int show = 8)
        {
            using (EntityQuery q = em.CreateEntityQuery(new EntityQueryDesc
            {
                All = new[] { ComponentType.ReadOnly<Lane>(), ComponentType.ReadOnly<Curve>(), ComponentType.ReadOnly<LaneObject>() },
                None = new[] { ComponentType.ReadOnly<Deleted>(), ComponentType.ReadOnly<Game.Tools.Temp>() },
            }))
            using (NativeArray<Entity> lanes = q.ToEntityArray(Allocator.Temp))
            {
                int entries = 0, dead = 0, ghosts = 0, oursN = 0;
                var shown = new List<string>();
                var oursShown = new List<string>();
                foreach (Entity lane in lanes)
                {
                    DynamicBuffer<LaneObject> objs = em.GetBuffer<LaneObject>(lane, true);
                    if (objs.Length == 0)
                    {
                        continue;
                    }
                    Bezier4x3 c = em.GetComponentData<Curve>(lane).m_Bezier;
                    for (int i = 0; i < objs.Length; i++)
                    {
                        Entity o = objs[i].m_LaneObject;
                        entries++;
                        if (!em.Exists(o))
                        {
                            dead++;
                            continue;
                        }
                        if (!em.HasComponent<Vehicle>(o) || !em.HasComponent<Transform>(o))
                        {
                            continue;
                        }
                        float3 lp = MathUtils.Position(c, math.saturate(objs[i].m_CurvePosition.x));
                        float d = math.distance(lp.xz, em.GetComponentData<Transform>(o).m_Position.xz);
                        bool mine = em.HasComponent<DtcActor>(o);
                        if (mine)
                        {
                            oursN++;
                            if (oursShown.Count < show)
                            {
                                oursShown.Add(string.Format(Inv, "{0} on lane {1} t={2:0.00} {3:0.0}m off", Who(em, o, me), lane.Index, objs[i].m_CurvePosition.x, d));
                            }
                        }
                        if (d > 25f)
                        {
                            ghosts++;
                            if (shown.Count < show)
                            {
                                shown.Add(string.Format(Inv, "{0} on lane {1} at ({2:0},{3:0}) but {4:0}m away", Who(em, o, me), lane.Index, lp.x, lp.z, d));
                            }
                        }
                    }
                }
                return string.Format(Inv, "lanes={0} entries={1} dead={2} ghosts>25m={3} [{4}] ours={5} [{6}]",
                    lanes.Length, entries, dead, ghosts, string.Join("; ", shown), oursN, string.Join("; ", oursShown));
            }
        }
    }
}
