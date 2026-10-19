using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using DriveTheCityII.Bridge;
using DriveTheCityII.CityBridge;
using DriveTheCityII.Core.Career;
using DriveTheCityII.Core.Control;
using DriveTheCityII.Core.Geometry;
using DriveTheCityII.Core.World;
using DriveTheCityII.Diagnostics;
using DriveTheCityII.Player;
using Unity.Mathematics;
using UnityEngine;

namespace DriveTheCityII.Careers
{
    /// <summary>
    /// Test-bridge commands for careers. "career drive" is a TEST AUTOPILOT: simulated input that follows the guidance
    /// route to each mission target and holds still while Career services the leg. Career rules (arrival, boarding,
    /// settlement) run unchanged; only the steering/pedal input is simulated.
    /// </summary>
    public static class CareerBridge
    {
        private static CareerSystem C => CareerSystem.Instance;
        private static PlayerSystem P => PlayerSystem.Instance;

        public static IBridgeOp Handle(string[] a)
        {
            if (C?.Service == null)
            {
                return new InstantOp(false, "career unavailable: " + C?.DisabledReason);
            }
            CareerService s = C.Service;
            string sub = a.Length > 1 ? a[1].ToLowerInvariant() : "status";
            switch (sub)
            {
                case "status":
                    return new InstantOp(true, Status());
                case "offers":
                {
                    var kind = (CareerKind)Enum.Parse(typeof(CareerKind), a.Length > 2 ? a[2] : "Taxi", true);
                    CareerWorldAdapter w = C.World;
                    w.StatQueries = w.StatEstimates = w.StatEstimateHits = w.StatEstimateSearches = w.StatDescribes = 0;
                    w.StatQueryMs = 0;
                    double searchMsBefore = w.EstimateMsThisFrame;
                    var sw = System.Diagnostics.Stopwatch.StartNew();
                    IReadOnlyList<MissionOffer> offers = s.RefreshOffers(kind, CareerWorldAdapter.ToPoint(CareerUi.PlayerPos()), C.Clock);
                    string stats = string.Format(CultureInfo.InvariantCulture, "[{0:0}ms total; queries {1} in {2:0}ms; estimates {3} (cache hits {4}, searches {5} in {6:0}ms); describes {7}] ",
                        sw.Elapsed.TotalMilliseconds, w.StatQueries, w.StatQueryMs, w.StatEstimates, w.StatEstimateHits, w.StatEstimateSearches, w.EstimateMsThisFrame - searchMsBefore, w.StatDescribes);
                    return new InstantOp(offers.Count > 0, stats + $"{offers.Count} offers: " + string.Join(" | ", offers.Select((o, i) =>
                        $"#{i} {o.Title} {o.PlannedMeters:0}m pay {o.PayMinCents}-{o.PayMaxCents} eligible={o.Eligible} {(o.Eligible ? string.Empty : o.IneligibleText)} svc={o.ProvidesServiceVehicle}")));
                }
                case "accept":
                {
                    var kind = (CareerKind)Enum.Parse(typeof(CareerKind), a.Length > 3 ? a[3] : "Taxi", true);
                    int idx = a.Length > 2 ? int.Parse(a[2], CultureInfo.InvariantCulture) : 0;
                    IReadOnlyList<MissionOffer> offers = s.GetOffers(kind, CareerWorldAdapter.ToPoint(CareerUi.PlayerPos()), C.Clock);
                    if (idx >= offers.Count)
                    {
                        return new InstantOp(false, "no offer #" + idx);
                    }
                    string vid = CareerUi.VehicleForAccept(s, offers[idx]);
                    CareerResult r = s.Accept(offers[idx].Id, vid, C.Clock);
                    return new InstantOp(r.IsOk, $"accept {offers[idx].Title}: {r.Code} {r.Text}");
                }
                case "ui":
                    UI.HudUISystem.Instance.InvokeAction(string.Join(":", a.Skip(2)));
                    return new InstantOp(true, "ui " + string.Join(":", a.Skip(2)) + " :: " + P.LastMessage);
                case "drive":
                    // career drive [seconds] [untilLeg]: stops early once the mission reaches leg index untilLeg.
                    return new MissionAutopilotOp(a.Length > 2 ? float.Parse(a[2], CultureInfo.InvariantCulture) : 300f,
                        a.Length > 3 ? int.Parse(a[3], CultureInfo.InvariantCulture) : -1);
                case "grant":
                    return new InstantOp(true, s.DebugGrant(long.Parse(a[2], CultureInfo.InvariantCulture), C.Clock).Text);
                case "chdrive":
                    return new MissionAutopilotOp(a.Length > 2 ? float.Parse(a[2], CultureInfo.InvariantCulture) : 300f, -1, true);
                case "challenges":
                    return new InstantOp(true, Challenges());
                case "company":
                    return new InstantOp(true, Company());
                case "xp":
                {
                    // Test seeding (marks the profile test-seeded permanently; never used by normal play).
                    var kind = (CareerKind)Enum.Parse(typeof(CareerKind), a[2], true);
                    s.DebugSeedXp(kind, long.Parse(a[3], CultureInfo.InvariantCulture));
                    return new InstantOp(true, $"xp seeded: {kind} level {s.GetProgress().TotalLevel} (profile marked test-seeded)");
                }
                case "fuel":
                    return new InstantOp(s.DebugSetFuel(a[2], double.Parse(a[3], CultureInfo.InvariantCulture)), "fuel set (profile marked test-seeded) :: " + Status());
                case "rep":
                    s.DebugSetReputation(int.Parse(a[2], CultureInfo.InvariantCulture));
                    return new InstantOp(true, $"reputation {s.GetProgress().Reputation} (profile marked test-seeded)");
                case "cancel":
                    return new InstantOp(true, "cancel: " + s.Cancel("bridge", C.Clock).Text);
                case "gotoveh":
                {
                    // Test helper: place the avatar beside a career vehicle (by id, or the newest one in the world).
                    ControlledVehicle v = a.Length > 2 ? C.FindWorldVehicle(a[2]) : P.ParkedVehicles.LastOrDefault(x => x.Kind == ActorKind.CareerVehicle);
                    if (v == null || P.State != PlayerState.OnFoot)
                    {
                        return new InstantOp(false, "no such parked vehicle, or not on foot");
                    }
                    Vec2 fwd = Vec2.FromYaw(v.State.Yaw);
                    Vec2 side = v.State.Position.XZ - fwd.RightPerp * (v.Spec.Width * 0.5f + 0.9f);
                    P.TeleportAvatar(new float3(side.X, v.State.Position.Y, side.Z));
                    return new InstantOp(true, $"avatar beside {v.CareerVehicleId} ({v.Prefab?.Name})");
                }
                case "goplace":
                {
                    // Test helper: walk-teleport to the nearest place of a kind (ServicePoint, Residential, Depot...).
                    var kind = (CityPlaceKind)Enum.Parse(typeof(CityPlaceKind), a.Length > 2 ? a[2] : "ServicePoint", true);
                    float3 me = CareerUi.PlayerPos();
                    CityPlace best = null;
                    float bestD = float.MaxValue;
                    foreach (CityPlace p in CityServices.Places.Places)
                    {
                        if (p.Kind != kind || math.lengthsq(p.RoadAccess) < 1f)
                        {
                            continue;
                        }
                        float d = math.distance(p.BuildingPos.xz, me.xz);
                        if (d < bestD)
                        {
                            bestD = d;
                            best = p;
                        }
                    }
                    if (P.State != PlayerState.OnFoot)
                    {
                        return new InstantOp(false, "not on foot");
                    }
                    if (best == null)
                    {
                        return new InstantOp(false, $"no place of kind {kind} among {CityServices.Places.Places.Count} indexed places");
                    }
                    CityServices.Places.RefineAccess(P.Probe, best);
                    P.TeleportAvatar(best.WalkAccess + new float3(0f, 0.5f, 0f));
                    return new InstantOp(true, $"avatar at {kind} '{best.Name}' ({bestD:0} m away) id={best.Id}");
                }
                case "json":
                    return new InstantOp(true, s.ToJsonText().Length + " chars");
                default:
                    return new InstantOp(false, "unknown career command");
            }
        }

        public static string Company()
        {
            CareerService s = C.Service;
            CompanyView co = s.GetCompany(6);
            string cands = string.Join(",", s.GetCandidates(C.Clock).Select(x => $"{x.Id}:skill{x.Skill}:rel{x.Reliability:0.00}:sal{x.SalaryCents}"));
            string offers = string.Join(",", s.GetCompanyContractOffers(C.Clock).Select(x => $"{x.Id}:{x.Career}:{x.RequiredClass}:q{x.QuotaPerPeriod}:pay{x.PayPerJobCents}:minRel{x.MinReliability:0.00}"));
            if (!co.Exists)
            {
                return $"company=none candidates=[{cands}] contractOffers=[{offers}]";
            }
            return $"company={co.Id}:'{co.Name}' cash={co.CashCents} debt={co.DebtCents} arrears={co.InArrears} lastPeriod={co.LastSettledPeriod} nextAt={co.NextSettlementAt:0} jobs={co.SimulatedJobsTotal} hqMissing={co.HeadquartersMissing} " +
                   $"employees=[{string.Join(",", co.Employees.Select(e => $"{e.Id}:veh={e.VehicleId}:ctr={e.ContractId}"))}] contracts=[{string.Join(",", co.Contracts.Select(k => $"{k.Id}:{k.Career}:q{k.QuotaPerPeriod}:left{k.RemainingPeriods}"))}] " +
                   $"fleet=[{string.Join(",", co.FleetVehicleIds)}] candidates=[{cands}] contractOffers=[{offers}] ledger=[{string.Join("; ", co.RecentLedger.Select(e => e.Type + ":" + e.AmountCents))}]";
        }

        public static string Challenges()
        {
            return "challenges=[" + string.Join(" | ", C.Service.GetChallenges().Select(c =>
                $"{c.Id}:{c.Type}:{c.Name}:pts{c.Points.Count}:avail={c.Available}:best={c.BestSeconds:0.0}:{c.Medal}:tries{c.Attempts}{(c.Running ? ":RUNNING@" + c.NextPoint : string.Empty)}")) + "]";
        }

        public static string Status()
        {
            CareerService s = C.Service;
            WalletView w = s.GetWallet(3);
            ActiveMissionView m = s.GetActiveMission();
            string mission = m == null || string.IsNullOrEmpty(m.MissionId) ? "none" :
                $"{m.Career}/{m.State} leg={m.CurrentLegIndex}:{m.CurrentLegKind}/{m.CurrentLegState} target='{m.TargetPlaceName}' pax={m.PassengerState} served={m.StopsServed} parcels={m.ParcelsDelivered}/{m.ParcelsTotal}";
            string vehicles = string.Join(",", s.GetVehicles().Where(v => v.State != VehicleLocationState.Removed).Select(v => $"{v.Id}:{v.ModelId}:{v.Kind}:{v.State}:fuel{v.FuelPercent:0}:cond{v.ConditionPercent:0}"));
            return $"wallet={w.BalanceCents} entries={w.EntryCount} level={s.GetProgress().TotalLevel} rep={s.GetProgress().Reputation} clock={C.Clock:0} mission=[{mission}] vehicles=[{vehicles}] last=[{string.Join("; ", w.Recent.Select(e => e.Type + ":" + e.AmountCents))}]";
        }
    }

    /// <summary>
    /// Drives the active mission to completion with SIMULATED input (test autopilot; see class summary above). It
    /// follows the guidance route, slows for corners and for vehicles ahead, makes three-point turns when the route
    /// starts behind the car, backs out when stuck, and after repeated stalls uses the player's recovery action
    /// (the same as pressing Backspace). Every recovery and turn is reported in the result.
    /// </summary>
    public sealed class MissionAutopilotOp : IBridgeOp
    {
        private readonly float m_End;
        private readonly float m_Start;
        private int m_LastLeg = -2;
        private readonly List<string> m_Log = new List<string>();
        private readonly List<Obstacle> m_Obstacles = new List<Obstacle>();
        private readonly List<float> m_StuckTimes = new List<float>();
        private float m_StuckFor;
        private float m_ReverseUntil;
        private float m_ReverseSteer;
        private int m_Recoveries;
        private int m_Turns;
        private int m_Backouts;
        private int m_TurnsThisLeg;
        private float m_BlockedFor;
        private float m_IgnoreStoppedUntil;
        private float m_ProgressCheckAt;
        private float m_ProgressDist = float.MaxValue;
        private float3 m_MoveAnchor;
        private float m_MoveCheckAt;

        private readonly int m_UntilLeg;
        private readonly bool m_Challenge;

        public MissionAutopilotOp(float seconds, int untilLeg = -1, bool challenge = false)
        {
            m_UntilLeg = untilLeg;
            m_Challenge = challenge;
            m_Start = Time.realtimeSinceStartup;
            m_End = m_Start + seconds;
        }

        public bool Ok { get; private set; }
        public string Result { get; private set; } = string.Empty;

        private string Summary() => $"legs: {string.Join(" > ", m_Log)}; threePointTurns={m_Turns} backouts={m_Backouts} recoveries={m_Recoveries} [simulated input via test autopilot]";

        public bool Update()
        {
            PlayerSystem p = PlayerSystem.Instance;
            CareerSystem c = CareerSystem.Instance;
            ActiveMissionView m = c.Service?.GetActiveMission();
            float now = Time.realtimeSinceStartup;
            bool missionActive = m != null && !string.IsNullOrEmpty(m.MissionId);
            ChallengeView ch = m_Challenge && !missionActive ? c.RunningChallenge() : null;
            if (!missionActive && (ch == null || ch.Points.Count == 0))
            {
                p.Input.Injected = null;
                Ok = true;
                Result = $"{(m_Challenge ? "challenge" : "mission")} finished after {now - m_Start:0}s; {Summary()} :: {CareerBridge.Status()} {(m_Challenge ? CareerBridge.Challenges() : string.Empty)}";
                return true;
            }
            int legIndex;
            string legLabel;
            float3 target;
            float radius = 8f;
            bool serviceLeg;
            bool lastPoint = true;
            if (missionActive)
            {
                legIndex = m.CurrentLegIndex;
                legLabel = m.CurrentLegKind + "@" + m.TargetPlaceName;
                target = CareerWorldAdapter.ToF3(m.TargetAccess);
                serviceLeg = m.CurrentLegState == LegState.Servicing;
                foreach (MissionLegView l in m.Legs)
                {
                    if (l.Index == m.CurrentLegIndex && l.Radius > 0)
                    {
                        radius = (float)Math.Max(4.0, l.Radius * 0.6);
                    }
                }
            }
            else
            {
                legIndex = Math.Min(Math.Max(0, ch.NextPoint), ch.Points.Count - 1);
                legLabel = "Checkpoint" + (legIndex + 1) + "@" + ch.Points[legIndex].Name;
                target = CareerWorldAdapter.ToF3(ch.Points[legIndex].Access);
                serviceLeg = false;
                radius = 5f;
                lastPoint = legIndex == ch.Points.Count - 1;
            }
            if (m_UntilLeg >= 0 && legIndex >= m_UntilLeg && p.Vehicle != null && Math.Abs(p.Vehicle.State.Speed) < 0.3f)
            {
                p.Input.Injected = null;
                Ok = true;
                Result = $"reached leg {legIndex} after {now - m_Start:0}s; {Summary()} :: {CareerBridge.Status()}";
                return true;
            }
            if (m_UntilLeg >= 0 && legIndex >= m_UntilLeg && p.Vehicle != null)
            {
                p.Input.Injected = new InputFrame { Brake = 1f };
                return false;
            }
            if (now > m_End)
            {
                p.Input.Injected = null;
                Ok = false;
                Result = $"timeout; {Summary()} :: {CareerBridge.Status()}";
                return true;
            }
            if (legIndex != m_LastLeg)
            {
                m_LastLeg = legIndex;
                m_TurnsThisLeg = 0;
                m_ProgressDist = float.MaxValue;
                m_ProgressCheckAt = now + 180f;
                m_MoveCheckAt = 0f;
                m_Log.Add($"{legLabel}({now - m_Start:0}s)");
            }
            if (p.State != PlayerState.Driving || p.Vehicle == null)
            {
                p.Input.Injected = null;
                return false;
            }
            ControlledVehicle v = p.Vehicle;
            float3 pos = v.State.Position.ToF3();
            float speed = v.State.Speed;
            float dt = Mathf.Clamp(Time.unscaledDeltaTime, 0f, 0.25f);
            float dist = math.distance(pos.xz, target.xz);
            // Stalled (moved < 25 m in 45 s) or no gain towards the target in 3 minutes (routes may first lead away):
            // use recovery like a player would.
            bool holding = serviceLeg || (dist < radius && lastPoint);
            if (m_MoveCheckAt <= 0f || holding)
            {
                m_MoveAnchor = pos;
                m_MoveCheckAt = now + 45f;
            }
            else if (now > m_MoveCheckAt)
            {
                bool stalled = math.distance(pos.xz, m_MoveAnchor.xz) < 25f;
                m_MoveAnchor = pos;
                m_MoveCheckAt = now + 45f;
                if (stalled)
                {
                    Recover(p, now, "stalled 45 s");
                    return false;
                }
            }
            if (dist < m_ProgressDist - 10f || holding)
            {
                m_ProgressDist = dist;
                m_ProgressCheckAt = now + 180f;
            }
            else if (now > m_ProgressCheckAt)
            {
                m_ProgressDist = dist;
                m_ProgressCheckAt = now + 180f;
                Recover(p, now, "no progress for 3 min");
                return false;
            }
            if (holding)
            {
                // Stop and hold with the handbrake (holding the brake at a standstill would engage reverse).
                p.Input.Injected = new InputFrame { Brake = Math.Abs(speed) > 0.3f ? 1f : 0f, Handbrake = Math.Abs(speed) <= 0.3f };
                m_StuckFor = 0f;
                return false;
            }
            if (now < m_ReverseUntil)
            {
                p.Input.Injected = new InputFrame { Brake = 0.8f, Steer = m_ReverseSteer };
                return false;
            }

            float3 aim = AimPoint(MarkerBoard.Route, pos, speed, target, out float turnAhead);
            float desired = math.atan2(aim.x - pos.x, aim.z - pos.z);
            float err = MathX.WrapAngle(desired - v.State.Yaw);

            // Route starts behind the car: three-point turn (reverse on the opposite lock), as on a narrow street.
            if (Math.Abs(err) > 1.9f && speed < 2.5f)
            {
                m_Turns++;
                m_TurnsThisLeg++;
                if (m_TurnsThisLeg > 10)
                {
                    Recover(p, now, "three-point turn did not converge");
                    m_TurnsThisLeg = 0;
                    return false;
                }
                m_ReverseUntil = now + 1.8f;
                m_ReverseSteer = -Math.Sign(err);
                p.Input.Injected = new InputFrame { Brake = 1f, Steer = m_ReverseSteer };
                return false;
            }

            float want = Mathf.Clamp(dist * 0.35f, 4f, 12f);
            if (turnAhead > 0.9f)
            {
                want = Math.Min(want, 4.5f);
            }
            else if (turnAhead > 0.45f)
            {
                want = Math.Min(want, 7f);
            }
            if (Math.Abs(err) > 0.9f)
            {
                want = Math.Min(want, 2.5f);
            }
            else if (Math.Abs(err) > 0.5f)
            {
                want = Math.Min(want, 4f);
            }
            bool ignoreStopped = now < m_IgnoreStoppedUntil;
            float gap = VehicleAheadGap(p, v, pos, ignoreStopped, out bool stoppedAhead);
            if (gap < 25f)
            {
                want = Math.Min(want, Math.Max(0f, (gap - 6f) * 0.6f));
                // Waiting behind a vehicle that does not move (parked or itself waiting): after 4 s, go around it.
                m_BlockedFor = stoppedAhead && Math.Abs(speed) < 0.5f ? m_BlockedFor + dt : 0f;
                if (m_BlockedFor > 4f)
                {
                    m_BlockedFor = 0f;
                    m_IgnoreStoppedUntil = now + 8f;
                    m_Log.Add($"pass-stopped-vehicle({now - m_Start:0}s)");
                }
            }
            else
            {
                m_BlockedFor = 0f;
            }
            var f = new InputFrame { Steer = math.clamp(err * 2f, -1f, 1f) };
            if (speed < want - 0.3f)
            {
                f.Throttle = math.clamp((want - speed) * 0.4f, 0.2f, 1f);
            }
            else if (speed > want + 0.5f)
            {
                f.Brake = math.clamp((speed - want) * 0.3f, 0.1f, 1f);
            }

            DtcLog.Throttled("autopilot", now, 3f, string.Format(CultureInfo.InvariantCulture,
                "autopilot: leg={0} dist={1:0} err={2:0.00} speed={3:0.0} want={4:0.0} gap={5:0} routePts={6} turnAhead={7:0.00} ground={8} pos={9}",
                legIndex, dist, err, speed, want, gap > 1000f ? -1f : gap, MarkerBoard.Route.Count, turnAhead, v.State.GroundKind, v.State.Position));
            // Stuck: pushing the throttle but not moving.
            if (f.Throttle > 0.15f && Math.Abs(speed) < 0.3f)
            {
                m_StuckFor += dt;
            }
            else
            {
                m_StuckFor = 0f;
            }
            if (m_StuckFor > 1.5f)
            {
                m_StuckFor = 0f;
                m_StuckTimes.Add(now);
                m_StuckTimes.RemoveAll(t => now - t > 45f);
                if (m_StuckTimes.Count >= 3)
                {
                    m_StuckTimes.Clear();
                    Recover(p, now, "stuck");
                    return false;
                }
                m_Backouts++;
                m_ReverseUntil = now + 2f;
                m_ReverseSteer = -Math.Sign(f.Steer);
                f = new InputFrame { Brake = 1f, Steer = m_ReverseSteer };
            }
            p.Input.Injected = f;
            return false;
        }

        private void Recover(PlayerSystem p, float now, string why)
        {
            m_Recoveries++;
            m_Log.Add($"recovery:{why}({now - m_Start:0}s)");
            p.Input.Injected = null;
            p.EnterRecovery("test autopilot " + why);
        }

        private static float3 AimPoint(List<float3> route, float3 pos, float speed, float3 fallback, out float turnAhead)
        {
            turnAhead = 0f;
            if (route.Count < 2)
            {
                return fallback;
            }
            int best = 0;
            float bestD = float.MaxValue;
            for (int i = 0; i < route.Count; i++)
            {
                float d = math.distancesq(route[i].xz, pos.xz);
                if (d < bestD)
                {
                    bestD = d;
                    best = i;
                }
            }
            float look = Mathf.Clamp(3f + Math.Max(0f, speed) * 0.5f, 3f, 8f);
            float acc = 0f;
            int j = best;
            while (j < route.Count - 1 && acc < look)
            {
                acc += math.distance(route[j], route[j + 1]);
                j++;
            }
            float2 d0 = math.normalizesafe(route[Math.Min(best + 1, route.Count - 1)].xz - route[best].xz);
            acc = 0f;
            for (int k = best; k < route.Count - 1 && acc < 30f; k++)
            {
                float2 dk = math.normalizesafe(route[k + 1].xz - route[k].xz);
                turnAhead = Math.Max(turnAhead, math.acos(math.clamp(math.dot(d0, dk), -1f, 1f)));
                acc += math.distance(route[k], route[k + 1]);
            }
            return route[j];
        }

        private float VehicleAheadGap(PlayerSystem p, ControlledVehicle v, float3 pos, bool ignoreStopped, out bool nearestStopped)
        {
            nearestStopped = false;
            Vec2 fwd = Vec2.FromYaw(v.State.Yaw);
            Vec2 right = fwd.RightPerp;
            var me = new Vec2(pos.x, pos.z);
            m_Obstacles.Clear();
            p.Probe.QueryObstacles(me + fwd * 12f, 13f, m_Obstacles);
            float gap = float.MaxValue;
            foreach (Obstacle o in m_Obstacles)
            {
                if (o.Kind != ObstacleKind.Vehicle)
                {
                    continue;
                }
                Vec2 rel = o.Footprint.Center - me;
                float along = Vec2.Dot(rel, fwd);
                bool stopped = o.Velocity.Length < 0.5f;
                if (ignoreStopped && stopped)
                {
                    continue;
                }
                if (along > 1f && along < 25f && Math.Abs(Vec2.Dot(rel, right)) < 1.8f && along < gap)
                {
                    gap = along;
                    nearestStopped = stopped;
                }
            }
            return gap;
        }
    }
}
