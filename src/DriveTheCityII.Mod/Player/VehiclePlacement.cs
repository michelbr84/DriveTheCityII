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
        public bool IsFootprintClear(VehicleSpec spec, float3 pos, float yaw) => IsFootprintClear(spec, pos, yaw, 0.15f, 0.3f);

        /// <summary>
        /// True when a box (centre, heading, half sizes) between two heights overlaps no building, static object or vehicle.
        /// With <paramref name="spec"/>, obstacles that do not block that vehicle (small props for long vehicles) are ignored.
        /// </summary>
        public bool IsBoxClear(float3 center, float yaw, float halfWidth, float halfLength, float baseY, float height, VehicleSpec spec = null)
        {
            var c = new Vec2(center.x, center.z);
            var box = new Obb2(c, Vec2.FromYaw(yaw), halfWidth, halfLength);
            m_PlaceScratch.Clear();
            m_Probe.QueryObstacles(c, halfLength + halfWidth + 3f, m_PlaceScratch);
            foreach (Obstacle o in m_PlaceScratch)
            {
                if (o.Kind == ObstacleKind.Creature || o.Kind == ObstacleKind.Proxy || !VehicleController.Blocks(o, spec))
                {
                    continue;
                }
                if (o.YMax < baseY + 0.25f || o.YMin > baseY + height)
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

        /// <summary>As above with explicit margins (0, 0: the bodies themselves overlap).</summary>
        public bool IsFootprintClear(VehicleSpec spec, float3 pos, float yaw, float widthMargin, float lengthMargin)
        {
            if (!m_Probe.IsInsideMap(pos.x, pos.z))
            {
                return false;
            }
            var c = new Vec2(pos.x, pos.z);
            var box = new Obb2(c, Vec2.FromYaw(yaw), spec.Width * 0.5f + widthMargin, spec.Length * 0.5f + lengthMargin);
            m_PlaceScratch.Clear();
            m_Probe.QueryObstacles(c, spec.Length * 0.5f + 4f, m_PlaceScratch);
            foreach (Obstacle o in m_PlaceScratch)
            {
                // A long vehicle placed over a lamppost can drive away (ISSUE-040), so small props do not block its spot.
                if (o.Kind == ObstacleKind.Creature || o.Kind == ObstacleKind.Proxy || !VehicleController.Blocks(o, spec))
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
        /// With <paramref name="preferYaw"/> (recovery), lanes running the other way count as 25 m further away, so the
        /// vehicle is not put on the opposite carriageway facing backwards when a spot in its own direction is near.
        /// </summary>
        public bool FindClearVehicleSpot(VehicleSpec spec, float3 near, bool allowParking, float maxRadius, out float3 pos, out float yaw, float preferYaw = float.NaN)
        {
            bool prefer = !float.IsNaN(preferYaw);
            float2 preferDir = prefer ? new float2(math.sin(preferYaw), math.cos(preferYaw)) : default;
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
            bool bestPenalized = false;
            int ringEnd = 1;
            bool found = false;
            for (int i = 0; i < m_PlaceProbes.Count; i++)
            {
                // Finish the current ring once something was found, so the closest clear spot wins (keep looking while
                // the best spot faces the wrong way and the rings are still within the penalty distance).
                if (found && i >= ringEnd && (!bestPenalized || math.distance(m_PlaceProbes[i].xz, near.xz) > bestD))
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
                        bool penalized = prefer && math.dot(math.normalizesafe(ctan.xz), preferDir) < 0f;
                        if (penalized)
                        {
                            d += 25f;
                        }
                        // Another level (the road under an overpass, or the bridge above): only when nothing is near on
                        // this level (observed in game: recovery on an elevated road put the truck on the road below).
                        if (math.abs(cand.y - near.y) > 2.5f)
                        {
                            d += 40f;
                            penalized = true;
                        }
                        if (d >= bestD || !IsFootprintClear(spec, cand, cyaw))
                        {
                            continue;
                        }
                        bestD = d;
                        bestPenalized = penalized;
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
