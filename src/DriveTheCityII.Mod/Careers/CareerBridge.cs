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
                    C.HandleResultEvents(r);
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
                    { CareerResult cr = s.Cancel("bridge", C.Clock); C.HandleResultEvents(cr); return new InstantOp(true, "cancel: " + cr.Text); }
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
        private float m_LastRecoverAt = -999f;
        private float m_YieldFor;
        private double m_LastIntegrity = 100.0;
        private float m_BypassUntil;
        private float m_BypassSide;
        private bool m_KTurn;
        private bool m_KForward;
        private float m_KDir;
        private float m_KPhaseSince;
        private int m_KPhases;
        private float m_LastGap = float.MaxValue;
        private float m_LastConflict = float.MaxValue;
        private float m_WaitingSince = -1f;
        private float m_NotDrivingSince = -1f;
        private float3 m_StillAnchor;
        private float m_StillSince = -1f;
        private float m_TouchedAt = -999f;

        private readonly int m_UntilLeg;
        private readonly bool m_Challenge;

        private Action<Core.Motion.VehicleStepReport> m_OnImpact;

        public MissionAutopilotOp(float seconds, int untilLeg = -1, bool challenge = false)
        {
            m_UntilLeg = untilLeg;
            m_Challenge = challenge;
            m_OnImpact = ReportImpact;
            if (PlayerSystem.Instance != null)
            {
                PlayerSystem.Instance.VehicleImpact += m_OnImpact;
            }
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
                Unsubscribe();
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
                        radius = (float)Math.Max(4.0, l.Radius * 0.85);
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
                Unsubscribe();
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
                Unsubscribe();
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
                // Returned to city building (for example by the recovery-loop protection): stop instead of waiting
                // for the timeout, so the caller can decide what to do.
                if (p.State == PlayerState.Building)
                {
                    if (m_NotDrivingSince < 0f)
                    {
                        m_NotDrivingSince = now;
                    }
                    else if (now - m_NotDrivingSince > 5f)
                    {
                        Ok = false;
                        Result = $"player returned to city building; {Summary()} :: {CareerBridge.Status()}";
                        Unsubscribe();
                return true;
                    }
                }
                return false;
            }
            m_NotDrivingSince = -1f;
            ControlledVehicle v = p.Vehicle;
            float3 pos = v.State.Position.ToF3();
            float speed = v.State.Speed;
            float dt = Mathf.Clamp(Time.unscaledDeltaTime, 0f, 0.25f);
            float dist = math.distance(pos.xz, target.xz);
            // Stalled (moved < 25 m in 45 s) or no gain towards the target in 3 minutes (routes may first lead away):
            // use recovery like a player would.
            bool holding = serviceLeg || (dist < radius && lastPoint);
            // Waiting in traffic (queue ahead or a crossing vehicle) is not a stall, up to two minutes.
            bool waitingInTraffic = m_LastGap < 25f || m_LastConflict < 3f;
            if (waitingInTraffic)
            {
                if (m_WaitingSince < 0f)
                {
                    m_WaitingSince = now;
                }
            }
            else
            {
                m_WaitingSince = -1f;
            }
            bool watchdogPaused = holding || m_KTurn || (waitingInTraffic && now - m_WaitingSince < 120f);
            // Hard limit, whatever the reason (the waiting timer above restarts whenever the gap reading flickers): not
            // moving 8 m in 120 s, or in 60 s while touching a stopped vehicle, is a deadlock. Observed in game on
            // 2026-10-19: a truck turning across our lane stopped against our front, the queues behind both locked the
            // road for 15 minutes, and traffic flowed again within minutes of our truck leaving.
            if (holding || m_StillSince < 0f || math.distance(pos.xz, m_StillAnchor.xz) > 8f)
            {
                m_StillAnchor = pos;
                m_StillSince = now;
            }
            else
            {
                bool touching = now - m_TouchedAt < 5f;
                float still = now - m_StillSince;
                // Queued behind a stopped vehicle (a jam on a highway): wait longer before giving up on the lane.
                float limit = m_LastGap < 6f ? 300f : 120f;
                if (still > limit || (touching && still > 60f))
                {
                    m_StillSince = now;
                    DtcLog.Info(string.Format(CultureInfo.InvariantCulture, "autopilot: deadlock after {0:0} s without moving (touching={1}) at {2}", still, touching, v.State.Position));
                    Recover(p, now, touching ? "deadlock against a stopped vehicle" : $"no movement for {limit:0} s");
                    return false;
                }
            }
            if (m_MoveCheckAt <= 0f || watchdogPaused)
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
            if (dist < m_ProgressDist - 10f || watchdogPaused)
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
                p.Input.Injected = Reverse(v, m_ReverseSteer);
                return false;
            }

            float3 aim = AimPoint(MarkerBoard.Route, pos, speed, target, out float turnAhead, out float curvature);
            bool bypassing = now < m_BypassUntil;
            if (bypassing)
            {
                // Going round a stopped vehicle: aim 3.5 m to the chosen side of the route.
                float3 side = new float3(math.cos(v.State.Yaw), 0f, -math.sin(v.State.Yaw)) * m_BypassSide;
                aim += side * 3.5f;
            }
            float desired = math.atan2(aim.x - pos.x, aim.z - pos.z);
            float err = MathX.WrapAngle(desired - v.State.Yaw);
            if (missionActive && m.LoadIntegrity >= 0 && Math.Abs(m.LoadIntegrity - m_LastIntegrity) > 0.5)
            {
                DtcLog.Info(string.Format(CultureInfo.InvariantCulture,
                    "autopilot: load integrity {0:0.0} -> {1:0.0} (speed {2:0.0}, gear {3}, k-turn {4}, bypass {5}, steer {6:0.00}, leg {7})",
                    m_LastIntegrity, m.LoadIntegrity, speed, v.State.Gear, m_KTurn, bypassing, v.State.SteerAngle, legIndex));
                m_LastIntegrity = m.LoadIntegrity;
            }
            DtcLog.Throttled("autopilot-pre", now, 3f, string.Format(CultureInfo.InvariantCulture,
                "autopilot-pre: leg={0} dist={1:0} err={2:0.00} speed={3:0.0} routePts={4} aim=({5:0},{6:0}) pos=({7:0},{8:0}) target=({9:0},{10:0}) gear={11} ground={12}",
                legIndex, dist, err, speed, MarkerBoard.Route.Count, aim.x, aim.z, pos.x, pos.z, target.x, target.z, v.State.Gear, v.State.GroundKind));

            // Target close and behind (a dock or stop we just passed): reverse straight onto it, as a driver would,
            // instead of turning round. The rear points along yaw + pi; steering is inverted when reversing.
            float directErr = MathX.WrapAngle(math.atan2(target.x - pos.x, target.z - pos.z) - v.State.Yaw);
            if (dist < Math.Max(14f, radius * 2.5f) && Math.Abs(directErr) > 1.6f && speed < 1.5f)
            {
                float errRear = MathX.WrapAngle(math.atan2(target.x - pos.x, target.z - pos.z) - (v.State.Yaw + math.PI));
                p.Input.Injected = Reverse(v, math.clamp(-2f * errRear, -1f, 1f));
                return false;
            }

            // Route starts behind the vehicle: turn round with a K-turn that checks the space before every move.
            if (!m_KTurn && Math.Abs(err) > 1.9f && speed < 2.5f)
            {
                m_KTurn = true;
                m_KDir = Math.Sign(err);
                m_KForward = FootprintClearAhead(p, v, pos, 1.2f, m_KDir);
                m_KPhaseSince = now;
                m_KPhases = 0;
                m_Turns++;
            }
            if (m_KTurn)
            {
                if (Math.Abs(err) < 0.6f)
                {
                    m_KTurn = false;
                }
                else
                {
                    KTurn(p, v, pos, speed, now);
                    return false;
                }
            }

            bool heavy = v.Spec.Length > 7f;
            // Trucks and buses: 9 m/s in general, 12 m/s on straight road. Observed in game on 2026-10-19: at 9 m/s on a
            // highway, traffic at 36-40 m/s ran into the truck from behind while changing lanes.
            // Curves: no faster than the cargo's comfortable lateral acceleration allows for the tightest bend ahead
            // (observed in game on 2026-10-19: at 14 m/s the steering limit could not hold a highway curve and the truck
            // ran off onto the verge).
            float curveCap = curvature > 1e-4f ? (float)Math.Sqrt((heavy ? 2.0f : 3.0f) / curvature) : float.MaxValue;
            bool straight = turnAhead < 0.15f && Math.Abs(err) < 0.15f;
            bool open = turnAhead < 0.06f && Math.Abs(err) < 0.06f && m_LastGap > 40f;
            float want = Mathf.Clamp(dist * 0.35f, 4f, heavy ? (open ? 16f : straight ? 12f : 9f) : 12f);
            want = Math.Min(want, Math.Max(2.5f, curveCap));
            if (turnAhead > 0.9f)
            {
                want = Math.Min(want, heavy ? 3.5f : 4.5f);
            }
            else if (turnAhead > 0.45f)
            {
                want = Math.Min(want, heavy ? 5f : 7f);
            }
            if (Math.Abs(err) > 0.9f)
            {
                want = Math.Min(want, 2.5f);
            }
            else if (Math.Abs(err) > 0.5f)
            {
                want = Math.Min(want, 4f);
            }
            bool ignoreStopped = now < m_IgnoreStoppedUntil || bypassing;
            float gap = VehicleAheadGap(p, v, pos, Math.Max(speed, 1f), ignoreStopped, out bool stoppedAhead, out float conflictIn, out float leaderSpeed);
            if (gap < 40f)
            {
                // Follow the vehicle ahead: its speed (a little less, so the gap opens) plus what still allows stopping 3 m
                // behind it at a gentle 2.5 m/s². A car cutting in close but moving as fast as we are needs no hard stop
                // (observed in game on 2026-10-19: full stops for cut-ins cost heavy cargo 4 points each).
                want = Math.Min(want, leaderSpeed * 0.9f + (float)Math.Sqrt(2.0 * 2.5 * Math.Max(0f, gap - 3f)));
                // Waiting behind a vehicle that does not move (parked, or waiting itself). Cars try to pass it after 20 s;
                // trucks and buses keep waiting (a collision would cost the cargo), and the stall watchdog decides.
                m_BlockedFor = stoppedAhead && Math.Abs(speed) < 0.5f ? m_BlockedFor + dt : 0f;
                if (m_BlockedFor > (heavy ? 30f : 15f) && !bypassing)
                {
                    // Go round it carefully on whichever side has room (left first, as in right-hand traffic).
                    m_BlockedFor = 0f;
                    float3 fwd3 = new float3(math.sin(v.State.Yaw), 0f, math.cos(v.State.Yaw));
                    float3 right3 = new float3(math.cos(v.State.Yaw), 0f, -math.sin(v.State.Yaw));
                    foreach (float sideSign in new[] { -1f, 1f })
                    {
                        if (p.IsFootprintClear(v.Spec, pos + fwd3 * 4f + right3 * (3.5f * sideSign), v.State.Yaw + sideSign * 0.2f))
                        {
                            m_BypassSide = sideSign;
                            m_BypassUntil = now + 14f;
                            m_Log.Add($"bypass-{(sideSign < 0f ? "left" : "right")}({now - m_Start:0}s)");
                            break;
                        }
                    }
                }
            }
            else
            {
                m_BlockedFor = 0f;
            }
            m_LastGap = gap;
            m_LastConflict = conflictIn;
            // Touching means the bodies overlap (a vehicle stopped against or into ours), not a tight queue: native cars
            // stop well under a metre apart, and a normal queue on a bridge was taken for a deadlock.
            if (gap < 1f && Math.Abs(speed) < 0.3f && !p.IsFootprintClear(v.Spec, pos, v.State.Yaw, 0f, 0f))
            {
                m_TouchedAt = now;
            }
            if (conflictIn < 3f)
            {
                // Another vehicle is predicted to cross or enter our path (junctions, merges): yield.
                want = conflictIn < 1.6f ? 0f : Math.Min(want, 2.5f);
                m_YieldFor += dt;
            }
            else
            {
                m_YieldFor = 0f;
            }
            if (!bypassing && speed > -0.1f && !FootprintClearAhead(p, v, pos, 0.8f + Math.Max(0f, speed) * 0.5f, Math.Sign(err)))
            {
                // Something is just ahead of the bumper (a pole or bollard in a lot, a car's corner): creep, so a touch
                // stays a light contact instead of costing cargo (observed in game on 2026-10-19: poles hit at 3.7 m/s).
                want = Math.Min(want, 1.2f);
            }
            if (bypassing)
            {
                // Creep, and stop before anything the footprint would touch.
                want = Math.Min(want, heavy ? 1.5f : 2.5f);
                if (!FootprintClearAhead(p, v, pos, 1.2f, Math.Sign(err)))
                {
                    want = 0f;
                }
            }
            // Steering within a comfortable lateral acceleration: about 0.25 g for trucks and buses (heavy cargo is damaged
            // from 0.35 g), 0.38 g for cars. Lateral acceleration is scored as v² tan(steer) / wheelbase; observed in game
            // on 2026-10-19: steering swings of 0.3 rad at 8 m/s cost heavy cargo three harsh events in two seconds.
            float aLat = heavy ? 2.45f : 3.7f;
            float v2 = speed * speed;
            float steerMax = v2 > 1f ? (float)Math.Atan(aLat * v.Spec.WheelBase / v2) : 1f;
            float lockAngle = v.Spec.MaxSteerAngle * MathX.Lerp(1f, v.Spec.HighSpeedSteerFactor, MathX.Clamp01(Math.Abs(speed) / v.Spec.SteerReferenceSpeed));
            float steerIn = lockAngle > 1e-3f ? Math.Min(1f, steerMax / lockAngle) : 1f;
            var f = new InputFrame { Steer = math.clamp(err * 2f, -steerIn, steerIn) };
            if (speed < want - 0.3f)
            {
                // Moving off needs a firm pedal: a loaded truck on a lot surface does not move at all at 0.4 (rolling
                // resistance), which looked like being stuck and sent it reversing into a pole (observed in game on
                // 2026-10-19 in a shipper's lot).
                float minThrottle = Math.Abs(speed) < 1f ? (heavy ? 0.75f : 0.45f) : 0.2f;
                f.Throttle = math.clamp((want - speed) * 0.4f, minThrottle, 1f);
            }
            else if (speed > want + 0.5f)
            {
                // Comfortable braking (about 0.3 g for trucks and buses: harsh braking damages cargo and upsets
                // passengers); full braking only when a collision is imminent.
                // Trucks and buses: firm (about 0.33 g, under the heavy-cargo limit) when a conflict is close, full only
                // when a collision is imminent; a contact costs more cargo than one harsh stop.
                float closing = speed - leaderSpeed;
                bool imminent = conflictIn < 0.6f || (gap < 2f && closing > 1f);
                bool emergency = conflictIn < 1f || (gap < 4f && closing > 0.5f);
                float maxBrake = imminent ? 1f : emergency ? (heavy ? 0.5f : 1f) : heavy ? 0.42f : 0.7f;
                f.Brake = math.clamp((speed - want) * 0.3f, 0.1f, maxBrake);
            }

            DtcLog.Throttled("autopilot", now, 3f, string.Format(CultureInfo.InvariantCulture,
                "autopilot: leg={0} dist={1:0} err={2:0.00} speed={3:0.0} want={4:0.0} gap={5:0} conflict={10:0.0} routePts={6} turnAhead={7:0.00} ground={8} pos={9}",
                legIndex, dist, err, speed, want, gap > 1000f ? -1f : gap, MarkerBoard.Route.Count, turnAhead, v.State.GroundKind, v.State.Position, conflictIn > 1000f ? -1f : conflictIn));
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
                f = Reverse(v, m_ReverseSteer);
            }
            p.Input.Injected = f;
            return false;
        }

        /// <summary>
        /// One step of a K-turn: creep forward on full lock towards the turn, then reverse on the opposite lock, switching
        /// direction before the footprint 1.2 m ahead (or behind) would touch anything. Gives up to recovery after 16 phases.
        /// </summary>
        private void KTurn(PlayerSystem p, ControlledVehicle v, float3 pos, float speed, float now)
        {
            bool heavy = v.Spec.Length > 7f;
            bool switchPhase;
            if (m_KForward)
            {
                switchPhase = now - m_KPhaseSince > 6f || !FootprintClearAhead(p, v, pos, 1.2f, m_KDir);
            }
            else
            {
                switchPhase = now - m_KPhaseSince > 5f || !FootprintClearAhead(p, v, pos, -1.2f, -m_KDir);
            }
            if (switchPhase && now - m_KPhaseSince > 0.6f)
            {
                m_KForward = !m_KForward;
                m_KPhaseSince = now;
                m_KPhases++;
                if (m_KPhases > 16)
                {
                    m_KTurn = false;
                    Recover(p, now, "turn-round did not succeed");
                    return;
                }
            }
            if (m_KForward)
            {
                bool wrongGear = v.State.Gear == Core.Motion.Gear.Reverse && speed < -0.1f;
                p.Input.Injected = wrongGear
                    ? new InputFrame { Throttle = 0f, Brake = 0f, Handbrake = false, Steer = m_KDir }
                    : new InputFrame { Throttle = speed < 1.2f ? (heavy ? 0.9f : 0.6f) : 0f, Steer = m_KDir };
                if (wrongGear)
                {
                    // Stop the reverse roll first (throttle acts as the brake in reverse).
                    p.Input.Injected = new InputFrame { Throttle = heavy ? 0.3f : 0.5f, Steer = m_KDir };
                }
            }
            else
            {
                p.Input.Injected = Reverse(v, -m_KDir);
            }
        }

        /// <summary>True when the vehicle's footprint moved by <paramref name="along"/> metres (and slightly turned) is clear.</summary>
        private static bool FootprintClearAhead(PlayerSystem p, ControlledVehicle v, float3 pos, float along, float turnSign)
        {
            float yaw = v.State.Yaw;
            float3 dir = new float3(math.sin(yaw), 0f, math.cos(yaw));
            return p.IsFootprintClear(v.Spec, pos + dir * along, yaw + turnSign * 0.12f);
        }

        /// <summary>
        /// Reverse input: enough pedal to overcome rolling resistance (a weak pedal does not move a truck at all), released
        /// above a slow reverse speed (1.5 m/s for trucks and buses, 2.5 m/s for cars).
        /// </summary>
        private static InputFrame Reverse(ControlledVehicle v, float steer)
        {
            float limit = v.Spec.Length > 7f ? 1.5f : 2.5f;
            float pedal = -v.State.Speed < limit ? (v.Spec.Length > 7f ? 0.8f : 0.7f) : 0f;
            if (v.State.Gear == Core.Motion.Gear.Forward && Math.Abs(v.State.Speed) < 0.3f)
            {
                pedal = 1f; // engage reverse (held brake at a standstill)
            }
            else if (v.State.Gear == Core.Motion.Gear.Forward && v.State.Speed > 0.3f)
            {
                pedal = v.Spec.Length > 7f ? 0.3f : 0.5f; // stop gently first (harsh braking damages cargo)
            }
            return new InputFrame { Brake = pedal, Steer = steer };
        }

        /// <summary>Diagnostics: what was hit, where it was relative to the vehicle, and what the autopilot had measured.</summary>
        private void ReportImpact(Core.Motion.VehicleStepReport r)
        {
            PlayerSystem p = PlayerSystem.Instance;
            if (p?.Vehicle == null)
            {
                return;
            }
            Vec2 c = p.Vehicle.State.Position.XZ;
            Vec2 fwd = Vec2.FromYaw(p.Vehicle.State.Yaw);
            var list = new List<Obstacle>();
            p.Probe.QueryObstacles(c, p.Vehicle.Spec.Length + 4f, list);
            string hit = "not found";
            foreach (Obstacle o in list)
            {
                if (o.SourceId == r.ImpactSource)
                {
                    Vec2 rel = o.Footprint.Center - c;
                    hit = string.Format(CultureInfo.InvariantCulture, "along={0:0.0} lat={1:0.0} size={2:0.0}x{3:0.0} v=({4:0.0},{5:0.0}) heading-dot={6:0.00}",
                        Vec2.Dot(rel, fwd), Vec2.Dot(rel, fwd.RightPerp), o.Footprint.HalfWidth * 2f, o.Footprint.HalfLength * 2f,
                        Vec2.Dot(o.Velocity, fwd), Vec2.Dot(o.Velocity, fwd.RightPerp), Vec2.Dot(o.Footprint.Forward, fwd));
                }
            }
            DtcLog.Info(string.Format(CultureInfo.InvariantCulture, "autopilot impact: {0:0.0} m/s with {1} #{2} :: {3} :: own speed {4:0.0} steer {5:0.00} lastGap {6:0.0} lastConflict {7:0.0}",
                r.ImpactSpeed, r.ImpactKind, r.ImpactSource, hit, p.Vehicle.State.Speed, p.Vehicle.State.SteerAngle, m_LastGap > 1000f ? -1f : m_LastGap, m_LastConflict > 1000f ? -1f : m_LastConflict));
        }

        private void Unsubscribe()
        {
            if (m_OnImpact != null && PlayerSystem.Instance != null)
            {
                PlayerSystem.Instance.VehicleImpact -= m_OnImpact;
                m_OnImpact = null;
            }
        }

        private void Recover(PlayerSystem p, float now, string why)
        {
            // Space recoveries out: several in quick succession trip the player's recovery-loop protection.
            if (now - m_LastRecoverAt < 30f)
            {
                m_Backouts++;
                m_ReverseUntil = now + 2f;
                m_ReverseSteer = m_Backouts % 2 == 0 ? 1f : -1f;
                return;
            }
            m_LastRecoverAt = now;
            m_Recoveries++;
            m_Log.Add($"recovery:{why}({now - m_Start:0}s)");
            DtcLog.Info("autopilot: recovery (" + why + ")");
            p.Input.Injected = null;
            p.EnterRecovery("test autopilot " + why);
        }

        private static float3 AimPoint(List<float3> route, float3 pos, float speed, float3 fallback, out float turnAhead, out float curvature)
        {
            turnAhead = 0f;
            curvature = 0f;
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
            // Far enough to slow down gently for the corner (30 m in town, more at highway speed).
            float window = Math.Max(30f, speed * speed / 4f + 10f);
            float2 prevDir = d0;
            float prevLen = 0f;
            for (int k = best; k < route.Count - 1 && acc < window; k++)
            {
                float2 dk = math.normalizesafe(route[k + 1].xz - route[k].xz);
                float len = math.distance(route[k].xz, route[k + 1].xz);
                turnAhead = Math.Max(turnAhead, math.acos(math.clamp(math.dot(d0, dk), -1f, 1f)));
                if (k > best && len > 0.5f && prevLen > 0.5f)
                {
                    // Heading change per metre between consecutive route segments (1 / radius).
                    float bend = math.acos(math.clamp(math.dot(prevDir, dk), -1f, 1f));
                    curvature = Math.Max(curvature, bend / Math.Max(2f, 0.5f * (len + prevLen)));
                }
                prevDir = dk;
                prevLen = len;
                acc += math.distance(route[k], route[k + 1]);
            }
            return route[j];
        }

        /// <summary>
        /// Bumper-to-bumper gap to the vehicle ahead in our corridor, and the time until another vehicle is predicted to
        /// enter our path (straight-line prediction of both over 3 s; covers cross traffic at junctions).
        /// </summary>
        private float VehicleAheadGap(PlayerSystem p, ControlledVehicle v, float3 pos, float speed, bool ignoreStopped, out bool nearestStopped, out float conflictIn, out float leaderSpeed)
        {
            nearestStopped = false;
            leaderSpeed = 0f;
            conflictIn = float.MaxValue;
            Vec2 fwd = Vec2.FromYaw(v.State.Yaw);
            Vec2 right = fwd.RightPerp;
            var me = new Vec2(pos.x, pos.z);
            m_Obstacles.Clear();
            p.Probe.QueryObstacles(me + fwd * 20f, 30f, m_Obstacles);
            float gap = float.MaxValue;
            float ownHalf = v.Spec.Length * 0.5f;
            float ownHalfW = v.Spec.Width * 0.5f;
            foreach (Obstacle o in m_Obstacles)
            {
                if (o.Kind != ObstacleKind.Vehicle)
                {
                    continue;
                }
                Vec2 rel = o.Footprint.Center - me;
                float along = Vec2.Dot(rel, fwd);
                bool stopped = o.Velocity.Length < 0.5f;
                // Only vehicles ahead or alongside can cut into our path; one coming up from behind is the follower's
                // business (counting it made us wait for a car that was itself waiting for us).
                // A vehicle going our way faster than we are (overtaking, or cutting in ahead and pulling away) is no
                // conflict: braking for it only upsets the cargo (observed in game: full stops for lane changes ahead).
                float vAlong = Vec2.Dot(o.Velocity, fwd);
                bool pullingAway = vAlong > 0.7f * o.Velocity.Length && vAlong > speed + 0.5f;
                if (!stopped && !pullingAway && along > -ownHalf * 0.5f)
                {
                    // Predicted conflict: both move in straight lines; the other vehicle ends up inside our corridor
                    // just ahead of our front bumper.
                    for (float t = 0.5f; t <= 3f; t += 0.5f)
                    {
                        Vec2 relT = o.Footprint.Center + o.Velocity * t - (me + fwd * (speed * t));
                        float aT = Vec2.Dot(relT, fwd);
                        float lT = Math.Abs(Vec2.Dot(relT, right));
                        if (lT < ownHalfW + o.Footprint.HalfWidth + 0.1f && aT > -ownHalf && aT < ownHalf + o.Footprint.HalfLength + 3f)
                        {
                            // Ignore vehicles in our own lane moving the same way (handled by the gap below).
                            bool sameWay = Vec2.Dot(o.Velocity, fwd) > 0.7f * o.Velocity.Length && Math.Abs(Vec2.Dot(rel, right)) < ownHalfW + o.Footprint.HalfWidth + 0.4f;
                            if (!sameWay)
                            {
                                conflictIn = Math.Min(conflictIn, t);
                            }
                            break;
                        }
                    }
                }
                if (ignoreStopped && stopped)
                {
                    continue;
                }
                // Bumper-to-bumper clearance (long trucks and buses: center distances alone would overlap the car ahead).
                float clearance = along - ownHalf - o.Footprint.HalfLength;
                if (along > 1f && clearance < 40f && Math.Abs(Vec2.Dot(rel, right)) < v.Spec.Width * 0.5f + o.Footprint.HalfWidth - 0.15f && clearance < gap)
                {
                    gap = Math.Max(0f, clearance);
                    nearestStopped = stopped;
                    leaderSpeed = Math.Max(0f, Vec2.Dot(o.Velocity, fwd));
                }
            }
            return gap;
        }
    }
}
