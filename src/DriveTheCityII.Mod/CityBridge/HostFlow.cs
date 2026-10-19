using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Colossal;
using Colossal.IO.AssetDatabase;
using Colossal.Serialization.Entities;
using DriveTheCityII.Diagnostics;
using Game;
using Game.Assets;
using Game.City;
using Game.Prefabs.Modes;
using Game.SceneFlow;
using Game.Simulation;
using Game.UI;
using Game.UI.Menu;
using Unity.Entities;

namespace DriveTheCityII.CityBridge
{
    /// <summary>
    /// Host game flow adapter: main menu, new city, save, load (CityBridge, SPEC-CITY). Used by the test bridge and
    /// by Persistence. It deliberately avoids the menu UI path for new cities because that path also overwrites
    /// the player's persisted default options (see refcache note 01, section 9).
    /// </summary>
    public static class HostFlow
    {
        /// <summary>Prefix of every city and save created by this project's tests (disposable lab data).</summary>
        public const string LabPrefix = "DTC2-LAB";

        private static World W => World.DefaultGameObjectInjectionWorld;

        public static GameManager Game => GameManager.instance;

        public static bool IsMainMenuReady =>
            Game != null && Game.gameMode == GameMode.MainMenu && !Game.isGameLoading && Game.state == GameManager.State.WorldReady;

        public static bool IsCityReady => Game != null && Game.gameMode == GameMode.Game && !Game.isGameLoading;

        public static string CityName
        {
            get
            {
                var cfg = W?.GetExistingSystemManaged<CityConfigurationSystem>();
                return cfg?.cityName ?? string.Empty;
            }
        }

        public static bool IsLabCity => IsCityReady && CityName.StartsWith(LabPrefix, StringComparison.Ordinal);

        /// <summary>All map metadata, enumerated exactly like the game's menu (MenuHelpers.UpdateMeta).</summary>
        public static IEnumerable<Metadata<MapInfo>> Maps() => AssetDatabase.global.GetAssets<Metadata<MapInfo>>();

        public static string MapDiagnostics() =>
            $"global<Metadata<MapInfo>>={AssetDatabase.global.GetAssets<Metadata<MapInfo>>().Count()} " +
            $"global<MapMetadata>={AssetDatabase.global.GetAssets<MapMetadata>().Count()} " +
            $"game<MapMetadata>={AssetDatabase.game.GetAssets<MapMetadata>().Count()}";

        /// <summary>Same rule as the game's menu: built-in maps need their content owned; others must be playable.</summary>
        public static bool IsPlayableMap(Metadata<MapInfo> meta)
        {
            if (meta?.target == null)
            {
                return false;
            }
            if (meta.database is AssetDatabase<Colossal.IO.AssetDatabase.Game>)
            {
                return Game.ArePrerequisitesMet(meta);
            }
            return meta.target.isPlayable;
        }

        public static Metadata<MapInfo> FindMap(string nameOrId)
        {
            foreach (Metadata<MapInfo> m in Maps())
            {
                if (!IsPlayableMap(m))
                {
                    continue;
                }
                MapInfo info = m.target;
                if (string.Equals(info.displayName, nameOrId, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(info.id, nameOrId, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(m.id.ToString(), nameOrId, StringComparison.OrdinalIgnoreCase))
                {
                    return m;
                }
            }
            return null;
        }

        public static IEnumerable<SaveGameMetadata> Saves() =>
            AssetDatabase.global.GetAssets<SaveGameMetadata>().Where(s => s.isValidSaveGame);

        public static SaveGameMetadata FindLabSave(string name)
        {
            if (!name.StartsWith(LabPrefix, StringComparison.Ordinal))
            {
                return null;
            }
            return Saves()
                .Where(s => s.target != null && string.Equals(s.target.displayName, name, StringComparison.Ordinal))
                .OrderByDescending(s => s.target.lastModified)
                .FirstOrDefault();
        }

        /// <summary>Starts a new city named <paramref name="cityName"/> (must carry the lab prefix) on a map.</summary>
        public static Task<bool> StartNewLabCity(Metadata<MapInfo> map, string cityName)
        {
            if (!cityName.StartsWith(LabPrefix, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Test cities must start with " + LabPrefix);
            }
            if (!IsMainMenuReady)
            {
                throw new InvalidOperationException("New city requires the main menu");
            }
            MapInfo info = map.target;
            var cfg = W.GetExistingSystemManaged<CityConfigurationSystem>();
            cfg.overrideLoadedOptions = true;
            cfg.overrideCityName = cityName;
            cfg.overrideThemeName = string.IsNullOrEmpty(info.theme) ? "European" : info.theme;
            cfg.overrideLeftHandTraffic = false;
            cfg.overrideNaturalDisasters = false;
            cfg.overrideUnlockAll = true;
            cfg.overrideUnlimitedMoney = true;
            cfg.overrideUnlockMapTiles = true;
            var mms = W.GetExistingSystemManaged<MapMetadataSystem>();
            mms.mapName = info.displayName;
            mms.prefabReferences = info.prefabReferences;
            W.GetExistingSystemManaged<GameModeSystem>().overrideMode = null;
            W.GetExistingSystemManaged<TimeSystem>().startingYear = info.startingYear != -1 ? info.startingYear : DateTime.Now.Year;
            DtcLog.Info($"HostFlow: new lab city '{cityName}' on map '{info.displayName}'");
            return RunExclusive(() => Game.Load(GameMode.Game, Purpose.NewGame, map));
        }

        /// <summary>
        /// Saves the current lab city under a lab save name and reports real success (the game's Save task returns
        /// true even on failure; success comes from the onGameSaveLoad event).
        /// </summary>
        public static async Task<bool> SaveLabCity(string saveName)
        {
            if (!IsLabCity)
            {
                throw new InvalidOperationException("Saving is only automated for lab cities");
            }
            if (!saveName.StartsWith(LabPrefix, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Lab saves must start with " + LabPrefix);
            }
            var tcs = new TaskCompletionSource<bool>();
            void Handler(string name, string preview, bool start, bool success)
            {
                if (!start && name == saveName)
                {
                    tcs.TrySetResult(success);
                }
            }
            Game.onGameSaveLoad += Handler;
            try
            {
                SaveInfo meta = W.GetExistingSystemManaged<MenuUISystem>().GetSaveInfo(autoSave: false);
                await RunExclusive(() => Game.Save(saveName, meta, AssetDatabase.user, (ScreenCaptureHelper.AsyncRequest)null));
                Task done = await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromSeconds(120)));
                bool ok = done == tcs.Task && tcs.Task.Result;
                DtcLog.Info($"HostFlow: save '{saveName}' success={ok}");
                return ok;
            }
            finally
            {
                Game.onGameSaveLoad -= Handler;
            }
        }

        /// <summary>Content id of the populated city the game ships for its built-in benchmark (game content, read-only).</summary>
        public const string BenchmarkSaveId = "9b18906ee9ca2ef3073e3cb3eb44114d";

        public static SaveGameMetadata FindBenchmarkCity() =>
            AssetDatabase.global.GetAssets<SaveGameMetadata>().FirstOrDefault(s =>
                s.target != null &&
                (string.Equals(s.target.id, BenchmarkSaveId, StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(s.id.guid.ToString(), BenchmarkSaveId, StringComparison.OrdinalIgnoreCase)));

        /// <summary>
        /// Loads the game's built-in benchmark city (shipped game content, not a player save) and renames it with the lab
        /// prefix so that every later save of it is a disposable DTC2-LAB save.
        /// </summary>
        public static async Task<bool> LoadBenchmarkAsLab(string suffix)
        {
            if (!IsMainMenuReady)
            {
                throw new InvalidOperationException("Requires the main menu");
            }
            SaveGameMetadata bench = FindBenchmarkCity();
            if (bench == null)
            {
                throw new InvalidOperationException("Benchmark city not found in game content");
            }
            SaveInfo info = bench.target;
            W.GetExistingSystemManaged<MapMetadataSystem>().mapName = info.mapName;
            W.GetExistingSystemManaged<MapMetadataSystem>().prefabReferences = info.prefabReferences;
            var cfg = W.GetExistingSystemManaged<CityConfigurationSystem>();
            cfg.overrideLoadedOptions = false;
            cfg.overrideThemeName = null;
            W.GetExistingSystemManaged<GameModeSystem>().overrideMode = info.gameMode;
            DtcLog.Info($"HostFlow: loading built-in benchmark city '{info.cityName}' as a lab city");
            bool ok = await RunExclusive(() => Game.Load(GameMode.Game, Purpose.LoadGame, bench));
            if (ok)
            {
                cfg.cityName = LabPrefix + "-" + suffix;
                DtcLog.Info($"HostFlow: benchmark city renamed to '{cfg.cityName}'");
            }
            return ok;
        }

        public static Task<bool> LoadLabSave(string saveName)
        {
            SaveGameMetadata meta = FindLabSave(saveName);
            if (meta == null)
            {
                throw new InvalidOperationException("Lab save not found: " + saveName);
            }
            SaveInfo info = meta.target;
            W.GetExistingSystemManaged<MapMetadataSystem>().mapName = info.mapName;
            W.GetExistingSystemManaged<MapMetadataSystem>().prefabReferences = info.prefabReferences;
            var cfg = W.GetExistingSystemManaged<CityConfigurationSystem>();
            cfg.overrideLoadedOptions = false;
            cfg.overrideThemeName = null;
            W.GetExistingSystemManaged<GameModeSystem>().overrideMode = info.gameMode;
            DtcLog.Info($"HostFlow: load lab save '{saveName}' ({info.lastModified:O})");
            return RunExclusive(() => Game.Load(GameMode.Game, Purpose.LoadGame, meta));
        }

        public static Task<bool> ToMainMenu()
        {
            var cfg = W.GetExistingSystemManaged<CityConfigurationSystem>();
            cfg.overrideLoadedOptions = false;
            cfg.overrideThemeName = null;
            return RunExclusive(() => Game.MainMenu());
        }

        /// <summary>Runs a save/load through the game's single-slot queue, like the UI does, and returns its result.</summary>
        private static async Task<bool> RunExclusive(Func<Task<bool>> op)
        {
            bool result = false;
            bool ran = false;
            await TaskManager.instance.EnqueueTask("SaveLoadGame", async () =>
            {
                ran = true;
                result = await op();
            }, 1);
            if (!ran)
            {
                DtcLog.Warn("HostFlow: the game's save/load queue was busy; operation dropped");
            }
            return ran && result;
        }

        public static float SimulationSpeed
        {
            get => W?.GetExistingSystemManaged<SimulationSystem>()?.selectedSpeed ?? 0f;
            set
            {
                var sim = W?.GetExistingSystemManaged<SimulationSystem>();
                if (sim != null)
                {
                    sim.selectedSpeed = value;
                }
            }
        }

        public static uint SimulationFrame => W?.GetExistingSystemManaged<SimulationSystem>()?.frameIndex ?? 0u;

        public static Guid SessionGuid => global::Game.PSI.Telemetry.GetCurrentSession();
    }
}
