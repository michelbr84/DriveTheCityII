using System;
using System.Collections.Generic;
using DriveTheCityII.CityBridge;
using DriveTheCityII.Core.Career;
using DriveTheCityII.Core.Control;
using DriveTheCityII.Core.Geometry;
using DriveTheCityII.Core.Motion;
using DriveTheCityII.Core.Persistence;
using DriveTheCityII.Core.Text;
using DriveTheCityII.Diagnostics;
using DriveTheCityII.Persistence;
using DriveTheCityII.Player;
using Game;
using Unity.Entities;
using Unity.Mathematics;
using UTime = UnityEngine.Time;

namespace DriveTheCityII.Careers
{
    /// <summary>
    /// Career host in the mod (SPEC-MODULES 4.6 wiring): owns the CareerService instance for the loaded city, advances
    /// the career clock (career-active time: character mode, not paused), feeds the player snapshot, reacts to career
    /// events, spawns career and service vehicles at real places, applies fuel/wear to the driving model, and exposes
    /// the career to the HUD and panels. All career rules live in Core.Career.
    /// </summary>
    public partial class CareerSystem : GameSystemBase
    {
        public static CareerSystem Instance { get; private set; }

        public CareerService Service { get; private set; }
        public CareerWorldAdapter World { get; private set; }
        public bool Disabled { get; private set; }
        public string DisabledReason { get; private set; } = string.Empty;
        public double Clock => m_Clock;

        private double m_Clock;
        private double m_DistanceAccum;
        private double m_ImpactMax;
        private float m_LastSpeed;
        private MissionObjects m_Objects;
        private JsonValue m_PreservedCareer;
        private bool m_UiRegistered;
        private string m_LastRouteTarget;
        private float m_NextRoute;
        private readonly Dictionary<string, VehicleSpec> m_BaseSpecs = new Dictionary<string, VehicleSpec>();

        protected override void OnCreate()
        {
            base.OnCreate();
            Instance = this;
            PersistenceRuntime.CareerProvider = ProvideCareer;
            PersistenceRuntime.CareerRestorer = Restore;
            PersistenceRuntime.PlayerProvider = ProvidePlayer;
        }

        private PlayerSystem P => PlayerSystem.Instance;

        private void EnsureWorld()
        {
            if (World == null && P?.Probe != null)
            {
                World = new CareerWorldAdapter(P.Probe);
                m_Objects = new MissionObjects(P.Catalog);
                P.DriveDistance += d => m_DistanceAccum += d;
                P.VehicleImpact += r => m_ImpactMax = Math.Max(m_ImpactMax, r.ImpactSpeed);
                P.StateChanged += OnPlayerStateChanged;
            }
        }

        // ---------------------------------------------------------------- persistence wiring

        private JsonValue ProvideCareer()
        {
            if (Disabled)
            {
                return m_PreservedCareer; // keep unreadable or newer data unchanged in the save
            }
            return Service?.ToJson();
        }

        private JsonValue ProvidePlayer() => JsonValue.Object().Set("careerClock", m_Clock);

        /// <summary>Called by Persistence after a city loaded (validated envelope content, or null for a new career).</summary>
        private void Restore(JsonValue career, EnvelopeStatus status, string note)
        {
            EnsureWorld();
            m_Objects?.RemoveAll();
            Disabled = false;
            DisabledReason = string.Empty;
            m_PreservedCareer = null;
            JsonValue player = PersistenceRuntime.LastLoadedPlayer;
            m_Clock = player != null ? player["careerClock"].AsDouble(0) : 0;
            if (status == EnvelopeStatus.NewerSchemaRefused)
            {
                Service = null;
                Disabled = true;
                DisabledReason = "Career data was saved by a newer version of the mod; it is kept unchanged.";
                return;
            }
            if (career == null)
            {
                if (status == EnvelopeStatus.Ok || status == EnvelopeStatus.NoCareerData)
                {
                    Service = CareerService.CreateNew(PersistenceRuntime.CityId, Seed(PersistenceRuntime.CityId), World, m_Clock);
                    DtcLog.Info("Career: new career created for this city");
                }
                else
                {
                    Service = null;
                    Disabled = true;
                    DisabledReason = note;
                }
                return;
            }
            Service = CareerService.FromJson(career, World, out CareerLoadResult res);
            if (Service == null || !res.IsOk)
            {
                m_PreservedCareer = career;
                Service = null;
                Disabled = true;
                DisabledReason = "Career data failed validation and was preserved unchanged: " + (res?.ToString() ?? "unknown");
                DtcLog.Warn("Career: " + DisabledReason);
                return;
            }
            // The career's own persisted clock is authoritative; a clock that went backwards would be skipped by Tick.
            m_Clock = Math.Max(m_Clock, Service.ClockSeconds);
            DtcLog.Info($"Career: restored (clock {m_Clock:0}s, balance {Service.GetWallet(0).BalanceCents} cents)");
        }

        private static ulong Seed(string cityId)
        {
            ulong h = 1469598103934665603UL;
            foreach (char c in cityId ?? "city")
            {
                h ^= c;
                h *= 1099511628211UL;
            }
            return h;
        }

        // ---------------------------------------------------------------- frame update

        protected override void OnGamePreload(Colossal.Serialization.Entities.Purpose purpose, GameMode mode)
        {
            base.OnGamePreload(purpose, mode);
            m_Objects?.RemoveAll();
            m_LastRouteTarget = null;
            MarkerBoard.Clear();
        }

        protected override void OnUpdate()
        {
            if (!HostFlow.IsCityReady || P == null)
            {
                return;
            }
            EnsureWorld();
            if (!m_UiRegistered && UI.HudUISystem.Instance != null)
            {
                CareerUi.Register(this);
                m_UiRegistered = true;
            }
            if (Service == null && !Disabled && World != null)
            {
                // A city without a load event for our system (e.g. mod added to an existing session): start fresh.
                Service = CareerService.CreateNew(PersistenceRuntime.CityId ?? "city", Seed(PersistenceRuntime.CityId), World, m_Clock);
            }
            PlayerState state = P.State;
            if (state == PlayerState.Building || Service == null)
            {
                return;
            }
            bool paused = (P.StateMachine.Overlays & (PlayerOverlay.Paused | PlayerOverlay.Menu)) != 0;
            bool pauseCareer = Settings.DriveTheCityIISettings.Instance == null || Settings.DriveTheCityIISettings.Instance.PauseCareerWhenPaused;
            float dt = math.clamp(UTime.unscaledDeltaTime, 0f, 0.25f);
            World.Now = UTime.realtimeSinceStartup;
            World.EstimatesThisFrame = 0;
            if (!(paused && pauseCareer))
            {
                m_Clock += dt;
                PlayerSnapshot snap = BuildSnapshot(dt);
                IReadOnlyList<CareerEvent> events;
                try
                {
                    events = Service.Tick(snap, m_Clock);
                }
                catch (Exception ex)
                {
                    DtcLog.Error("Career tick failed (career paused for this session)", ex);
                    Disabled = true;
                    DisabledReason = "internal error; see log";
                    m_PreservedCareer = SafeJson();
                    Service = null;
                    return;
                }
                HandleEvents(events);
            }
            ApplyVehiclePerformance();
            UpdatePresentation(dt, paused);
        }

        private JsonValue SafeJson()
        {
            try
            {
                return Service?.ToJson();
            }
            catch
            {
                return null;
            }
        }

        private PlayerSnapshot BuildSnapshot(float dt)
        {
            var snap = new PlayerSnapshot();
            ControlledVehicle v = P.Vehicle;
            switch (P.State)
            {
                case PlayerState.OnFoot:
                    snap.Mode = PlayerMode.OnFoot;
                    break;
                case PlayerState.Driving:
                    snap.Mode = PlayerMode.Driving;
                    break;
                default:
                    snap.Mode = PlayerMode.Transition;
                    break;
            }
            if (snap.Mode == PlayerMode.Driving && v != null)
            {
                snap.Position = CareerWorldAdapter.ToPoint(v.State.Position.ToF3());
                snap.SpeedMps = Math.Abs(v.State.Speed);
                snap.VehicleId = v.Kind == ActorKind.CareerVehicle ? v.CareerVehicleId : null;
                snap.HeadingDegrees = v.State.Yaw * MathX.Rad2Deg;
                float accel = dt > 0f ? (v.State.Speed - m_LastSpeed) / dt : 0f;
                snap.LongitudinalAccel = accel;
                snap.LateralAccel = Math.Abs(v.State.Speed) * Math.Abs(v.State.Speed) * Math.Abs((float)Math.Tan(v.State.SteerAngle)) / Math.Max(1f, v.Spec.WheelBase);
                m_LastSpeed = v.State.Speed;
            }
            else
            {
                snap.Position = CareerWorldAdapter.ToPoint(P.Avatar.Position.ToF3());
                snap.SpeedMps = P.Avatar.HorizontalSpeed;
                snap.HeadingDegrees = P.Avatar.Yaw * MathX.Rad2Deg;
                m_LastSpeed = 0f;
            }
            snap.Stationary = snap.SpeedMps < CareerBalance.ServiceSpeedMps;
            snap.DistanceDrivenMeters = m_DistanceAccum;
            snap.ImpactSpeed = m_ImpactMax;
            m_DistanceAccum = 0;
            m_ImpactMax = 0;
            return snap;
        }

        private void OnPlayerStateChanged(TransitionRecord r)
        {
            if (r.To == PlayerState.Building)
            {
                m_Objects?.RemoveAll();
                MarkerBoard.Clear();
                m_LastRouteTarget = null;
            }
        }

        // ---------------------------------------------------------------- events

        private void HandleEvents(IReadOnlyList<CareerEvent> events)
        {
            if (events == null)
            {
                return;
            }
            foreach (CareerEvent e in events)
            {
                switch (e.Kind)
                {
                    case CareerEventKind.Notification:
                    case CareerEventKind.Settled:
                    case CareerEventKind.VehicleImmobilized:
                    case CareerEventKind.CompanySettled:
                        if (!string.IsNullOrEmpty(e.Text) || !string.IsNullOrEmpty(e.Key))
                        {
                            P.Say(CareerText.Of(e.Key, e.Text, e.Args));
                        }
                        break;
                    case CareerEventKind.ServiceVehicleProvided:
                        SpawnServiceVehicle(e);
                        break;
                    case CareerEventKind.VehicleReleased:
                        RemoveWorldVehicle(e.VehicleId);
                        break;
                    case CareerEventKind.MissionStateChanged:
                        if (e.ToState == MissionState.Completed || e.ToState == MissionState.Failed || e.ToState == MissionState.Cancelled ||
                            e.ToState == MissionState.Invalidated || e.ToState == MissionState.Expired)
                        {
                            m_LastRouteTarget = null;
                            MarkerBoard.Clear();
                        }
                        if (!string.IsNullOrEmpty(e.Text))
                        {
                            P.Say(CareerText.Of(e.Key, e.Text, e.Args));
                        }
                        break;
                }
                DtcLog.Info("Career event: " + e);
            }
        }

        // ---------------------------------------------------------------- vehicles

        public ControlledVehicle FindWorldVehicle(string careerVehicleId)
        {
            if (string.IsNullOrEmpty(careerVehicleId))
            {
                return null;
            }
            if (P.Vehicle != null && P.Vehicle.CareerVehicleId == careerVehicleId)
            {
                return P.Vehicle;
            }
            foreach (ControlledVehicle v in P.ParkedVehicles)
            {
                if (v.CareerVehicleId == careerVehicleId)
                {
                    return v;
                }
            }
            return null;
        }

        /// <summary>Places a career vehicle in the world on a road lane near a point (curb side when possible).</summary>
        public bool SpawnCareerVehicleNear(VehicleView view, float3 near, out string error)
        {
            error = null;
            if (FindWorldVehicle(view.Id) != null)
            {
                return true;
            }
            if (!P.Probe.FindNearestLanePoint(near, 40f, CityWorldProbe.LaneKind.Parking, out float3 at, out float3 tan, out _) &&
                !P.Probe.FindNearestLanePoint(near, 120f, CityWorldProbe.LaneKind.Road, out at, out tan, out _))
            {
                error = Loc.T("career.noRoadNearby");
                return false;
            }
            string category = string.IsNullOrEmpty(view.PrefabCategory) ? "car" : view.PrefabCategory;
            if (!P.SpawnCareerVehicle(category, at, math.atan2(tan.x, tan.z), view.Id, out ControlledVehicle cv))
            {
                error = P.LastMessage;
                return false;
            }
            m_BaseSpecs[view.Id] = cv.Spec.Clone();
            return true;
        }

        private void SpawnServiceVehicle(CareerEvent e)
        {
            VehicleView view = Service.GetVehicle(e.VehicleId);
            if (view == null)
            {
                return;
            }
            float3 near = P.Avatar.Position.ToF3();
            if (!string.IsNullOrEmpty(e.PlaceId) && CityServices.Places.TryGet(e.PlaceId, out CityPlace place))
            {
                CityServices.Places.RefineAccess(P.Probe, place);
                near = place.RoadAccess;
            }
            if (!SpawnCareerVehicleNear(view, near, out string err))
            {
                P.Say(err);
            }
            else
            {
                P.Say(Loc.T("career.serviceVehicleReady", view.Name));
                MarkerBoard.Markers.Add(new WorldMarker { Position = near, Style = MarkerStyle.Vehicle, Radius = 4f });
            }
        }

        public void RemoveWorldVehicle(string careerVehicleId)
        {
            ControlledVehicle v = FindWorldVehicle(careerVehicleId);
            if (v == null || v == P.Vehicle)
            {
                return;
            }
            P.RemoveParkedVehicle(v);
        }

        /// <summary>Applies wear/fuel performance from Career to the controlled career vehicle (never blocks braking).</summary>
        private void ApplyVehiclePerformance()
        {
            ControlledVehicle v = P.Vehicle;
            if (v == null || v.Kind != ActorKind.CareerVehicle || string.IsNullOrEmpty(v.CareerVehicleId))
            {
                P.ThrottleBlocked = false;
                return;
            }
            VehiclePerformance perf = Service.GetPerformance(v.CareerVehicleId);
            if (!m_BaseSpecs.TryGetValue(v.CareerVehicleId, out VehicleSpec baseSpec))
            {
                baseSpec = v.Spec.Clone();
                m_BaseSpecs[v.CareerVehicleId] = baseSpec;
            }
            VehicleSpec s = baseSpec.Clone();
            double accel = perf.AccelerationMultiplier > 0 ? perf.AccelerationMultiplier : 1;
            double brake = perf.BrakingMultiplier > 0 ? perf.BrakingMultiplier : 1;
            s.Acceleration = (float)(baseSpec.Acceleration * accel);
            s.BrakeDeceleration = (float)(baseSpec.BrakeDeceleration * Math.Max(0.6, brake));
            if (perf.MaxSpeedMps > 1)
            {
                s.MaxSpeed = (float)Math.Min(baseSpec.MaxSpeed, perf.MaxSpeedMps);
            }
            v.Spec = s;
            P.ThrottleBlocked = perf.Immobilized;
        }
    }
}
