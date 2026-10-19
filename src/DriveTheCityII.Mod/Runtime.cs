using System;
using System.Linq;
using DriveTheCityII.Bridge;
using DriveTheCityII.Diagnostics;
using Game;
using Game.SceneFlow;
using Unity.Entities;

namespace DriveTheCityII
{
    /// <summary>
    /// Composition root and Supervisor (SPEC-MODULES 4.1): registers systems in a fixed order, detects conflicting
    /// controllers, and holds session-wide flags (safe-disable, conflict, degraded modules).
    /// </summary>
    public static class Runtime
    {
        public static bool ConflictDetected { get; private set; }
        public static string ConflictReason { get; private set; } = string.Empty;
        public static bool TestMode { get; private set; }

        /// <summary>Assembly names of other vehicle-control mods that must not run together with ours (REQ-COMPAT-001).</summary>
        private static readonly string[] s_ConflictingAssemblies = { "BetaTestDrivingMod" };

        public static void Startup(UpdateSystem updateSystem)
        {
            TestMode = TestBridge.IsEnabled();
            DtcLog.Info($"Test bridge {(TestMode ? "ENABLED (lab sessions only)" : "disabled")}");

            // Bridge first so that it can drive the menus even if a later module fails.
            updateSystem.UpdateAt<TestBridgeSystem>(SystemUpdatePhase.MainLoop);
            updateSystem.UpdateAt<SessionGuardSystem>(SystemUpdatePhase.MainLoop);

            // CityBridge: structural entity operations where the game's Modification systems see them, and removal of
            // every mod actor right before a save is serialized.
            updateSystem.UpdateAt<CityBridge.ActorOpsSystem>(SystemUpdatePhase.Modification1);
            updateSystem.UpdateBefore<Game.Serialization.PreSerialize<CityBridge.ActorOpsSystem>>(SystemUpdatePhase.Serialize);
            updateSystem.UpdateAfter<CityBridge.ActorPostSerializeSystem, Game.Serialization.WriteSystem>(SystemUpdatePhase.Serialize);

            // Player: per frame, after the game's tools and modifications, before culling and rendering; camera pose
            // applied right before the game's camera system.
            updateSystem.UpdateBefore<Player.PlayerSystem, Game.Rendering.PreRenderSystem>(SystemUpdatePhase.MainLoop);
            updateSystem.UpdateBefore<Player.PlayerCameraSystem, Game.Rendering.CameraUpdateSystem>(SystemUpdatePhase.PreCulling);

            // Career host: after the Player module, before rendering.
            updateSystem.UpdateAfter<Careers.CareerSystem, Player.PlayerSystem>(SystemUpdatePhase.MainLoop);

            // Persistence: career envelope inside every city save (system serializer) plus sidecar backups.
            updateSystem.UpdateAt<Persistence.DtcSaveSystem>(SystemUpdatePhase.MainLoop);

            // World markers and route guidance, drawn right before the game's overlay renderer.
            updateSystem.UpdateBefore<CityBridge.WorldMarkerSystem, Game.Rendering.OverlayRenderSystem>(SystemUpdatePhase.Rendering);

            // UI: HUD view model and action routing.
            updateSystem.UpdateAt<UI.HudUISystem>(SystemUpdatePhase.UIUpdate);

            PlayerBridgeCommands.Register();

            GameManager.instance.onGameLoadingComplete += OnLoadingComplete;
            GameManager.instance.onGamePreload += OnGamePreload;
        }

        public static void Shutdown()
        {
            if (GameManager.instance != null)
            {
                GameManager.instance.onGameLoadingComplete -= OnLoadingComplete;
                GameManager.instance.onGamePreload -= OnGamePreload;
            }
        }

        /// <summary>Checks loaded assemblies for a conflicting vehicle controller. Called once mods are loaded.</summary>
        public static void CheckConflicts()
        {
            string found = AppDomain.CurrentDomain.GetAssemblies()
                .Select(a => a.GetName().Name)
                .FirstOrDefault(n => s_ConflictingAssemblies.Contains(n));
            if (found != null && !ConflictDetected)
            {
                ConflictDetected = true;
                ConflictReason = $"Conflicting vehicle-control mod loaded: {found}. Drive the City II stays inactive.";
                DtcLog.Warn(ConflictReason);
            }
        }

        private static void OnGamePreload(Colossal.Serialization.Entities.Purpose purpose, GameMode mode)
        {
            DtcLog.Info($"Host: preload purpose={purpose} mode={mode}");
        }

        private static void OnLoadingComplete(Colossal.Serialization.Entities.Purpose purpose, GameMode mode)
        {
            CheckConflicts();
            DtcLog.Info($"Host: loading complete purpose={purpose} mode={mode} city='{CityBridge.HostFlow.CityName}' session={CityBridge.HostFlow.SessionGuid}");
        }
    }
}
