using System;
using System.Collections.Generic;
using DriveTheCityII.CityBridge;
using DriveTheCityII.Core.Control;
using DriveTheCityII.Core.Geometry;
using DriveTheCityII.Core.Motion;
using DriveTheCityII.Core.World;
using DriveTheCityII.Core.Text;
using DriveTheCityII.Diagnostics;
using Game;
using Game.Common;
using Game.Objects;
using Game.Prefabs;
using Game.Rendering;
using Game.UI.InGame;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Transform = Game.Objects.Transform;
using UTime = UnityEngine.Time;

namespace DriveTheCityII.Player
{
    public enum CameraView
    {
        ThirdPerson = 0,
        FirstPerson = 1,
        Chase = 2,
        Hood = 3,
    }

    /// <summary>The vehicle the player currently controls or has parked nearby.</summary>
    public sealed class ControlledVehicle
    {
        public Entity Entity;
        public ActorKind Kind;
        public int RecordId;
        public CarPrefabInfo Prefab;
        public VehicleSpec Spec;
        public VehicleState State;
        public bool SpawnPending;
        public bool Lost;
        public string CareerVehicleId;
        public bool LightsOn = true;
    }

    /// <summary>
    /// The Player module (SPEC-MODULES 4.3): ControlAuthority state machine, avatar and vehicle movement authority,
    /// camera ownership, input routing, transitions, Recovery, and the simulation-speed policy. It runs every frame in
    /// MainLoop before PreRenderSystem, so the camera system and the renderer see this frame's poses.
    /// </summary>
    public partial class PlayerSystem : GameSystemBase
    {
        public static PlayerSystem Instance { get; private set; }

        private CityWorldProbe m_Probe;
        private readonly PrefabCatalog m_Catalog = new PrefabCatalog();
        private readonly CameraRig m_Camera = new CameraRig();
        private readonly InputRouter m_Input = new InputRouter();
        private readonly ControlStateMachine m_Sm = new ControlStateMachine();
        private readonly CleanupStack m_Session = new CleanupStack();
        private AvatarMotor m_Motor;
        private VehicleController m_VehCtrl;
        private PrefabSystem m_PrefabSystem;
        private GameScreenUISystem m_Screens;
        private ActorOpsSystem m_ActorOps;

        private AvatarState m_Avatar;
        private Entity m_AvatarEntity = Entity.Null;
        private bool m_AvatarSpawnPending;
        private bool m_AvatarVisible;
        private int m_AvatarSeed;
        private ControlledVehicle m_Vehicle;
        private readonly List<ControlledVehicle> m_ParkedVehicles = new List<ControlledVehicle>();
        private int m_NextRecordId = 1;

        private float m_FovBase = 62f;
        private float m_CamYaw;
        private float m_CamPitch = -8f;
        private CameraView m_FootView = CameraView.ThirdPerson;
        private CameraView m_DriveView = CameraView.Chase;
        private float m_ChaseYawOffset;
        private float3 m_SmoothedCamPos;
        private bool m_CamInitialized;

        // Transition bookkeeping.
        private float3 m_TransStart;
        private float3 m_TransEnd;
        private float m_TransDuration;
        private float m_TransTime;
        private float3 m_PreEntryPos;
        private int m_RecoverySearches;

        private float m_PriorSpeed = 1f;
        private bool m_SpeedOverridden;
        private bool m_RespawnAfterSave;
        private float m_StuckTimer;
        private float3 m_LastProgressPos;

        public ControlStateMachine StateMachine => m_Sm;
        public PlayerState State => m_Sm.State;
        public AvatarState Avatar => m_Avatar;
        public ControlledVehicle Vehicle => m_Vehicle;
        public InputRouter Input => m_Input;
        public CityWorldProbe Probe => m_Probe;
        public PrefabCatalog Catalog => m_Catalog;
        public bool Sandbox { get; set; } = true;
        public string LastMessage { get; private set; } = string.Empty;
        public float LastMessageTime { get; private set; }
        public CameraView ActiveView => m_Sm.State == PlayerState.Driving ? m_DriveView : m_FootView;

        /// <summary>Raised on gameplay messages for the HUD (blocked actions with a next step, REQ-UI-003).</summary>
        public event Action<string> Message;

        /// <summary>Raised on every state change (Career observes Boarded/arrivals through it).</summary>
        public event Action<TransitionRecord> StateChanged;

        protected override void OnCreate()
        {
            base.OnCreate();
            Instance = this;
            m_Probe = new CityWorldProbe(this);
            m_Motor = new AvatarMotor(new AvatarSpec());
            m_VehCtrl = new VehicleController(new VehicleSpec());
            m_PrefabSystem = World.GetOrCreateSystemManaged<PrefabSystem>();
            m_Screens = World.GetOrCreateSystemManaged<GameScreenUISystem>();
            m_ActorOps = World.GetOrCreateSystemManaged<ActorOpsSystem>();
            ActorOpsSystem.ActorsRemovedForSave += OnActorsRemovedForSave;
            m_Sm.StateChanged += r =>
            {
                DtcLog.Info($"Player: {r}");
                StateChanged?.Invoke(r);
            };
        }

        protected override void OnDestroy()
        {
            ActorOpsSystem.ActorsRemovedForSave -= OnActorsRemovedForSave;
            ForceBuilding("system destroyed");
            m_Input.Dispose();
            base.OnDestroy();
        }

        protected override void OnGamePreload(Colossal.Serialization.Entities.Purpose purpose, GameMode mode)
        {
            base.OnGamePreload(purpose, mode);
            // TR-13: Loading/Unloading always tears down to Building; the old world's entities are discarded.
            m_Sm.SetOverlay(PlayerOverlay.Loading, true);
            ForceBuilding("loading " + purpose);
            m_ParkedVehicles.Clear();
            m_Catalog.Invalidate();
        }

        protected override void OnGameLoadingComplete(Colossal.Serialization.Entities.Purpose purpose, GameMode mode)
        {
            base.OnGameLoadingComplete(purpose, mode);
            m_Sm.SetOverlay(PlayerOverlay.Loading, false);
        }

        private void OnActorsRemovedForSave()
        {
            m_AvatarEntity = Entity.Null;
            if (m_Vehicle != null && m_Vehicle.Kind == ActorKind.CareerVehicle)
            {
                m_Vehicle.Entity = Entity.Null;
            }
            foreach (ControlledVehicle p in m_ParkedVehicles)
            {
                if (p.Kind == ActorKind.CareerVehicle)
                {
                    p.Entity = Entity.Null;
                }
            }
            m_RespawnAfterSave = true;
        }

        public void Say(string text)
        {
            LastMessage = text;
            LastMessageTime = UTime.realtimeSinceStartup;
            DtcLog.Info("Player message: " + text);
            Message?.Invoke(text);
        }

        // =====================================================================================================
        // Frame update
        // =====================================================================================================

        protected override void OnUpdate()
        {
            if (!HostFlow.IsCityReady || Runtime.ConflictDetected)
            {
                if (m_Sm.State != PlayerState.Building)
                {
                    ForceBuilding(Runtime.ConflictDetected ? "conflicting mod" : "city not ready");
                }
                if (Runtime.ConflictDetected && m_Input.ToggleWasPressed())
                {
                    Say(Loc.T("msg.conflict"));
                }
                return;
            }

            CompleteDependency();
            m_Probe.BeginFrame();
            m_Catalog.EnsureBuilt(EntityManager, m_PrefabSystem);
            if (m_Sm.State != PlayerState.Building)
            {
                CityServices.Refresh(EntityManager, m_Probe, UTime.realtimeSinceStartup);
            }
            MarkerBoard.Visible = m_Sm.State != PlayerState.Building;

            Settings.DriveTheCityIISettings settings = Settings.DriveTheCityIISettings.Instance;
            if (settings != null)
            {
                Sandbox = settings.SandboxBorrowing;
                m_FovBase = settings.FieldOfView;
                if (settings.SafeDisable)
                {
                    // Safe-disable (SPEC-MODULES 4.9): normal cleanup back to Building, then stay inert.
                    if (m_Sm.State != PlayerState.Building)
                    {
                        ForceBuilding("safe-disable");
                        Say(Loc.T("msg.safeDisabled"));
                    }
                    if (m_Input.ToggleWasPressed())
                    {
                        Say(Loc.T("msg.safeDisabled"));
                    }
                    return;
                }
            }
            float dt = math.clamp(UTime.unscaledDeltaTime, 0f, 0.1f);
            bool menu = m_Screens != null && m_Screens.isMenuActive;
            bool paused = HostFlow.SimulationSpeed <= 0f || !Application.isFocused;
            m_Sm.SetOverlay(PlayerOverlay.Menu, menu);
            m_Sm.SetOverlay(PlayerOverlay.Paused, paused);

            if (m_Sm.State == PlayerState.Building)
            {
                if (!menu && m_Input.ToggleWasPressed())
                {
                    EnterCharacterMode();
                }
                return;
            }

            try
            {
                FrameCharacterMode(dt, menu, paused);
            }
            catch (Exception ex)
            {
                DtcLog.Error("Player fault; forcing Building (TR-13)", ex);
                ForceBuilding("fault: " + ex.Message);
            }
        }

        private Game.Tools.ToolRaycastSystem m_ToolRaycast;

        /// <summary>Disables the city-builder hover/selection raycast while in character mode (as photo mode does).</summary>
        private void SetToolHover(bool disabled)
        {
            m_ToolRaycast ??= World.GetExistingSystemManaged<Game.Tools.ToolRaycastSystem>();
            if (m_ToolRaycast == null)
            {
                return;
            }
            if (disabled)
            {
                m_ToolRaycast.raycastFlags |= Game.Common.RaycastFlags.FreeCameraDisable;
            }
            else
            {
                m_ToolRaycast.raycastFlags &= ~Game.Common.RaycastFlags.FreeCameraDisable;
            }
        }

        private void FrameCharacterMode(float dt, bool menu, bool paused)
        {
            SetToolHover(true);
            bool panel = UI.HudUISystem.Instance != null && UI.HudUISystem.Instance.PanelOpen;
            m_Input.SetCursorLock(!menu && !panel);
            InputFrame input = m_Input.Read(uiBlocking: menu);
            if (input.CareerPanelPressed && !menu)
            {
                UI.HudUISystem.Instance?.TogglePanel();
            }
            // Esc: the Shortcuts map (which owns "Pause Menu") is blocked in character mode, so Esc is handled here:
            // close our panel first, otherwise open the game's pause menu (save, options, quit stay reachable).
            if (!menu && UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                if (panel)
                {
                    UI.HudUISystem.Instance?.SetPanel(false);
                }
                else
                {
                    m_Screens?.SetScreen(GameScreenUISystem.GameScreen.PauseMenu);
                }
            }
            if (m_Input.Injected.HasValue == false && input.FreeCursorHeld)
            {
                m_Input.SetCursorLock(false);
            }

            HandleRespawns();
            ValidateEntities();

            if (input.TogglePressed && !menu)
            {
                RequestReturnToBuilding();
                if (m_Sm.State == PlayerState.Building)
                {
                    return;
                }
            }
            if (input.CameraViewPressed)
            {
                ToggleView();
            }
            if (input.LightsPressed && m_Vehicle != null)
            {
                m_Vehicle.LightsOn = !m_Vehicle.LightsOn;
            }

            // Mouse/gamepad look is always applied (also while paused) so the player can look around.
            m_CamYaw = MathX.WrapAngle(m_CamYaw + input.LookX * MathX.Deg2Rad);
            m_CamPitch = math.clamp(m_CamPitch + input.LookY, -70f, 70f);

            bool frozen = paused || menu;
            if (!frozen && m_Sm.Tick(dt))
            {
                OnTransitionTimeout();
            }

            switch (m_Sm.State)
            {
                case PlayerState.OnFoot:
                    UpdateOnFoot(input, frozen ? 0f : dt);
                    break;
                case PlayerState.EnteringVehicle:
                    UpdateEntering(frozen ? 0f : dt);
                    break;
                case PlayerState.Driving:
                    UpdateDriving(input, frozen ? 0f : dt);
                    break;
                case PlayerState.ExitingVehicle:
                    UpdateExiting(frozen ? 0f : dt);
                    break;
                case PlayerState.Recovery:
                    UpdateRecovery();
                    break;
            }

            WritePoses(frozen);
            UpdateCamera(dt);
        }

        // =====================================================================================================
        // TR-01 / TR-02 / TR-09 / TR-13
        // =====================================================================================================

        public bool EnterCharacterMode()
        {
            if (m_Sm.State != PlayerState.Building)
            {
                return true;
            }
            if (Runtime.ConflictDetected)
            {
                Say(Loc.T("msg.conflict"));
                return false;
            }
            if (!m_Probe.IsReady)
            {
                Say(Loc.T("msg.notReady"));
                return false;
            }
            if (m_Catalog.HumanCount == 0)
            {
                Say(Loc.T("msg.noCharacter"));
                return false;
            }
            if (!m_Camera.TryGetGameplayView(out Vector3 pivot, out Vector3 rot))
            {
                Say(Loc.T("msg.cameraUnavailable"));
                return false;
            }
            if (!FindSpawnPoint(new float3(pivot.x, pivot.y, pivot.z), out float3 spawn, out float yaw))
            {
                Say(Loc.T("msg.noWalkable"));
                return false;
            }

            m_Avatar = new AvatarState();
            if (!m_Motor.TryPlace(m_Probe, ref m_Avatar, new Vec2(spawn.x, spawn.z), spawn.y + 1f))
            {
                Say(Loc.T("msg.spawnBlocked"));
                return false;
            }
            m_Avatar.Yaw = yaw;
            m_CamYaw = yaw;
            m_CamPitch = -8f;
            m_CamInitialized = false;

            if (m_Sm.Request(PlayerState.OnFoot, "player command") != TransitionResultCode.Ok)
            {
                Say(Loc.T("msg.cannotEnterMode"));
                return false;
            }

            // Side effects in order, each registering its cleanup (SPEC-CONTROL TR-01, section 7).
            m_PriorSpeed = HostFlow.SimulationSpeed;
            m_SpeedOverridden = true;
            m_Session.Push("speed", RestoreSpeed);
            if (HostFlow.SimulationSpeed <= 0f)
            {
                HostFlow.SimulationSpeed = 1f;
            }
            m_AvatarSeed = UnityEngine.Random.Range(0, 100000);
            SpawnAvatarEntity();
            m_Session.Push("avatar", DespawnAvatarEntity);
            m_Camera.Take();
            m_Session.Push("camera", m_Camera.Release);
            m_Input.Activate();
            m_Session.Push("input", m_Input.Deactivate);
            SetToolHover(true);
            m_Session.Push("toolHover", () => SetToolHover(false));
            m_Session.Push("vehicles", StowAllVehicles);
            m_StuckTimer = 0f;
            m_LastProgressPos = m_Avatar.Position.ToF3();
            Say(Loc.T("msg.enterMode"));
            // Onboarding (SPEC-ONBOARDING): the first time, open the help panel; the option turns itself off.
            Settings.DriveTheCityIISettings st = Settings.DriveTheCityIISettings.Instance;
            if (st != null && st.ShowOnboarding && UI.HudUISystem.Instance != null)
            {
                UI.HudUISystem.Instance.OpenTab("help");
                st.ShowOnboarding = false;
                st.ApplyAndSave();
            }
            return true;
        }

        public void RequestReturnToBuilding()
        {
            if (m_Sm.State == PlayerState.Driving && !VehicleController.IsStationary(m_Vehicle.State, m_Vehicle.Spec))
            {
                Say(Loc.T("msg.stopFirst"));
                return;
            }
            if (m_Sm.State == PlayerState.EnteringVehicle || m_Sm.State == PlayerState.ExitingVehicle || m_Sm.State == PlayerState.Recovery)
            {
                Say(Loc.T("msg.waitAction"));
                return;
            }
            PlayerState from = m_Sm.State;
            m_Sm.Request(PlayerState.Building, "player command");
            if (m_Sm.State == PlayerState.Building)
            {
                RunTeardown();
                DtcLog.Info($"Returned to city building from {from}");
            }
        }

        /// <summary>TR-13: forced return to Building from any state; runs every cleanup.</summary>
        public void ForceBuilding(string reason)
        {
            if (m_Sm.State == PlayerState.Building && m_Session.Count == 0)
            {
                return;
            }
            m_Sm.ForceBuilding(reason);
            RunTeardown();
        }

        private void RunTeardown()
        {
            int released = m_Session.ReleaseAll((name, ex) => DtcLog.Error($"Cleanup step '{name}' failed", ex));
            DtcLog.Info($"Teardown complete ({released} resources released)");
        }

        private void RestoreSpeed()
        {
            if (!m_SpeedOverridden)
            {
                return;
            }
            m_SpeedOverridden = false;
            // Not restored while a new city is loading (TR-13 note): the old value belongs to the old city.
            if (HostFlow.IsCityReady && (m_Sm.Overlays & PlayerOverlay.Loading) == 0)
            {
                HostFlow.SimulationSpeed = m_PriorSpeed;
            }
        }

        private void StowAllVehicles()
        {
            if (m_Vehicle != null)
            {
                ReleaseVehicle(m_Vehicle, handBackSpeed: 0f);
                m_Vehicle = null;
            }
            foreach (ControlledVehicle p in m_ParkedVehicles)
            {
                ReleaseVehicle(p, 0f);
            }
            m_ParkedVehicles.Clear();
        }

        private void ReleaseVehicle(ControlledVehicle v, float handBackSpeed)
        {
            Entity e = v.Entity;
            if (e == Entity.Null)
            {
                return;
            }
            if (v.Kind == ActorKind.BorrowedNativeVehicle)
            {
                ActorOpsSystem.Enqueue(em => ActorWriter.HandBackNative(em, e, handBackSpeed));
            }
            else
            {
                ActorOpsSystem.Enqueue(em =>
                {
                    if (em.Exists(e) && !em.HasComponent<Deleted>(e))
                    {
                        ActorWriter.ClearLanePresence(em, e);
                        em.AddComponent<Deleted>(e);
                    }
                });
            }
            v.Entity = Entity.Null;
        }

        // =====================================================================================================
        // Spawn points, entities
        // =====================================================================================================

        private bool FindSpawnPoint(float3 near, out float3 point, out float yaw)
        {
            yaw = 0f;
            float terrainY = m_Probe.TerrainHeight(near.x, near.z);
            var probePos = new float3(near.x, terrainY, near.z);
            foreach (float r in new[] { 40f, 120f, 300f })
            {
                if (m_Probe.FindNearestLanePoint(probePos, r, pedestrian: true, out point, out float3 tangent, out _))
                {
                    yaw = math.atan2(tangent.x, tangent.z);
                    return true;
                }
            }
            if (m_Probe.FindNearestLanePoint(probePos, 300f, pedestrian: false, out float3 carPoint, out float3 t2, out _))
            {
                float3 right = math.normalize(new float3(t2.z, 0f, -t2.x));
                point = carPoint + right * 5f;
                yaw = math.atan2(t2.x, t2.z);
                return true;
            }
            point = probePos;
            return m_Probe.IsInsideMap(near.x, near.z);
        }

        private void SpawnAvatarEntity()
        {
            if (m_AvatarSpawnPending || m_AvatarEntity != Entity.Null)
            {
                return;
            }
            Entity prefab = m_Catalog.PickHuman(m_AvatarSeed);
            float3 pos = m_Avatar.Position.ToF3();
            quaternion rot = quaternion.RotateY(m_Avatar.Yaw);
            m_AvatarSpawnPending = true;
            ActorOpsSystem.Enqueue(em =>
            {
                m_AvatarEntity = ActorWriter.SpawnHuman(em, prefab, pos, rot, 0, (ushort)m_AvatarSeed);
                m_AvatarSpawnPending = false;
                m_AvatarVisible = true;
                m_Probe.IgnoreEntities.Add(m_AvatarEntity);
            });
        }

        private void DespawnAvatarEntity()
        {
            Entity e = m_AvatarEntity;
            m_AvatarEntity = Entity.Null;
            m_AvatarVisible = false;
            if (e == Entity.Null)
            {
                return;
            }
            m_Probe.IgnoreEntities.Remove(e);
            ActorOpsSystem.Enqueue(em =>
            {
                if (em.Exists(e) && !em.HasComponent<Deleted>(e))
                {
                    em.AddComponent<Deleted>(e);
                }
            });
        }

        public bool SpawnCareerVehicle(string prefabOrCategory, float3 pos, float yaw, string careerVehicleId, out ControlledVehicle vehicle)
        {
            vehicle = null;
            CarPrefabInfo info = m_Catalog.FindCar(prefabOrCategory, UnityEngine.Random.Range(0, 1000));
            if (info == null)
            {
                Say(Loc.T("msg.noModel", prefabOrCategory));
                return false;
            }
            return SpawnCareerVehicle(info, pos, yaw, careerVehicleId, out vehicle);
        }

        /// <summary>
        /// Places a career vehicle of a category near a point where its whole footprint is clear (road lane; parking lane
        /// for small vehicles), then spawns it.
        /// </summary>
        public bool SpawnCareerVehicleNear(string prefabOrCategory, float3 near, string careerVehicleId, out ControlledVehicle vehicle)
        {
            vehicle = null;
            CarPrefabInfo info = m_Catalog.FindCar(prefabOrCategory, UnityEngine.Random.Range(0, 1000));
            if (info == null)
            {
                Say(Loc.T("msg.noModel", prefabOrCategory));
                return false;
            }
            VehicleSpec spec = VehicleSpecs.FromPrefab(info);
            if (!FindClearVehicleSpot(spec, near, spec.Length <= 6.5f, 160f, out float3 at, out float yaw))
            {
                Say(Loc.T("career.noRoadNearby"));
                return false;
            }
            return SpawnCareerVehicle(info, at, yaw, careerVehicleId, out vehicle);
        }

        private bool SpawnCareerVehicle(CarPrefabInfo info, float3 pos, float yaw, string careerVehicleId, out ControlledVehicle vehicle)
        {
            var v = new ControlledVehicle
            {
                Kind = ActorKind.CareerVehicle,
                RecordId = m_NextRecordId++,
                Prefab = info,
                Spec = VehicleSpecs.FromPrefab(info),
                CareerVehicleId = careerVehicleId,
            };
            v.State = new VehicleState { Position = new Vec3(pos.x, pos.y, pos.z), Yaw = yaw, Grounded = true, ParkingBrake = true };
            SpawnVehicleEntity(v);
            m_ParkedVehicles.Add(v);
            vehicle = v;
            return true;
        }

        private void SpawnVehicleEntity(ControlledVehicle v)
        {
            if (v.SpawnPending || v.Kind != ActorKind.CareerVehicle)
            {
                return;
            }
            v.SpawnPending = true;
            float3 pos = v.State.Position.ToF3();
            quaternion rot = VehicleRotation(v.State);
            Entity prefab = v.Prefab.Prefab;
            ActorOpsSystem.Enqueue(em =>
            {
                v.Entity = ActorWriter.SpawnCar(em, prefab, pos, rot, v.RecordId, (ushort)(v.RecordId * 7919));
                v.SpawnPending = false;
            });
        }

        private void HandleRespawns()
        {
            if (!m_RespawnAfterSave)
            {
                return;
            }
            m_RespawnAfterSave = false;
            if (m_AvatarVisible || m_Sm.State == PlayerState.OnFoot || m_Sm.State == PlayerState.ExitingVehicle || m_Sm.State == PlayerState.EnteringVehicle)
            {
                m_AvatarVisible = false;
                SpawnAvatarEntity();
            }
            if (m_Vehicle != null && m_Vehicle.Kind == ActorKind.CareerVehicle && m_Vehicle.Entity == Entity.Null)
            {
                SpawnVehicleEntity(m_Vehicle);
            }
            foreach (ControlledVehicle p in m_ParkedVehicles)
            {
                if (p.Kind == ActorKind.CareerVehicle && p.Entity == Entity.Null)
                {
                    SpawnVehicleEntity(p);
                }
            }
            DtcLog.Info("Player: actors respawned after a save");
        }

        /// <summary>Detects entities removed by the game (EntityInvalidated) and reacts per SPEC-CONTROL §10.</summary>
        private void ValidateEntities()
        {
            EntityManager em = EntityManager;
            if (m_AvatarEntity != Entity.Null && (!em.Exists(m_AvatarEntity) || em.HasComponent<Deleted>(m_AvatarEntity)))
            {
                DtcLog.Warn("Avatar entity vanished; respawning");
                m_AvatarEntity = Entity.Null;
                m_AvatarVisible = false;
                if (m_Sm.State == PlayerState.OnFoot)
                {
                    SpawnAvatarEntity();
                }
            }
            if (m_Vehicle != null && m_Vehicle.Entity != Entity.Null && !m_Vehicle.SpawnPending &&
                (!em.Exists(m_Vehicle.Entity) || em.HasComponent<Deleted>(m_Vehicle.Entity)))
            {
                m_Vehicle.Entity = Entity.Null;
                if (m_Vehicle.Kind == ActorKind.CareerVehicle)
                {
                    DtcLog.Warn("Controlled career vehicle removed by the game; respawning from its record");
                    SpawnVehicleEntity(m_Vehicle);
                }
                else
                {
                    m_Vehicle.Lost = true;
                    DtcLog.Warn("Borrowed vehicle removed by the game; entering Recovery");
                    if (m_Sm.State == PlayerState.Driving || m_Sm.State == PlayerState.EnteringVehicle || m_Sm.State == PlayerState.ExitingVehicle)
                    {
                        PlaceAvatarAfterVehicleLoss();
                    }
                }
            }
        }

        private void PlaceAvatarAfterVehicleLoss()
        {
            float3 at = m_Vehicle != null ? m_Vehicle.State.Position.ToF3() : m_Avatar.Position.ToF3();
            m_Vehicle = null;
            m_Avatar.Position = new Vec3(at.x, at.y, at.z);
            m_Sm.Request(PlayerState.Recovery, "vehicle invalidated");
            m_RecoverySearches = 0;
            Say(Loc.T("msg.vehicleGone"));
        }

        // =====================================================================================================
        // OnFoot
        // =====================================================================================================

        private void UpdateOnFoot(InputFrame input, float dt)
        {
            var ai = new AvatarInput
            {
                Move = new Vec2(input.MoveX, input.MoveZ),
                Run = input.Run,
                CameraYaw = m_CamYaw,
            };
            if (dt > 0f)
            {
                AvatarStepReport r = m_Motor.Step(m_Probe, ref m_Avatar, ai, dt);
                if (r.LastGroundStatus == GroundStatus.Invalid)
                {
                    EnterRecovery("invalid ground");
                    return;
                }
                if (m_Avatar.AirTime > 6f)
                {
                    EnterRecovery("falling too long");
                    return;
                }
                CheckStuck(new Vec2(input.MoveX, input.MoveZ).Length > 0.2f, dt);
            }
            if (input.InteractPressed)
            {
                TryInteract();
            }
            if (input.RecoverPressed)
            {
                EnterRecovery("player request");
            }
        }

        private void CheckStuck(bool wantsToMove, float dt)
        {
            float3 p = m_Sm.State == PlayerState.Driving && m_Vehicle != null ? m_Vehicle.State.Position.ToF3() : m_Avatar.Position.ToF3();
            if (!wantsToMove || math.distance(p, m_LastProgressPos) > 0.5f)
            {
                m_StuckTimer = 0f;
                m_LastProgressPos = p;
                return;
            }
            m_StuckTimer += dt;
            if (m_StuckTimer > 6f)
            {
                m_StuckTimer = 0f;
                Say(Loc.T("msg.stuck"));
            }
        }

        /// <summary>TR-03: enter the nearest enterable vehicle (career vehicle, or a native car in sandbox mode).</summary>
        public void TryInteract()
        {
            if (m_Sm.State != PlayerState.OnFoot)
            {
                return;
            }
            float3 me = m_Avatar.Position.ToF3();
            ControlledVehicle best = null;
            float bestD = 4.0f;
            foreach (ControlledVehicle p in m_ParkedVehicles)
            {
                if (p.Entity == Entity.Null)
                {
                    continue;
                }
                float d = DistanceToVehicle(me, p.State, p.Spec);
                if (d < bestD)
                {
                    bestD = d;
                    best = p;
                }
            }
            if (best == null && Sandbox)
            {
                best = FindBorrowableNative(me, ref bestD);
            }
            if (best == null)
            {
                Say(Loc.T(Sandbox ? "msg.noVehicleSandbox" : "msg.noVehicleCareer"));
                return;
            }
            if (!VehicleController.IsStationary(best.State, best.Spec))
            {
                Say(Loc.T("msg.vehicleMoving"));
                return;
            }
            BeginEntering(best);
        }

        private static float DistanceToVehicle(float3 p, VehicleState s, VehicleSpec spec)
        {
            var box = Obb2.FromYaw(s.Position.XZ, s.Yaw, spec.Width * 0.5f, spec.Length * 0.5f);
            return Vec2.Distance(box.ClosestPoint(new Vec2(p.x, p.z)), new Vec2(p.x, p.z));
        }

        private ControlledVehicle FindBorrowableNative(float3 me, ref float bestD)
        {
            var obstacles = new List<Obstacle>();
            bool prev = m_Probe.IncludeCreatures;
            m_Probe.IncludeCreatures = false;
            m_Probe.QueryObstacles(new Vec2(me.x, me.z), 8f, obstacles);
            m_Probe.IncludeCreatures = prev;
            Entity bestE = Entity.Null;
            foreach (Obstacle o in obstacles)
            {
                if (o.Kind != ObstacleKind.Vehicle)
                {
                    continue;
                }
                float d = Vec2.Distance(o.Footprint.ClosestPoint(new Vec2(me.x, me.z)), new Vec2(me.x, me.z));
                if (d >= bestD || o.Velocity.Length > 0.5f)
                {
                    continue;
                }
                var e = new Entity { Index = (int)(o.SourceId & 0xffffffff), Version = (int)(o.SourceId >> 32) };
                if (!IsBorrowable(e))
                {
                    continue;
                }
                bestD = d;
                bestE = e;
            }
            if (bestE == Entity.Null)
            {
                return null;
            }
            EntityManager em = EntityManager;
            Transform t = em.GetComponentData<Transform>(bestE);
            PrefabRef pr = em.GetComponentData<PrefabRef>(bestE);
            ObjectGeometryData g = em.GetComponentData<ObjectGeometryData>(pr.m_Prefab);
            CarData cd = em.HasComponent<CarData>(pr.m_Prefab) ? em.GetComponentData<CarData>(pr.m_Prefab) : default;
            var info = new CarPrefabInfo { Prefab = pr.m_Prefab, Name = "native", Category = "car", Size = g.m_Size, MaxSpeed = cd.m_MaxSpeed, Acceleration = cd.m_Acceleration, Braking = cd.m_Braking };
            float3 fwd = math.mul(t.m_Rotation, new float3(0, 0, 1));
            return new ControlledVehicle
            {
                Entity = bestE,
                Kind = ActorKind.BorrowedNativeVehicle,
                RecordId = m_NextRecordId++,
                Prefab = info,
                Spec = VehicleSpecs.FromPrefab(info),
                State = new VehicleState { Position = new Vec3(t.m_Position.x, t.m_Position.y, t.m_Position.z), Yaw = math.atan2(fwd.x, fwd.z), Grounded = true },
            };
        }

        /// <summary>Sandbox borrowable classes: private cars and light delivery vehicles only (SPEC-CONTROL §11).</summary>
        private bool IsBorrowable(Entity e)
        {
            EntityManager em = EntityManager;
            if (!em.Exists(e) || em.HasComponent<DtcActor>(e) || em.HasComponent<Game.Objects.Unspawned>(e))
            {
                return false;
            }
            if (!em.HasComponent<Game.Vehicles.Car>(e) || !em.HasComponent<Moving>(e) || !em.HasBuffer<TransformFrame>(e))
            {
                return false;
            }
            if (em.HasComponent<Game.Vehicles.PublicTransport>(e) || em.HasComponent<Game.Vehicles.Ambulance>(e) ||
                em.HasComponent<Game.Vehicles.FireEngine>(e) || em.HasComponent<Game.Vehicles.PoliceCar>(e) ||
                em.HasComponent<Game.Vehicles.Hearse>(e) || em.HasComponent<Game.Vehicles.GarbageTruck>(e) ||
                em.HasComponent<Game.Vehicles.MaintenanceVehicle>(e) || em.HasComponent<Game.Vehicles.PostVan>(e) ||
                em.HasComponent<Game.Vehicles.Taxi>(e) || em.HasComponent<Game.Vehicles.CarTrailer>(e))
            {
                return false;
            }
            if (em.HasBuffer<Game.Vehicles.LayoutElement>(e) && em.GetBuffer<Game.Vehicles.LayoutElement>(e).Length > 1)
            {
                return false;
            }
            return em.HasComponent<Game.Vehicles.PersonalCar>(e) || em.HasComponent<Game.Vehicles.DeliveryTruck>(e);
        }

        // =====================================================================================================
        // Entering / Exiting
        // =====================================================================================================

        private void BeginEntering(ControlledVehicle v)
        {
            if (m_Sm.Request(PlayerState.EnteringVehicle, "interact") != TransitionResultCode.Ok)
            {
                Say(Loc.T("msg.cannotEnterVehicle"));
                return;
            }
            m_PreEntryPos = m_Avatar.Position.ToF3();
            if (v.Kind == ActorKind.BorrowedNativeVehicle)
            {
                Entity e = v.Entity;
                int rid = v.RecordId;
                ActorOpsSystem.Enqueue(em => ActorWriter.BorrowNative(em, e, rid));
                DtcLog.Info($"Sandbox: borrowing native vehicle {e}");
            }
            else
            {
                m_ParkedVehicles.Remove(v);
            }
            m_Vehicle = v;
            m_Vehicle.State.ParkingBrake = true;
            m_Probe.IgnoreEntities.Add(v.Entity);
            m_VehCtrl.SetSpec(v.Spec);
            m_TransStart = m_Avatar.Position.ToF3();
            m_TransEnd = DoorPoint(v, left: true);
            m_TransDuration = math.clamp(math.distance(m_TransStart, m_TransEnd) / 1.6f, 0.35f, 2.5f);
            m_TransTime = 0f;
            Say(Loc.T("msg.entering"));
        }

        private float3 DoorPoint(ControlledVehicle v, bool left)
        {
            Vec2 fwd = Vec2.FromYaw(v.State.Yaw);
            Vec2 right = fwd.RightPerp;
            float side = (v.Spec.Width * 0.5f + 0.45f) * (left ? -1f : 1f);
            Vec2 p = v.State.Position.XZ + right * side + fwd * (v.Spec.Length * 0.12f);
            return new float3(p.X, v.State.Position.Y, p.Z);
        }

        private void UpdateEntering(float dt)
        {
            if (m_Vehicle == null || m_Vehicle.Lost)
            {
                AbortEntering("vehicle invalidated");
                return;
            }
            m_TransTime += dt;
            float k = math.saturate(m_TransTime / m_TransDuration);
            float3 p = math.lerp(m_TransStart, m_TransEnd, k);
            float3 dir = m_TransEnd - m_TransStart;
            if (math.lengthsq(dir.xz) > 0.01f)
            {
                m_Avatar.Yaw = math.atan2(dir.x, dir.z);
            }
            m_Avatar.Position = new Vec3(p.x, p.y, p.z);
            m_Avatar.Velocity = k < 1f ? new Vec2(dir.x, dir.z) / math.max(0.1f, m_TransDuration) : Vec2.Zero;
            if (k >= 1f && m_Vehicle.Entity != Entity.Null && !m_Vehicle.SpawnPending)
            {
                // TR-04: seated. The avatar is attached (hidden) and the driving controller gets authority.
                if (m_Sm.Request(PlayerState.Driving, "seated") == TransitionResultCode.Ok)
                {
                    DespawnAvatarEntity();
                    m_Vehicle.State.ParkingBrake = false;
                    m_Vehicle.State.Speed = 0f;
                    m_ChaseYawOffset = 0f;
                    m_CamYaw = m_Vehicle.State.Yaw;
                    m_StuckTimer = 0f;
                    Say(Loc.T(m_Vehicle.Kind == ActorKind.BorrowedNativeVehicle ? "msg.drivingSandbox" : "msg.driving"));
                }
            }
        }

        private void AbortEntering(string reason)
        {
            // TR-05: back to the pre-entry position; a borrowed car is handed back immediately.
            if (m_Vehicle != null)
            {
                m_Probe.IgnoreEntities.Remove(m_Vehicle.Entity);
                if (m_Vehicle.Kind == ActorKind.BorrowedNativeVehicle)
                {
                    ReleaseVehicle(m_Vehicle, 0f);
                }
                else if (!m_Vehicle.Lost)
                {
                    m_ParkedVehicles.Add(m_Vehicle);
                }
                m_Vehicle = null;
            }
            m_Avatar.Position = new Vec3(m_PreEntryPos.x, m_PreEntryPos.y, m_PreEntryPos.z);
            if (m_Sm.Request(PlayerState.OnFoot, reason) != TransitionResultCode.Ok)
            {
                EnterRecovery(reason);
            }
            else if (m_AvatarEntity == Entity.Null)
            {
                SpawnAvatarEntity();
            }
        }

        /// <summary>TR-06: exit at a validated point, preferring the driver door, then alternates.</summary>
        public void TryExit()
        {
            if (m_Sm.State != PlayerState.Driving || m_Vehicle == null)
            {
                return;
            }
            if (!VehicleController.IsStationary(m_Vehicle.State, m_Vehicle.Spec))
            {
                Say(Loc.T("msg.stopToExit"));
                return;
            }
            if (!FindExitPoint(m_Vehicle, out float3 exit))
            {
                Say(Loc.T("msg.exitBlocked"));
                return;
            }
            if (m_Sm.Request(PlayerState.ExitingVehicle, "exit") != TransitionResultCode.Ok)
            {
                return;
            }
            m_Vehicle.State.ParkingBrake = true;
            m_Vehicle.State.Speed = 0f;
            m_TransStart = DoorPoint(m_Vehicle, left: true);
            m_TransEnd = exit;
            m_TransDuration = math.clamp(math.distance(m_TransStart, m_TransEnd) / 1.4f, 0.4f, 2.0f);
            m_TransTime = 0f;
            m_Avatar.Position = new Vec3(m_TransStart.x, m_TransStart.y, m_TransStart.z);
            m_Avatar.Yaw = m_Vehicle.State.Yaw - MathX.Pi / 2f;
            m_Avatar.Grounded = true;
            SpawnAvatarEntity();
        }

        /// <summary>Exit candidates: driver side, passenger side, rear, front. Each must be supported ground at a
        /// sensible level and free of obstacles (TEST-TRANS-002: never inside a building or under the road).</summary>
        public bool FindExitPoint(ControlledVehicle v, out float3 point)
        {
            Vec2 fwd = Vec2.FromYaw(v.State.Yaw);
            Vec2 right = fwd.RightPerp;
            Vec2 c = v.State.Position.XZ;
            float hw = v.Spec.Width * 0.5f + 0.7f;
            float hl = v.Spec.Length * 0.5f + 0.8f;
            // Curb side first (right-hand traffic: the right of the direction of travel), then the other side, rear, front,
            // then farther out on each side so that a sidewalk across a parking lane is still found.
            Vec2[] candidates =
            {
                c + right * hw,
                c - right * hw,
                c - fwd * hl,
                c + fwd * hl,
                c + right * (hw + 1.5f),
                c + right * (hw + 3.0f),
                c - right * (hw + 1.5f),
                c - right * (hw + 3.0f),
            };
            var gs = new GroundSolver();
            bool found = false;
            point = default;
            foreach (Vec2 cand in candidates)
            {
                GroundResult g = gs.Solve(m_Probe, cand.X, cand.Z, v.State.Position.Y + 0.3f, 0.6f, 1.2f, 0.6f);
                if (g.Status != GroundStatus.Supported)
                {
                    continue;
                }
                if (math.abs(g.Height - v.State.Position.Y) > 1.0f)
                {
                    continue;
                }
                if (m_Motor.IsPenetrating(m_Probe, cand, g.Height))
                {
                    continue;
                }
                var p = new float3(cand.X, g.Height, cand.Z);
                if (g.Kind == SurfaceKind.Pedestrian)
                {
                    point = p; // curb side first: never step into a traffic lane when a sidewalk is reachable
                    return true;
                }
                if (!found)
                {
                    point = p;
                    found = true;
                }
            }
            // No sidewalk right beside the car (stopped mid-lane on a wide road): prefer the nearest sidewalk within 8 m
            // over the carriageway, if it is clear and at a similar height (ISSUE-010).
            float3 car = v.State.Position.ToF3();
            if (m_Probe.FindNearestLanePoint(car, 8f, CityWorldProbe.LaneKind.Sidewalk, out float3 walk, out _, out _) &&
                math.abs(walk.y - car.y) < 1.0f && !m_Motor.IsPenetrating(m_Probe, new Vec2(walk.x, walk.z), walk.y))
            {
                point = walk;
                return true;
            }
            return found;
        }

        private void UpdateExiting(float dt)
        {
            if (m_Vehicle == null)
            {
                m_Sm.Request(PlayerState.Recovery, "vehicle lost while exiting");
                return;
            }
            m_TransTime += dt;
            float k = math.saturate(m_TransTime / m_TransDuration);
            float3 p = math.lerp(m_TransStart, m_TransEnd, k);
            float3 dir = m_TransEnd - m_TransStart;
            if (math.lengthsq(dir.xz) > 0.01f)
            {
                m_Avatar.Yaw = math.atan2(dir.x, dir.z);
            }
            m_Avatar.Position = new Vec3(p.x, p.y, p.z);
            m_Avatar.Velocity = k < 1f ? new Vec2(dir.x, dir.z) / math.max(0.1f, m_TransDuration) : Vec2.Zero;
            if (k < 1f)
            {
                return;
            }
            // TR-07: re-validate the exit point at commit.
            if (m_Motor.IsPenetrating(m_Probe, new Vec2(p.x, p.z), p.y) || !m_Motor.TryPlace(m_Probe, ref m_Avatar, new Vec2(p.x, p.z), p.y + 0.5f))
            {
                if (FindExitPoint(m_Vehicle, out float3 alt))
                {
                    m_Avatar.Position = new Vec3(alt.x, alt.y, alt.z);
                }
                else
                {
                    EnterRecovery("exit point lost");
                    return;
                }
            }
            m_Avatar.Yaw = math.atan2(dir.x, dir.z);
            if (m_Sm.Request(PlayerState.OnFoot, "placed") == TransitionResultCode.Ok)
            {
                ControlledVehicle v = m_Vehicle;
                m_Vehicle = null;
                m_Probe.IgnoreEntities.Remove(v.Entity);
                if (v.Kind == ActorKind.BorrowedNativeVehicle)
                {
                    ReleaseVehicle(v, 0f);
                    Say(Loc.T("msg.leftBorrowed"));
                }
                else
                {
                    v.State.ParkingBrake = true;
                    m_ParkedVehicles.Add(v);
                    Say(Loc.T("msg.parked"));
                }
                m_CamYaw = m_Avatar.Yaw;
            }
        }

        private void OnTransitionTimeout()
        {
            switch (m_Sm.State)
            {
                case PlayerState.EnteringVehicle:
                    AbortEntering("timeout");
                    break;
                case PlayerState.ExitingVehicle:
                    // TR-08 abort back to Driving if the vehicle is still valid.
                    if (m_Vehicle != null && m_Vehicle.Entity != Entity.Null)
                    {
                        DespawnAvatarEntity();
                        m_Sm.Request(PlayerState.Driving, "exit timeout");
                    }
                    else
                    {
                        EnterRecovery("exit timeout");
                    }
                    break;
                case PlayerState.Recovery:
                    ForceBuilding("recovery timeout");
                    Say(Loc.T("msg.recoverFailed"));
                    break;
            }
        }

        // =====================================================================================================
        // Driving
        // =====================================================================================================

        private void UpdateDriving(InputFrame input, float dt)
        {
            if (m_Vehicle == null)
            {
                EnterRecovery("no vehicle");
                return;
            }
            if (m_Vehicle.Entity == Entity.Null || m_Vehicle.SpawnPending)
            {
                return; // respawning after a save; hold still for a frame
            }
            var vi = new VehicleInput
            {
                Throttle = input.Throttle,
                Brake = input.Brake,
                Steer = input.Steer,
                Handbrake = input.Handbrake,
            };
            if (ThrottleBlocked)
            {
                // Immobilized (out of fuel or broken down, decided by Career): no drive in either direction, but brakes
                // and steering always work so the player is never left uncontrolled.
                vi.Throttle = 0f;
                if (VehicleController.IsStationary(m_Vehicle.State, m_Vehicle.Spec) || m_Vehicle.State.Gear == Gear.Reverse)
                {
                    vi.Brake = 0f;
                    vi.Handbrake = true;
                }
            }
            if (dt > 0f)
            {
                m_VehCtrl.SetSpec(m_Vehicle.Spec);
                // Which level the vehicle is on where road surfaces overlap: the lane it is following (ISSUE-029).
                float2 hd = new float2(math.sin(m_Vehicle.State.Yaw), math.cos(m_Vehicle.State.Yaw));
                m_VehCtrl.LevelHint = m_Probe.TryLaneLevel(m_Vehicle.State.Position.ToF3(), hd, 3f, out float laneY) ? laneY : float.NaN;
                VehicleStepReport r = m_VehCtrl.Step(m_Probe, ref m_Vehicle.State, vi, dt);
                LastImpact = r.ImpactSpeed > 2f ? r : LastImpact;
                if (r.ImpactSpeed > 2f)
                {
                    DtcLog.Throttled("impact", UTime.realtimeSinceStartup, 1f, $"Impact {r.ImpactSpeed:0.0} m/s with {r.ImpactKind}");
                    VehicleImpact?.Invoke(r);
                }
                if (r.WorstGround == GroundStatus.Invalid)
                {
                    EnterRecovery("vehicle on invalid ground");
                    return;
                }
                CheckStuck(input.Throttle > 0.2f || input.Brake > 0.2f, dt);
                DriveDistance?.Invoke(r.Distance);
            }
            // Mouse look offsets the chase camera; it eases back to the heading.
            m_ChaseYawOffset = MathX.WrapAngle(m_ChaseYawOffset + input.LookX * MathX.Deg2Rad);
            if (math.abs(input.LookX) < 0.01f)
            {
                m_ChaseYawOffset *= 1f - MathX.SmoothFactor(1.5f, dt > 0f ? dt : UTime.unscaledDeltaTime);
            }
            m_Vehicle.State.Speed = m_Vehicle.State.Speed; // keep
            if (input.InteractPressed)
            {
                TryExit();
            }
            if (input.RecoverPressed)
            {
                EnterRecovery("player request");
            }
            LastInput = vi;
        }

        public VehicleInput LastInput { get; private set; }
        public VehicleStepReport LastImpact { get; private set; }
        public event Action<VehicleStepReport> VehicleImpact;
        public event Action<float> DriveDistance;

        // =====================================================================================================
        // Recovery
        // =====================================================================================================

        public void EnterRecovery(string reason)
        {
            if (m_Sm.State == PlayerState.Building || m_Sm.State == PlayerState.Recovery)
            {
                return;
            }
            if (m_Sm.RecoveryLoopDetected)
            {
                ForceBuilding("recovery loop");
                Say(Loc.T("msg.recoveryLoop"));
                return;
            }
            if (m_Sm.Request(PlayerState.Recovery, reason) == TransitionResultCode.Ok)
            {
                m_RecoverySearches = 0;
                Say(Loc.T("msg.recovering", reason));
            }
        }

        private void UpdateRecovery()
        {
            bool inVehicle = m_Vehicle != null && m_Vehicle.Entity != Entity.Null && !m_Vehicle.Lost;
            float3 from = inVehicle ? m_Vehicle.State.Position.ToF3() : m_Avatar.Position.ToF3();
            m_RecoverySearches++;
            float radius = 30f * m_RecoverySearches;
            if (inVehicle)
            {
                // A spot where the whole vehicle fits (a bus moved to the nearest lane point can still overlap a pole).
                if (FindClearVehicleSpot(m_Vehicle.Spec, from, false, radius, out float3 p, out float placedYaw, m_Vehicle.State.Yaw))
                {
                    m_Vehicle.State.Position = new Vec3(p.x, p.y, p.z);
                    m_Vehicle.State.Yaw = placedYaw;
                    m_Vehicle.State.Speed = 0f;
                    m_Vehicle.State.VerticalSpeed = 0f;
                    m_Vehicle.State.Grounded = true;
                    m_Vehicle.State.ParkingBrake = false;
                    m_Sm.Request(PlayerState.Driving, "vehicle placed");
                    Say(Loc.T("msg.vehicleToRoad"));
                    return;
                }
            }
            else
            {
                if (m_Probe.FindNearestLanePoint(from, radius, pedestrian: true, out float3 p, out float3 tan, out _) &&
                    m_Motor.TryPlace(m_Probe, ref m_Avatar, new Vec2(p.x, p.z), p.y + 1f))
                {
                    m_Avatar.Yaw = math.atan2(tan.x, tan.z);
                    m_Sm.Request(PlayerState.OnFoot, "avatar placed");
                    if (m_AvatarEntity == Entity.Null)
                    {
                        SpawnAvatarEntity();
                    }
                    Say(Loc.T("msg.toSidewalk"));
                    return;
                }
            }
            if (m_RecoverySearches >= 3)
            {
                // TR-12: no safe point.
                m_Sm.Request(PlayerState.Building, "no safe point");
                RunTeardown();
                Say(Loc.T("msg.noSafePoint"));
            }
        }

        // =====================================================================================================
        // Pose writes and camera
        // =====================================================================================================

        private void WritePoses(bool frozen)
        {
            EntityManager em = EntityManager;
            bool avatarShown = m_Sm.State == PlayerState.OnFoot || m_Sm.State == PlayerState.EnteringVehicle || m_Sm.State == PlayerState.ExitingVehicle || m_Sm.State == PlayerState.Recovery;
            if (avatarShown && m_AvatarEntity != Entity.Null && em.Exists(m_AvatarEntity))
            {
                float3 pos = m_Avatar.Position.ToF3();
                quaternion rot = quaternion.RotateY(m_Avatar.Yaw);
                float speed = m_Avatar.Velocity.Length;
                bool moving = !frozen && speed > 0.15f;
                float3 vel = new float3(m_Avatar.Velocity.X, 0f, m_Avatar.Velocity.Z);
                ActorWriter.WritePose(em, m_AvatarEntity, pos, rot, vel, 0, moving ? TransformState.Move : TransformState.Idle);
                ActorWriter.PinHumanNavigation(em, m_AvatarEntity, pos, math.normalizesafe(new float2(vel.x, vel.z)), speed > 2.5f);
            }
            if (m_Vehicle != null)
            {
                WriteVehicle(em, m_Vehicle, active: m_Sm.State == PlayerState.Driving, frozen);
            }
            foreach (ControlledVehicle p in m_ParkedVehicles)
            {
                WriteVehicle(em, p, active: false, frozen);
            }
        }

        private void WriteVehicle(EntityManager em, ControlledVehicle v, bool active, bool frozen)
        {
            if (v.Entity == Entity.Null || v.SpawnPending || !em.Exists(v.Entity))
            {
                return;
            }
            VehicleState s = v.State;
            TransformFlags flags = 0;
            if (active || v.Kind == ActorKind.BorrowedNativeVehicle)
            {
                flags |= TransformFlags.RearLights;
                if (v.LightsOn && IsDark())
                {
                    flags |= TransformFlags.MainLights;
                }
                if (active && (LastInput.Brake > 0.1f && s.Speed > 0.3f || LastInput.Throttle > 0.1f && s.Speed < -0.3f || LastInput.Handbrake))
                {
                    flags |= TransformFlags.Braking;
                }
                if (s.Gear == Gear.Reverse && s.Speed < -0.1f)
                {
                    flags |= TransformFlags.Reversing;
                }
            }
            float3 vel = new float3(math.sin(s.Yaw), 0f, math.cos(s.Yaw)) * (frozen ? 0f : s.Speed);
            ActorWriter.WritePose(em, v.Entity, s.Position.ToF3(), VehicleRotation(s), vel, flags, TransformState.Default);
            // Traffic presence: native cars see the player's vehicle only through lane object buffers.
            float2 heading = new float2(math.sin(s.Yaw), math.cos(s.Yaw));
            if (m_Probe.FindCarLaneUnder(s.Position.ToF3(), heading, out Entity lane, out float laneT))
            {
                ActorWriter.UpdateLanePresence(em, v.Entity, lane, laneT);
            }
            else
            {
                ActorWriter.UpdateLanePresence(em, v.Entity, Entity.Null, 0f);
            }
            if (UTime.frameCount % 120 == 0)
            {
                ActorWriter.RefreshTripSource(em, v.Entity);
            }
        }

        private Game.Rendering.LightingSystem m_Lighting;

        private bool IsDark()
        {
            m_Lighting ??= World.GetExistingSystemManaged<Game.Rendering.LightingSystem>();
            return m_Lighting != null && m_Lighting.dayLightBrightness < 0.5f;
        }

        public static quaternion VehicleRotation(VehicleState s) =>
            quaternion.Euler(new float3(-s.Pitch, s.Yaw, -s.Roll), math.RotationOrder.ZXY);

        private void ToggleView()
        {
            if (m_Sm.State == PlayerState.Driving)
            {
                m_DriveView = m_DriveView == CameraView.Chase ? CameraView.Hood : CameraView.Chase;
            }
            else
            {
                m_FootView = m_FootView == CameraView.ThirdPerson ? CameraView.FirstPerson : CameraView.ThirdPerson;
            }
        }

        private void UpdateCamera(float dt)
        {
            float rdt = math.max(1e-3f, UTime.unscaledDeltaTime);
            float3 target;
            float yawDeg;
            float pitchDeg;
            float fov = 60f;
            bool driving = m_Sm.State == PlayerState.Driving && m_Vehicle != null;
            if (driving)
            {
                VehicleState s = m_Vehicle.State;
                float camYaw = s.Yaw + m_ChaseYawOffset;
                float3 fwd = new float3(math.sin(camYaw), 0f, math.cos(camYaw));
                float3 basePos = s.Position.ToF3();
                if (m_DriveView == CameraView.Hood)
                {
                    target = basePos + new float3(math.sin(s.Yaw), 0f, math.cos(s.Yaw)) * (m_Vehicle.Spec.Length * 0.15f) + new float3(0f, m_Vehicle.Spec.Height + 0.25f, 0f);
                    yawDeg = camYaw * MathX.Rad2Deg;
                    pitchDeg = -m_CamPitch * 0.5f + 2f;
                    fov = 70f;
                }
                else
                {
                    float dist = m_Vehicle.Spec.Length * 1.1f + 4.5f;
                    float height = m_Vehicle.Spec.Height + 1.6f;
                    target = basePos - fwd * dist + new float3(0f, height, 0f);
                    target = ClampCameraAboveSurface(target, basePos.y + 0.8f);
                    yawDeg = camYaw * MathX.Rad2Deg;
                    pitchDeg = 8f;
                    fov = m_FovBase + math.min(10f, math.abs(s.Speed) * 0.25f);
                }
            }
            else
            {
                float3 head = m_Avatar.Position.ToF3() + new float3(0f, 1.62f, 0f);
                float3 fwd = new float3(math.sin(m_CamYaw), 0f, math.cos(m_CamYaw));
                if (m_FootView == CameraView.FirstPerson)
                {
                    target = head + fwd * 0.12f;
                }
                else
                {
                    float pitchRad = math.radians(m_CamPitch);
                    float dist = 3.6f;
                    float3 back = -fwd * math.cos(pitchRad) * dist + new float3(0f, -math.sin(pitchRad) * dist + 0.35f, 0f);
                    target = head + back;
                    target = PullCameraIn(head, target);
                    target = ClampCameraAboveSurface(target, m_Avatar.Position.Y + 0.3f);
                }
                yawDeg = m_CamYaw * MathX.Rad2Deg;
                pitchDeg = -m_CamPitch;
                fov = m_FootView == CameraView.FirstPerson ? m_FovBase + 8f : m_FovBase;
            }

            if (!m_CamInitialized)
            {
                m_SmoothedCamPos = target;
                m_CamInitialized = true;
            }
            float sharp = driving && m_DriveView == CameraView.Chase ? 10f : 30f;
            m_SmoothedCamPos = math.lerp(m_SmoothedCamPos, target, MathX.SmoothFactor(sharp, rdt));
            m_Camera.TargetPosition = new Vector3(m_SmoothedCamPos.x, m_SmoothedCamPos.y, m_SmoothedCamPos.z);
            m_Camera.TargetEuler = new Vector3(pitchDeg, yawDeg, 0f);
            m_Camera.TargetFov = fov;
        }

        private float3 ClampCameraAboveSurface(float3 cam, float minY)
        {
            var samples = new List<SurfaceSample>(8);
            m_Probe.SampleSurfaces(cam.x, cam.z, minY - 5f, cam.y + 1f, samples);
            float best = float.MinValue;
            foreach (SurfaceSample s in samples)
            {
                if (s.Height <= cam.y + 1f && s.Height > best && s.Kind != SurfaceKind.Water)
                {
                    best = s.Height;
                }
            }
            if (best > float.MinValue && cam.y < best + 0.4f)
            {
                cam.y = best + 0.4f;
            }
            return cam;
        }

        /// <summary>Moves the third-person camera towards the head if a solid obstacle is in between.</summary>
        private float3 PullCameraIn(float3 head, float3 cam)
        {
            var obstacles = new List<Obstacle>();
            float3 mid = (head + cam) * 0.5f;
            m_Probe.QueryObstacles(new Vec2(mid.x, mid.z), math.distance(head, cam) * 0.5f + 1f, obstacles);
            if (obstacles.Count == 0)
            {
                return cam;
            }
            const int steps = 8;
            float3 last = head;
            for (int i = 1; i <= steps; i++)
            {
                float3 p = math.lerp(head, cam, i / (float)steps);
                foreach (Obstacle o in obstacles)
                {
                    if (o.Kind == ObstacleKind.Creature)
                    {
                        continue;
                    }
                    if (p.y > o.YMin && p.y < o.YMax && Overlap.CircleVsBox(new Vec2(p.x, p.z), 0.25f, o.Footprint).Overlaps)
                    {
                        return last;
                    }
                }
                last = p;
            }
            return cam;
        }

        public void ApplyCamera() => m_Camera.Apply();

        // =====================================================================================================
        // Test/diagnostic helpers (used by the bridge; never by normal play)
        // =====================================================================================================

        public void TeleportAvatar(float3 p)
        {
            if (m_Sm.State != PlayerState.OnFoot)
            {
                return;
            }
            m_Motor.TryPlace(m_Probe, ref m_Avatar, new Vec2(p.x, p.z), p.y + 1f);
        }

        public void TeleportVehicle(float3 p, float yaw)
        {
            if (m_Vehicle == null)
            {
                return;
            }
            m_Vehicle.State.Position = new Vec3(p.x, p.y, p.z);
            m_Vehicle.State.Yaw = yaw;
            m_Vehicle.State.Speed = 0f;
            m_Vehicle.State.Grounded = true;
            m_Vehicle.State.VerticalSpeed = 0f;
        }

        private float m_NearestCacheTime;
        private float m_NearestCache = float.MaxValue;

        /// <summary>Distance to the nearest vehicle the player could enter (cached 4 times a second; for prompts).</summary>
        public float NearestEnterableDistance()
        {
            float now = UTime.realtimeSinceStartup;
            if (now - m_NearestCacheTime < 0.25f)
            {
                return m_NearestCache;
            }
            m_NearestCacheTime = now;
            float3 me = m_Avatar.Position.ToF3();
            float best = float.MaxValue;
            foreach (ControlledVehicle p in m_ParkedVehicles)
            {
                if (p.Entity != Entity.Null)
                {
                    best = math.min(best, DistanceToVehicle(me, p.State, p.Spec));
                }
            }
            if (best > 4f && Sandbox && m_Probe.IsReady)
            {
                float d = 4.0f;
                if (FindBorrowableNative(me, ref d) != null)
                {
                    best = d;
                }
            }
            m_NearestCache = best;
            return best;
        }

        public IReadOnlyList<ControlledVehicle> ParkedVehicles => m_ParkedVehicles;

        /// <summary>Set by Career when the vehicle in use is immobilized (fuel or breakdown).</summary>
        public bool ThrottleBlocked { get; set; }

        /// <summary>Removes a parked career vehicle from the world (returned, sold, or released by Career).</summary>
        public void RemoveParkedVehicle(ControlledVehicle v)
        {
            if (v == null || !m_ParkedVehicles.Remove(v))
            {
                return;
            }
            ReleaseVehicle(v, 0f);
        }
        public Entity AvatarEntity => m_AvatarEntity;
        public float CameraYaw { get => m_CamYaw; set => m_CamYaw = value; }
        public float CameraPitch { get => m_CamPitch; set => m_CamPitch = value; }
        public int ActorCount => m_ActorOps != null ? m_ActorOps.CountActors() : 0;
    }

    public static class VecExtensions
    {
        public static float3 ToF3(this Vec3 v) => new float3(v.X, v.Y, v.Z);
    }

    /// <summary>Maps game prefab data to player handling (SPEC-DRIVING class parameters).</summary>
    public static class VehicleSpecs
    {
        public static VehicleSpec FromPrefab(CarPrefabInfo info)
        {
            var s = new VehicleSpec
            {
                Name = info.Name,
                Width = math.clamp(info.Size.x * CityWorldProbe.VehicleBodyWidthFactor, 1.4f, 3.0f),
                Height = math.clamp(info.Size.y, 1.2f, 4.5f),
                Length = math.clamp(info.Size.z, 3f, 18f),
            };
            s.WheelBase = s.Length * 0.6f;
            switch (info.Category)
            {
                case "bus":
                    s.MaxSpeed = 22f;
                    s.Acceleration = 1.6f;
                    s.BrakeDeceleration = 6f;
                    s.MaxSteerAngle = 40f * MathX.Deg2Rad;
                    s.HighSpeedSteerFactor = 0.35f;
                    s.SteerRate = 1.4f;
                    s.MaxReverseSpeed = 4f;
                    break;
                case "truck":
                    s.MaxSpeed = 24f;
                    s.Acceleration = 1.8f;
                    s.BrakeDeceleration = 6.5f;
                    s.MaxSteerAngle = 38f * MathX.Deg2Rad;
                    s.HighSpeedSteerFactor = 0.3f;
                    s.SteerRate = 1.6f;
                    s.MaxReverseSpeed = 4.5f;
                    break;
                case "van":
                    s.MaxSpeed = 30f;
                    s.Acceleration = 2.8f;
                    s.BrakeDeceleration = 7.5f;
                    break;
                default:
                    s.MaxSpeed = info.MaxSpeed > 5f ? math.clamp(info.MaxSpeed, 25f, 45f) : 38f;
                    s.Acceleration = 3.6f;
                    s.BrakeDeceleration = 8.5f;
                    break;
            }
            return s;
        }
    }
}
