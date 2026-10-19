using System;
using DriveTheCityII.CityBridge;
using DriveTheCityII.Core.Career;
using DriveTheCityII.Core.Control;
using DriveTheCityII.Player;
using Unity.Entities;
using Unity.Mathematics;
using UTime = UnityEngine.Time;

namespace DriveTheCityII.Careers
{
    public partial class CareerSystem
    {
        /// <summary>
        /// Shows the active mission in the world: a marker at the current target, a guidance route (refreshed every few
        /// seconds), and mod-owned passengers waiting at taxi pickups or walking away after drop-off (SPEC-TAXI: career
        /// passengers are represented honestly as mod characters, not native citizens).
        /// </summary>
        private void UpdatePresentation(float dt, bool paused)
        {
            ActiveMissionView m = Service.GetActiveMission();
            float now = UTime.realtimeSinceStartup;
            EntityManager em = EntityManager;
            m_Objects?.Update(em, dt, now, paused);
            if (m == null || string.IsNullOrEmpty(m.MissionId))
            {
                if (ShowRunningChallenge(em, now))
                {
                    return;
                }
                if (m_LastRouteTarget != null)
                {
                    MarkerBoard.Clear();
                    m_LastRouteTarget = null;
                }
                ShowNearbyServicePoints();
                return;
            }

            // Target marker and route.
            float3 target = CareerWorldAdapter.ToF3(m.TargetAccess);
            bool hasTarget = !string.IsNullOrEmpty(m.TargetPlaceId) && math.lengthsq(target) > 1f;
            if (hasTarget && (m.TargetPlaceId != m_LastRouteTarget || now >= m_NextRoute))
            {
                m_LastRouteTarget = m.TargetPlaceId;
                m_NextRoute = now + 3f;
                MarkerBoard.Clear();
                MarkerStyle style = m.CurrentLegKind == LegKind.Pickup || m.CurrentLegKind == LegKind.Load ? MarkerStyle.Pickup
                    : m.CurrentLegKind == LegKind.Checkpoint ? MarkerStyle.Checkpoint
                    : MarkerStyle.Dropoff;
                MarkerBoard.Markers.Add(new WorldMarker { Position = target, Style = style, Radius = 5f });
                if (P.State == PlayerState.Driving && P.Vehicle != null)
                {
                    float3 from = P.Vehicle.State.Position.ToF3();
                    float2 heading = new float2(math.sin(P.Vehicle.State.Yaw), math.cos(P.Vehicle.State.Yaw));
                    float wrongWay = P.Vehicle.Spec.Length > 7f ? 600f : 30f;
                    // End the route on a lane right at the target (docks and bus stops have 8 m arrival radii); widen only
                    // if no lane is that close.
                    RouteResult r = CityServices.Router.FindRoute(em, from, heading, target, 6f, 80000, wrongWay);
                    if (r.Status != RouteStatusCode.Reachable)
                    {
                        r = CityServices.Router.FindRoute(em, from, heading, target, 18f, 80000, wrongWay);
                    }
                    if (r.Status == RouteStatusCode.Reachable)
                    {
                        MarkerBoard.Route.AddRange(r.Polyline);
                    }
                    else
                    {
                        Diagnostics.DtcLog.Throttled("guidance-route", now, 10f, $"Guidance route {r.Status} from {from} to {target} (expansions {r.Expansions})", warn: true);
                    }
                }
                else if (m.VehicleId != null)
                {
                    // On foot during a mission: also mark the mission vehicle.
                    ControlledVehicle mv = FindWorldVehicle(m.VehicleId);
                    if (mv != null)
                    {
                        MarkerBoard.Markers.Add(new WorldMarker { Position = mv.State.Position.ToF3(), Style = MarkerStyle.Vehicle, Radius = 3f });
                    }
                }
            }

            // Taxi passengers.
            if (m.Career == CareerKind.Taxi && m_Objects != null)
            {
                string key = "pax:" + m.MissionId;
                MissionLegView pickup = null;
                MissionLegView dropoff = null;
                foreach (MissionLegView l in m.Legs)
                {
                    if (l.Kind == LegKind.Pickup && pickup == null)
                    {
                        pickup = l;
                    }
                    if (l.Kind == LegKind.Dropoff)
                    {
                        dropoff = l;
                    }
                }
                if (m.PassengerState == PassengerState.Waiting && pickup != null)
                {
                    float3 at = WalkPoint(pickup.PlaceId, CareerWorldAdapter.ToF3(pickup.Access));
                    m_Objects.ShowWaiting(key, at, 0f, Seed32(m.MissionId));
                }
                else if (m.PassengerState == PassengerState.Aboard)
                {
                    m_Objects.Board(key);
                }
                else if (m.PassengerState == PassengerState.Alighted && dropoff != null && !m_Alighted.Contains(m.MissionId))
                {
                    m_Alighted.Add(m.MissionId);
                    float3 from = P.Vehicle != null ? P.Vehicle.State.Position.ToF3() : CareerWorldAdapter.ToF3(dropoff.Access);
                    float3 to = WalkPoint(dropoff.PlaceId, from);
                    m_Objects.Alight(key, from + new float3(0.5f, 0f, 0.5f), to, Seed32(m.MissionId), now);
                }
            }
        }

        /// <summary>The running challenge, if any (null otherwise).</summary>
        public ChallengeView RunningChallenge()
        {
            if (Service == null)
            {
                return null;
            }
            foreach (ChallengeView c in Service.GetChallenges())
            {
                if (c.Running)
                {
                    return c;
                }
            }
            return null;
        }

        /// <summary>A running challenge: the next checkpoint (and the one after it) are marked and routed like a mission target.</summary>
        private bool ShowRunningChallenge(EntityManager em, float now)
        {
            ChallengeView c = RunningChallenge();
            if (c == null || c.Points.Count == 0)
            {
                return false;
            }
            int next = Math.Min(Math.Max(0, c.NextPoint), c.Points.Count - 1);
            string key = "ch:" + c.Id + ":" + next;
            if (key == m_LastRouteTarget && now < m_NextRoute)
            {
                return true;
            }
            m_LastRouteTarget = key;
            m_NextRoute = now + 3f;
            MarkerBoard.Clear();
            float3 target = CareerWorldAdapter.ToF3(c.Points[next].Access);
            MarkerBoard.Markers.Add(new WorldMarker { Position = target, Style = MarkerStyle.Checkpoint, Radius = 5f });
            if (next + 1 < c.Points.Count)
            {
                MarkerBoard.Markers.Add(new WorldMarker { Position = CareerWorldAdapter.ToF3(c.Points[next + 1].Access), Style = MarkerStyle.Checkpoint, Radius = 2.5f });
            }
            if (P.State == PlayerState.Driving && P.Vehicle != null)
            {
                float3 from = P.Vehicle.State.Position.ToF3();
                float2 heading = new float2(math.sin(P.Vehicle.State.Yaw), math.cos(P.Vehicle.State.Yaw));
                RouteResult r = CityServices.Router.FindRoute(em, from, heading, target, 18f, 80000, P.Vehicle.Spec.Length > 7f ? 600f : 30f);
                if (r.Status == RouteStatusCode.Reachable)
                {
                    MarkerBoard.Route.AddRange(r.Polyline);
                }
            }
            return true;
        }

        private readonly System.Collections.Generic.HashSet<string> m_Alighted = new System.Collections.Generic.HashSet<string>();
        private float m_NextServiceScan;

        /// <summary>Without an active mission, nearby service points are marked so the player can find rentals and repairs.</summary>
        private void ShowNearbyServicePoints()
        {
            float now = UTime.realtimeSinceStartup;
            if (now < m_NextServiceScan)
            {
                return;
            }
            m_NextServiceScan = now + 2f;
            MarkerBoard.Clear();
            float3 me = P.State == PlayerState.Driving && P.Vehicle != null ? P.Vehicle.State.Position.ToF3() : P.Avatar.Position.ToF3();
            int n = 0;
            foreach (CityPlace p in CityServices.Places.Places)
            {
                if (p.Kind != CityPlaceKind.ServicePoint && p.Kind != CityPlaceKind.Depot)
                {
                    continue;
                }
                if (math.distance(p.RoadAccess.xz, me.xz) > 600f)
                {
                    continue;
                }
                CityServices.Places.RefineAccess(P.Probe, p);
                MarkerBoard.Markers.Add(new WorldMarker { Position = p.RoadAccess, Style = MarkerStyle.ServicePoint, Radius = 3f });
                if (++n >= 6)
                {
                    break;
                }
            }
            HomeView home = Service.GetHome();
            if (home != null && home.HasHome)
            {
                MarkerBoard.Markers.Add(new WorldMarker { Position = CareerWorldAdapter.ToF3(home.HomeAccess), Style = MarkerStyle.Home, Radius = 3f });
            }
        }

        private float3 WalkPoint(string placeId, float3 fallback)
        {
            if (!string.IsNullOrEmpty(placeId) && CityServices.Places.TryGet(placeId, out CityPlace p))
            {
                CityServices.Places.RefineAccess(P.Probe, p);
                return p.WalkAccess;
            }
            if (P.Probe.FindNearestLanePoint(fallback, 25f, CityWorldProbe.LaneKind.Sidewalk, out float3 w, out _, out _))
            {
                return w;
            }
            return fallback;
        }

        private static int Seed32(string s)
        {
            int h = 17;
            foreach (char c in s ?? string.Empty)
            {
                h = h * 31 + c;
            }
            return h & 0x7fffffff;
        }

        /// <summary>Nearest service point (or depot) within a radius of the player, for garage interactions.</summary>
        public CityPlace NearestServicePoint(float radius)
        {
            float3 me = P.State == PlayerState.Driving && P.Vehicle != null ? P.Vehicle.State.Position.ToF3() : P.Avatar.Position.ToF3();
            CityPlace best = null;
            float bestD = radius;
            foreach (CityPlace p in CityServices.Places.Places)
            {
                if (p.Kind != CityPlaceKind.ServicePoint && p.Kind != CityPlaceKind.Depot)
                {
                    continue;
                }
                float d = Math.Min(math.distance(p.RoadAccess, me), math.distance(p.WalkAccess, me));
                if (d < bestD)
                {
                    bestD = d;
                    best = p;
                }
            }
            return best;
        }
    }
}
