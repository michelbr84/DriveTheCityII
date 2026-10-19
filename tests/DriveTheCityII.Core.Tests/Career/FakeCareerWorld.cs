using System.Collections.Generic;
using DriveTheCityII.Core.Career;

namespace DriveTheCityII.Core.Tests.Career
{
    /// <summary>
    /// In-memory city for career tests: places with access points, a reachability table (unreachable, unknown and
    /// per-class blocked places), removable places, and bus lines. Route distance is the straight distance times
    /// <see cref="RouteFactor"/>; time is distance / <see cref="SpeedMps"/>.
    /// </summary>
    public sealed class FakeCareerWorld : ICareerWorld
    {
        private readonly List<PlaceInfo> m_Places = new List<PlaceInfo>();

        public readonly HashSet<string> Unreachable = new HashSet<string>();
        public readonly HashSet<string> Unknown = new HashSet<string>();
        public readonly HashSet<string> BlockedForClass = new HashSet<string>();
        public readonly List<TransitLineInfo> Lines = new List<TransitLineInfo>();
        public double RouteFactor = 1.25;
        public double SpeedMps = 10;
        public int EstimateCalls;

        public long CityRevision { get; set; } = 1;

        public PlaceInfo Add(string id, PlaceKind kind, double x, double z, string name = null)
        {
            var p = new PlaceInfo(id, kind, name ?? id, new MapPoint(x, 0, z));
            m_Places.Add(p);
            return p;
        }

        public PlaceInfo Get(string id)
        {
            foreach (PlaceInfo p in m_Places)
            {
                if (p.Id == id)
                {
                    return p;
                }
            }
            return null;
        }

        /// <summary>Demolishes a place (city edit).</summary>
        public void Remove(string id)
        {
            m_Places.RemoveAll(p => p.Id == id);
            CityRevision++;
        }

        public void Move(string id, double x, double z)
        {
            PlaceInfo p = Get(id);
            p.Access = new MapPoint(x, 0, z);
            CityRevision++;
        }

        public void SetUnreachable(string id, bool unreachable)
        {
            if (unreachable)
            {
                Unreachable.Add(id);
            }
            else
            {
                Unreachable.Remove(id);
            }
            CityRevision++;
        }

        public TransitLineInfo AddLine(string id, string name, int number, params string[] stopIds)
        {
            var stops = new List<PlaceInfo>();
            foreach (string s in stopIds)
            {
                stops.Add(Get(s));
            }
            var line = new TransitLineInfo(id, name, number, stops);
            Lines.Add(line);
            return line;
        }

        public IReadOnlyList<PlaceInfo> QueryPlaces(PlaceQuery q)
        {
            var result = new List<PlaceInfo>();
            // Reverse insertion order on purpose: Career must not depend on the port's ordering.
            for (int i = m_Places.Count - 1; i >= 0; i--)
            {
                PlaceInfo p = m_Places[i];
                if (q.Accepts(p.Kind) && MapPoint.DistanceXZ(p.Access, q.Near) <= q.RadiusMeters)
                {
                    result.Add(Copy(p));
                    if (q.MaxCount > 0 && result.Count >= q.MaxCount)
                    {
                        break;
                    }
                }
            }
            return result;
        }

        public PlaceInfo Describe(string placeId)
        {
            PlaceInfo p = Get(placeId);
            return p == null ? null : Copy(p);
        }

        public RouteEstimate Estimate(MapPoint from, string placeId, VehicleClass vehicleClass)
        {
            EstimateCalls++;
            PlaceInfo p = Get(placeId);
            if (p == null || Unreachable.Contains(placeId) || BlockedForClass.Contains(placeId + "|" + vehicleClass))
            {
                return RouteEstimate.Unreachable();
            }
            if (Unknown.Contains(placeId))
            {
                return RouteEstimate.Unknown();
            }
            double d = MapPoint.Distance(from, p.Access) * RouteFactor;
            return RouteEstimate.Reachable(d, d / SpeedMps);
        }

        public IReadOnlyList<TransitLineInfo> BusLines() => Lines;

        private static PlaceInfo Copy(PlaceInfo p) => new PlaceInfo(p.Id, p.Kind, p.Name, p.Access);
    }
}
