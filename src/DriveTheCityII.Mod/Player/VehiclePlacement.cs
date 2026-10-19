using System.Collections.Generic;
using DriveTheCityII.CityBridge;
using DriveTheCityII.Core.Geometry;
using DriveTheCityII.Core.Motion;
using DriveTheCityII.Core.World;
using Unity.Mathematics;

namespace DriveTheCityII.Player
{
    /// <summary>
    /// Clear-spot search for placing a whole vehicle (career vehicles, employer trucks and buses, recovery). A lane point
    /// alone is not enough: a 12 m bus placed at the nearest lane point can overlap a lamppost, tree or shelter and then
    /// cannot move (observed in game on 2026-10-19). Every candidate is checked against buildings, static objects and
    /// vehicles over the vehicle's full footprint.
    /// </summary>
    public partial class PlayerSystem
    {
        private readonly List<Obstacle> m_PlaceScratch = new List<Obstacle>();
        private readonly List<float3> m_PlaceProbes = new List<float3>(80);

        /// <summary>True when a vehicle footprint at a position and heading overlaps no building, static object or vehicle.</summary>
        public bool IsFootprintClear(VehicleSpec spec, float3 pos, float yaw)
        {
            if (!m_Probe.IsInsideMap(pos.x, pos.z))
            {
                return false;
            }
            var c = new Vec2(pos.x, pos.z);
            var box = new Obb2(c, Vec2.FromYaw(yaw), spec.Width * 0.5f + 0.15f, spec.Length * 0.5f + 0.3f);
            m_PlaceScratch.Clear();
            m_Probe.QueryObstacles(c, spec.Length * 0.5f + 4f, m_PlaceScratch);
            foreach (Obstacle o in m_PlaceScratch)
            {
                if (o.Kind == ObstacleKind.Creature || o.Kind == ObstacleKind.Proxy)
                {
                    continue;
                }
                if (o.YMax < pos.y + 0.25f || o.YMin > pos.y + spec.Height)
                {
                    continue;
                }
                if (Overlap.BoxVsBox(box, o.Footprint).Overlaps)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// The closest spot near <paramref name="near"/> on a road lane (or a parking lane, for small vehicles) where the
        /// whole vehicle fits. Tries the nearest lane point first, then slides along the lane and widens the search in rings.
        /// </summary>
        public bool FindClearVehicleSpot(VehicleSpec spec, float3 near, bool allowParking, float maxRadius, out float3 pos, out float yaw)
        {
            pos = default;
            yaw = 0f;
            m_PlaceProbes.Clear();
            m_PlaceProbes.Add(near);
            for (float r = 10f; r <= maxRadius; r += 10f)
            {
                int n = r < 40f ? 8 : 12;
                for (int k = 0; k < n; k++)
                {
                    float a = k * 2f * math.PI / n;
                    m_PlaceProbes.Add(near + new float3(math.cos(a) * r, 0f, math.sin(a) * r));
                }
            }
            float slide = math.max(4f, spec.Length * 0.6f);
            float[] offsets = { 0f, slide, -slide, 2f * slide, -2f * slide };
            CityWorldProbe.LaneKind[] kinds = allowParking
                ? new[] { CityWorldProbe.LaneKind.Parking, CityWorldProbe.LaneKind.Road }
                : new[] { CityWorldProbe.LaneKind.Road };
            float bestD = float.MaxValue;
            int ringEnd = 1;
            bool found = false;
            for (int i = 0; i < m_PlaceProbes.Count; i++)
            {
                // Finish the current ring once something was found, so the closest clear spot wins.
                if (found && i >= ringEnd)
                {
                    break;
                }
                if (i >= ringEnd)
                {
                    float rr = math.distance(m_PlaceProbes[i].xz, near.xz);
                    ringEnd = i + (rr < 40f ? 8 : 12);
                }
                foreach (CityWorldProbe.LaneKind kind in kinds)
                {
                    if (!m_Probe.FindNearestLanePoint(m_PlaceProbes[i], 14f, kind, out float3 p, out float3 tan, out _))
                    {
                        continue;
                    }
                    float3 dir = math.normalizesafe(new float3(tan.x, 0f, tan.z));
                    foreach (float s in offsets)
                    {
                        float3 cand = p + dir * s;
                        float3 ctan = tan;
                        if (s != 0f && !m_Probe.FindNearestLanePoint(cand, 3f, kind, out cand, out ctan, out _))
                        {
                            continue;
                        }
                        float cyaw = math.atan2(ctan.x, ctan.z);
                        float d = math.distance(cand.xz, near.xz);
                        if (d >= bestD || !IsFootprintClear(spec, cand, cyaw))
                        {
                            continue;
                        }
                        bestD = d;
                        pos = cand;
                        yaw = cyaw;
                        found = true;
                    }
                }
            }
            return found;
        }
    }
}
