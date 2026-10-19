using System;
using System.Collections.Generic;
using DriveTheCityII.CityBridge;
using DriveTheCityII.Core.Career;
using DriveTheCityII.Diagnostics;
using Game.Common;
using Unity.Entities;
using Unity.Mathematics;

namespace DriveTheCityII.Careers
{
    /// <summary>
    /// ICareerWorld over the live city (CityBridge adapter, SPEC-MODULES 4.2): real places with curb-side access points,
    /// road routes from the lane graph (never guessed: Unknown when the search budget runs out), and real bus lines.
    /// </summary>
    public sealed class CareerWorldAdapter : ICareerWorld
    {
        private readonly CityWorldProbe m_Probe;
        private readonly Dictionary<string, (RouteEstimate est, float time, long rev)> m_RouteCache = new Dictionary<string, (RouteEstimate, float, long)>();
        private readonly List<PlaceInfo> m_Scratch = new List<PlaceInfo>(64);

        public CareerWorldAdapter(CityWorldProbe probe)
        {
            m_Probe = probe;
        }

        public float Now;
        public int EstimatesThisFrame;

        public long CityRevision => CityServices.CityRevision;

        private static EntityManager EM => World.DefaultGameObjectInjectionWorld.EntityManager;

        public static PlaceKind ToKind(CityPlaceKind k) => (PlaceKind)Enum.Parse(typeof(PlaceKind), k.ToString());

        public static MapPoint ToPoint(float3 p) => new MapPoint(p.x, p.y, p.z);

        public static float3 ToF3(MapPoint p) => new float3((float)p.X, (float)p.Y, (float)p.Z);

        public PlaceInfo ToInfo(CityPlace p)
        {
            CityServices.Places.RefineAccess(m_Probe, p);
            return new PlaceInfo(p.Id, ToKind(p.Kind), p.Name, ToPoint(p.RoadAccess));
        }

        public IReadOnlyList<PlaceInfo> QueryPlaces(PlaceQuery query)
        {
            m_Scratch.Clear();
            float3 near = ToF3(query.Near);
            double r2 = query.RadiusMeters > 0 ? query.RadiusMeters * query.RadiusMeters : double.MaxValue;
            int max = query.MaxCount > 0 ? query.MaxCount : 64;
            // Collect candidates in range, then keep a spread: the nearest ones and some farther ones.
            var candidates = new List<(CityPlace p, float d)>();
            foreach (CityPlace p in CityServices.Places.Places)
            {
                if (!query.Accepts(ToKind(p.Kind)))
                {
                    continue;
                }
                float d2 = math.distancesq(p.RoadAccess.xz, near.xz);
                if (d2 <= r2)
                {
                    candidates.Add((p, d2));
                }
            }
            candidates.Sort((a, b) => a.d.CompareTo(b.d));
            int step = Math.Max(1, candidates.Count / Math.Max(1, max));
            for (int i = 0; i < candidates.Count && m_Scratch.Count < max; i += step)
            {
                m_Scratch.Add(ToInfo(candidates[i].p));
            }
            return m_Scratch.ToArray();
        }

        public PlaceInfo Describe(string placeId)
        {
            if (string.IsNullOrEmpty(placeId) || !CityServices.Places.TryGet(placeId, out CityPlace p))
            {
                return null;
            }
            EntityManager em = EM;
            if (p.Entity == Entity.Null || !em.Exists(p.Entity) || em.HasComponent<Deleted>(p.Entity))
            {
                return null;
            }
            return ToInfo(p);
        }

        public RouteEstimate Estimate(MapPoint from, string placeId, VehicleClass vehicleClass)
        {
            if (!CityServices.Places.TryGet(placeId, out CityPlace p))
            {
                return RouteEstimate.Unreachable();
            }
            CityServices.Places.RefineAccess(m_Probe, p);
            float3 f = ToF3(from);
            string key = ((int)(f.x / 40f)).ToString() + ":" + ((int)(f.z / 40f)).ToString() + ">" + placeId;
            long rev = CityServices.CityRevision;
            if (m_RouteCache.TryGetValue(key, out var hit) && hit.rev == rev && Now - hit.time < 90f)
            {
                return hit.est;
            }
            if (EstimatesThisFrame >= 24)
            {
                return RouteEstimate.Unknown(); // budget: never stall a frame; Career will not offer Unknown
            }
            EstimatesThisFrame++;
            RouteResult r;
            try
            {
                r = CityServices.Router.FindRoute(EM, f, float2.zero, p.RoadAccess, 18f, 60000);
            }
            catch (Exception ex)
            {
                DtcLog.Throttled("estimate", Now, 10f, "Route estimate failed: " + ex.Message, warn: true);
                return RouteEstimate.Unknown();
            }
            RouteEstimate est = r.Status == RouteStatusCode.Reachable
                ? RouteEstimate.Reachable(r.DistanceMeters, r.TimeSeconds)
                : r.Status == RouteStatusCode.Unreachable ? RouteEstimate.Unreachable() : RouteEstimate.Unknown();
            m_RouteCache[key] = (est, Now, rev);
            if (m_RouteCache.Count > 4000)
            {
                m_RouteCache.Clear();
            }
            return est;
        }

        public IReadOnlyList<TransitLineInfo> BusLines()
        {
            var list = new List<TransitLineInfo>();
            foreach (CityBusLine l in CityServices.Places.BusLines)
            {
                var stops = new List<PlaceInfo>(l.Stops.Count);
                foreach (CityPlace s in l.Stops)
                {
                    stops.Add(new PlaceInfo(s.Id, PlaceKind.TransitStop, s.Name, ToPoint(s.RoadAccess)));
                }
                list.Add(new TransitLineInfo(l.Id, l.Name, l.Number, stops));
            }
            return list;
        }
    }
}
