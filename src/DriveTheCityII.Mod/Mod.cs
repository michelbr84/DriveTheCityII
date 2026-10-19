using System;
using Colossal.Logging;
using DriveTheCityII.Diagnostics;
using Game;
using Game.Modding;
using Game.SceneFlow;

namespace DriveTheCityII
{
    /// <summary>
    /// Entry point loaded by the game's ModManager from &lt;user data&gt;\Mods\DriveTheCityII\ (ADR-0005).
    /// It only wires modules; gameplay lives in the systems it registers (SPEC-MODULES 4.1).
    /// </summary>
    public sealed class Mod : IMod
    {
        public const string Id = "DriveTheCityII";
        public static string Version => BuildInfo.InformationalVersion;

        public static Mod Instance { get; private set; }

        /// <summary>Folder of the loaded mod (assemblies are loaded from bytes, so Assembly.Location is empty).</summary>
        public static string ModFolder { get; private set; }

        public void OnLoad(UpdateSystem updateSystem)
        {
            Instance = this;
            DtcLog.Init();
            DtcLog.Info($"Drive the City II {Version} loading (build {BuildInfo.Stamp})");
            try
            {
                if (GameManager.instance.modManager.TryGetExecutableAsset(this, out var asset))
                {
                    ModFolder = System.IO.Path.GetDirectoryName(asset.path);
                    DtcLog.Info($"Mod asset: {asset.name} at {DtcLog.Sanitize(asset.path)}");
                }
                Runtime.Startup(updateSystem);
                DtcLog.Info("Drive the City II systems registered");
            }
            catch (Exception ex)
            {
                DtcLog.Error("Startup failed; the mod stays inert", ex);
            }
        }

        public void OnDispose()
        {
            DtcLog.Info("Drive the City II disposing");
            try
            {
                Runtime.Shutdown();
            }
            catch (Exception ex)
            {
                DtcLog.Error("Shutdown error", ex);
            }
            Instance = null;
        }
    }
}
