using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using DriveTheCityII.Bridge;
using DriveTheCityII.CityBridge;
using DriveTheCityII.Core.Career;
using DriveTheCityII.Core.Control;
using DriveTheCityII.Core.Geometry;
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
                    IReadOnlyList<MissionOffer> offers = s.RefreshOffers(kind, CareerWorldAdapter.ToPoint(CareerUi.PlayerPos()), C.Clock);
                    return new InstantOp(offers.Count > 0, $"{offers.Count} offers: " + string.Join(" | ", offers.Select((o, i) =>
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
                    string vid = P.Vehicle != null && P.Vehicle.Kind == ActorKind.CareerVehicle ? P.Vehicle.CareerVehicleId : null;
                    CareerResult r = s.Accept(offers[idx].Id, vid, C.Clock);
                    return new InstantOp(r.IsOk, $"accept {offers[idx].Title}: {r.Code} {r.Text}");
                }
                case "ui":
                    UI.HudUISystem.Instance.InvokeAction(string.Join(":", a.Skip(2)));
                    return new InstantOp(true, "ui " + string.Join(":", a.Skip(2)) + " :: " + P.LastMessage);
                case "drive":
                    return new MissionAutopilotOp(a.Length > 2 ? float.Parse(a[2], CultureInfo.InvariantCulture) : 300f);
                case "grant":
                    return new InstantOp(true, s.DebugGrant(long.Parse(a[2], CultureInfo.InvariantCulture), C.Clock).Text);
                case "json":
                    return new InstantOp(true, s.ToJsonText().Length + " chars");
                default:
                    return new InstantOp(false, "unknown career command");
            }
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

    /// <summary>Drives the active mission to completion with simulated input (test autopilot; see class summary above).</summary>
    public sealed class MissionAutopilotOp : IBridgeOp
    {
        private readonly float m_End;
        private readonly float m_Start;
        private int m_LastLeg = -2;
        private readonly List<string> m_Log = new List<string>();
        private float m_Stuck;
        private float3 m_LastPos;

        public MissionAutopilotOp(float seconds)
        {
            m_Start = Time.realtimeSinceStartup;
            m_End = m_Start + seconds;
        }

        public bool Ok { get; private set; }
        public string Result { get; private set; } = string.Empty;

        public bool Update()
        {
            PlayerSystem p = PlayerSystem.Instance;
            CareerSystem c = CareerSystem.Instance;
            ActiveMissionView m = c.Service.GetActiveMission();
            if (m == null || string.IsNullOrEmpty(m.MissionId))
            {
                p.Input.Injected = null;
                Ok = true;
                Result = $"mission finished after {Time.realtimeSinceStartup - m_Start:0}s; legs: {string.Join(" > ", m_Log)} :: {CareerBridge.Status()}";
                return true;
            }
            if (Time.realtimeSinceStartup > m_End)
            {
                p.Input.Injected = null;
                Ok = false;
                Result = $"timeout; legs: {string.Join(" > ", m_Log)} :: {CareerBridge.Status()}";
                return true;
            }
            if (m.CurrentLegIndex != m_LastLeg)
            {
                m_LastLeg = m.CurrentLegIndex;
                m_Log.Add($"{m.CurrentLegKind}@{m.TargetPlaceName}({Time.realtimeSinceStartup - m_Start:0}s)");
            }
            if (p.State != PlayerState.Driving || p.Vehicle == null)
            {
                p.Input.Injected = null;
                return false;
            }
            float3 pos = p.Vehicle.State.Position.ToF3();
            float3 target = CareerWorldAdapter.ToF3(m.TargetAccess);
            float dist = math.distance(pos.xz, target.xz);
            float radius = 8f;
            foreach (MissionLegView l in m.Legs)
            {
                if (l.Index == m.CurrentLegIndex && l.Radius > 0)
                {
                    radius = (float)Math.Max(4.0, l.Radius * 0.6);
                }
            }
            if (dist < radius || m.CurrentLegState == LegState.Servicing)
            {
                p.Input.Injected = new InputFrame { Brake = 1f };
                m_Stuck = 0f;
                return false;
            }
            InputFrame f = FollowRoute(p, pos, Mathf.Clamp(dist * 0.35f, 4f, 12f));
            // Unstick: if not progressing, reverse briefly.
            if (math.distance(pos, m_LastPos) < 0.05f)
            {
                m_Stuck += Time.unscaledDeltaTime;
            }
            else
            {
                m_Stuck = 0f;
            }
            m_LastPos = pos;
            if (m_Stuck > 4f && m_Stuck < 6f)
            {
                f = new InputFrame { Brake = 1f, Steer = -f.Steer };
            }
            else if (m_Stuck >= 6f)
            {
                m_Stuck = 0f;
            }
            p.Input.Injected = f;
            return false;
        }

        private static InputFrame FollowRoute(PlayerSystem p, float3 pos, float targetSpeed)
        {
            var f = new InputFrame();
            List<float3> route = MarkerBoard.Route;
            float3 aim = pos;
            if (route.Count >= 2)
            {
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
                float acc = 0f;
                int j = best;
                while (j < route.Count - 1 && acc < 12f)
                {
                    acc += math.distance(route[j], route[j + 1]);
                    j++;
                }
                aim = route[j];
            }
            float desired = math.atan2(aim.x - pos.x, aim.z - pos.z);
            float err = MathX.WrapAngle(desired - p.Vehicle.State.Yaw);
            f.Steer = math.clamp(err * 2.0f, -1f, 1f);
            float speed = p.Vehicle.State.Speed;
            float want = Math.Abs(err) > 0.6f ? Math.Min(targetSpeed, 5f) : targetSpeed;
            if (speed < want)
            {
                f.Throttle = math.clamp((want - speed) * 0.4f, 0.15f, 1f);
            }
            else
            {
                f.Brake = math.clamp((speed - want) * 0.3f, 0f, 1f);
            }
            return f;
        }
    }
}
