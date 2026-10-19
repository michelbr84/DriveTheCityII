using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DriveTheCityII.CityBridge;
using DriveTheCityII.Diagnostics;
using Game;
using Game.SceneFlow;
using Unity.Entities;
using UnityEngine;
using UTime = UnityEngine.Time;

namespace DriveTheCityII.Bridge
{
    /// <summary>
    /// Local test bridge (TEST_STRATEGY: in-game harness). OFF unless the marker file
    /// %LOCALAPPDATA%\DriveTheCityII\test\bridge.enabled exists, which only the project's test-launch script creates.
    /// It reads command files written by tools\bridge.ps1, runs them in order on the main thread, and appends results to
    /// bridge.log. City-changing commands (new city, save, load, quit) work only on disposable "DTC2-LAB" cities.
    /// Commands executed here are host execution, but any "input" they inject is simulated input, and reports say so.
    /// </summary>
    public static class TestBridge
    {
        public static string Root => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DriveTheCityII", "test");
        public static string MarkerPath => Path.Combine(Root, "bridge.enabled");
        public static string CommandPath => Path.Combine(Root, "commands.txt");
        public static string LogPath => Path.Combine(Root, "bridge.log");
        public static string CaptureDir => Path.Combine(Root, "captures");

        public static bool IsEnabled()
        {
            try
            {
                return File.Exists(MarkerPath);
            }
            catch
            {
                return false;
            }
        }

        private static readonly object s_LogLock = new object();

        public static void Log(string line)
        {
            string text = $"{DateTime.Now:HH:mm:ss.fff} f={UTime.frameCount} {line}";
            lock (s_LogLock)
            {
                try
                {
                    File.AppendAllText(LogPath, text + Environment.NewLine, Encoding.UTF8);
                }
                catch (Exception ex)
                {
                    DtcLog.Warn("bridge log write failed: " + ex.Message);
                }
            }
            DtcLog.Info("[bridge] " + line);
        }

        /// <summary>Extension point: other modules register command handlers (e.g. "player", "drive").</summary>
        public static readonly Dictionary<string, Func<string[], IBridgeOp>> Handlers = new Dictionary<string, Func<string[], IBridgeOp>>(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>A command in progress. Update returns true when finished; Result holds the outcome text.</summary>
    public interface IBridgeOp
    {
        bool Update();
        bool Ok { get; }
        string Result { get; }
    }

    public sealed class InstantOp : IBridgeOp
    {
        public InstantOp(bool ok, string result)
        {
            Ok = ok;
            Result = result;
        }

        public bool Update() => true;
        public bool Ok { get; }
        public string Result { get; }
    }

    public sealed class TaskOp : IBridgeOp
    {
        private readonly Task<bool> m_Task;
        private readonly string m_What;
        private readonly float m_Deadline;

        public TaskOp(Task<bool> task, string what, float timeoutSeconds)
        {
            m_Task = task;
            m_What = what;
            m_Deadline = UTime.realtimeSinceStartup + timeoutSeconds;
        }

        public bool Ok { get; private set; }
        public string Result { get; private set; } = string.Empty;

        public bool Update()
        {
            if (m_Task.IsCompleted)
            {
                if (m_Task.IsFaulted)
                {
                    Ok = false;
                    Result = m_What + " faulted: " + m_Task.Exception?.GetBaseException().Message;
                }
                else
                {
                    Ok = m_Task.Result;
                    Result = m_What + " -> " + m_Task.Result;
                }
                return true;
            }
            if (UTime.realtimeSinceStartup > m_Deadline)
            {
                Ok = false;
                Result = m_What + " timed out";
                return true;
            }
            return false;
        }
    }

    public sealed class WaitOp : IBridgeOp
    {
        private readonly Func<bool> m_Condition;
        private readonly float m_Deadline;
        private readonly string m_What;
        private readonly int m_SettleFrames;
        private int m_Satisfied;

        public WaitOp(Func<bool> condition, float timeoutSeconds, string what, int settleFrames = 1)
        {
            m_Condition = condition;
            m_Deadline = UTime.realtimeSinceStartup + timeoutSeconds;
            m_What = what;
            m_SettleFrames = Math.Max(1, settleFrames);
        }

        public bool Ok { get; private set; }
        public string Result { get; private set; } = string.Empty;

        public bool Update()
        {
            bool c;
            try
            {
                c = m_Condition();
            }
            catch (Exception ex)
            {
                Ok = false;
                Result = m_What + " error: " + ex.Message;
                return true;
            }
            m_Satisfied = c ? m_Satisfied + 1 : 0;
            if (m_Satisfied >= m_SettleFrames)
            {
                Ok = true;
                Result = m_What + " reached";
                return true;
            }
            if (UTime.realtimeSinceStartup > m_Deadline)
            {
                Ok = false;
                Result = m_What + " timed out";
                return true;
            }
            return false;
        }
    }

    public sealed class FramesOp : IBridgeOp
    {
        private int m_Frames;
        private readonly Action m_Then;
        private readonly string m_What;

        public FramesOp(int frames, string what, Action then = null)
        {
            m_Frames = frames;
            m_What = what;
            m_Then = then;
        }

        public bool Ok { get; private set; } = true;
        public string Result => m_What;

        public bool Update()
        {
            if (--m_Frames > 0)
            {
                return false;
            }
            m_Then?.Invoke();
            return true;
        }
    }

    /// <summary>Main-thread executor of bridge command files.</summary>
    public partial class TestBridgeSystem : GameSystemBase
    {
        private readonly Queue<string> m_Pending = new Queue<string>();
        private IBridgeOp m_Current;
        private string m_CurrentText;
        private float m_CurrentStart;
        private bool m_Enabled;
        private DateTime m_LastFileTime;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_Enabled = TestBridge.IsEnabled();
            if (m_Enabled)
            {
                Directory.CreateDirectory(TestBridge.CaptureDir);
                TestBridge.Log($"bridge online: mod {Mod.Version} build {BuildInfo.Stamp}, game {Application.version}, unity {Application.unityVersion}");
            }
        }

        protected override void OnUpdate()
        {
            if (!m_Enabled)
            {
                return;
            }
            PerfMonitor.Sample();
            // An "abort" file stops the running operation and drops queued commands (test tooling only).
            string abortPath = Path.Combine(TestBridge.Root, "abort");
            if (File.Exists(abortPath))
            {
                try
                {
                    File.Delete(abortPath);
                }
                catch (IOException)
                {
                }
                if (m_Current != null)
                {
                    TestBridge.Log($"< ERR [{UTime.realtimeSinceStartup - m_CurrentStart:0.00}s] {m_CurrentText} :: aborted by request");
                    m_Current = null;
                }
                m_Pending.Clear();
                if (Player.PlayerSystem.Instance != null)
                {
                    Player.PlayerSystem.Instance.Input.Injected = null;
                }
            }
            PollFile();
            int guard = 0;
            while (guard++ < 32)
            {
                if (m_Current == null)
                {
                    if (m_Pending.Count == 0)
                    {
                        return;
                    }
                    m_CurrentText = m_Pending.Dequeue();
                    m_CurrentStart = UTime.realtimeSinceStartup;
                    TestBridge.Log("> " + m_CurrentText);
                    try
                    {
                        m_Current = Dispatch(m_CurrentText);
                    }
                    catch (Exception ex)
                    {
                        m_Current = new InstantOp(false, "exception: " + ex.Message);
                        DtcLog.Error("bridge command failed: " + m_CurrentText, ex);
                    }
                }
                bool done;
                try
                {
                    done = m_Current.Update();
                }
                catch (Exception ex)
                {
                    m_Current = new InstantOp(false, "exception: " + ex.Message);
                    DtcLog.Error("bridge op failed: " + m_CurrentText, ex);
                    done = true;
                }
                if (!done)
                {
                    return;
                }
                float took = UTime.realtimeSinceStartup - m_CurrentStart;
                TestBridge.Log($"< {(m_Current.Ok ? "OK" : "ERR")} [{took:0.00}s] {m_CurrentText} :: {m_Current.Result}");
                m_Current = null;
            }
        }

        private void PollFile()
        {
            string path = TestBridge.CommandPath;
            try
            {
                if (!File.Exists(path))
                {
                    return;
                }
                DateTime t = File.GetLastWriteTimeUtc(path);
                if (t == m_LastFileTime)
                {
                    return;
                }
                string[] lines = File.ReadAllLines(path, Encoding.UTF8);
                File.Delete(path);
                m_LastFileTime = t;
                foreach (string raw in lines)
                {
                    string line = raw.Trim();
                    if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal))
                    {
                        continue;
                    }
                    m_Pending.Enqueue(line);
                }
            }
            catch (IOException)
            {
                // The writer may still hold the file; retry next frame.
            }
        }

        private static string[] Split(string text) => text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

        private static float F(string s) => float.Parse(s, CultureInfo.InvariantCulture);

        private IBridgeOp Dispatch(string text)
        {
            string[] a = Split(text);
            string cmd = a[0].ToLowerInvariant();
            switch (cmd)
            {
                case "ping":
                    return new InstantOp(true, "pong");
                case "mark":
                    return new InstantOp(true, text.Length > 5 ? text.Substring(5) : string.Empty);
                case "status":
                    return new InstantOp(true, Status());
                case "maps":
                    return new InstantOp(true, HostFlow.MapDiagnostics() + " :: " + string.Join(" | ", HostFlow.Maps()
                        .Where(HostFlow.IsPlayableMap)
                        .Select(m => $"{m.target.displayName} [{m.id}] theme={m.target.theme} area={m.target.area:0} builtin={m.database is Colossal.IO.AssetDatabase.AssetDatabase<Colossal.IO.AssetDatabase.Game>}")));
                case "saves":
                    return new InstantOp(true, string.Join(" | ", HostFlow.Saves()
                        .Where(s => s.target != null && s.target.displayName != null && s.target.displayName.StartsWith(HostFlow.LabPrefix, StringComparison.Ordinal))
                        .Select(s => $"{s.target.displayName} ({s.target.lastModified:O})")));
                case "newcity":
                {
                    // newcity <map name or id...> [= <city suffix>]
                    string rest = text.Substring(cmd.Length).Trim();
                    string suffix = DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
                    int eq = rest.IndexOf('=');
                    if (eq >= 0)
                    {
                        suffix = rest.Substring(eq + 1).Trim();
                        rest = rest.Substring(0, eq).Trim();
                    }
                    var map = HostFlow.FindMap(rest);
                    if (map == null)
                    {
                        return new InstantOp(false, "map not found or not owned: " + rest);
                    }
                    string name = HostFlow.LabPrefix + "-" + suffix;
                    return new TaskOp(HostFlow.StartNewLabCity(map, name), "newcity " + name, 600f);
                }
                case "benchcity":
                    return new TaskOp(HostFlow.LoadBenchmarkAsLab(a.Length > 1 ? a[1] : "Bench"), "benchmark city", 900f);
                case "waitcity":
                    return new WaitOp(() => HostFlow.IsCityReady, a.Length > 1 ? F(a[1]) : 600f, "city ready", 30);
                case "waitmenu":
                    return new WaitOp(() => HostFlow.IsMainMenuReady, a.Length > 1 ? F(a[1]) : 300f, "main menu", 30);
                case "wait":
                {
                    float until = UTime.realtimeSinceStartup + (a.Length > 1 ? F(a[1]) : 1f);
                    return new WaitOp(() => UTime.realtimeSinceStartup >= until, 3600f, "wait");
                }
                case "frames":
                    return new FramesOp(a.Length > 1 ? int.Parse(a[1], CultureInfo.InvariantCulture) : 1, "frames");
                case "save":
                    return new TaskOp(HostFlow.SaveLabCity(a[1]), "save " + a[1], 300f);
                case "load":
                    return new TaskOp(HostFlow.LoadLabSave(a[1]), "load " + a[1], 600f);
                case "menu":
                    return new TaskOp(HostFlow.ToMainMenu(), "main menu", 300f);
                case "speed":
                    if (!HostFlow.IsCityReady)
                    {
                        return new InstantOp(false, "no city");
                    }
                    HostFlow.SimulationSpeed = F(a[1]);
                    return new InstantOp(true, "speed=" + HostFlow.SimulationSpeed);
                case "shot":
                {
                    string name = new string((a.Length > 1 ? a[1] : "shot").Where(c => char.IsLetterOrDigit(c) || c == '-' || c == '_').ToArray());
                    string file = Path.Combine(TestBridge.CaptureDir, $"{DateTime.Now:yyyyMMdd-HHmmss}-{name}.png");
                    ScreenCapture.CaptureScreenshot(file);
                    return new FramesOp(3, "screenshot " + Path.GetFileName(file));
                }
                case "quit":
                    if (HostFlow.IsCityReady && !HostFlow.IsLabCity)
                    {
                        return new InstantOp(false, "refusing to quit from a non-lab city");
                    }
                    return new FramesOp(5, "quitting", () => Application.Quit());
                default:
                    if (TestBridge.Handlers.TryGetValue(cmd, out var handler))
                    {
                        return handler(a);
                    }
                    return new InstantOp(false, "unknown command");
            }
        }

        private static string Status()
        {
            GameManager gm = GameManager.instance;
            var sb = new StringBuilder();
            sb.Append("mode=").Append(gm.gameMode)
              .Append(" state=").Append(gm.state)
              .Append(" loading=").Append(gm.isGameLoading)
              .Append(" city='").Append(HostFlow.CityName).Append('\'')
              .Append(" lab=").Append(HostFlow.IsLabCity)
              .Append(" speed=").Append(HostFlow.SimulationSpeed.ToString(CultureInfo.InvariantCulture))
              .Append(" simFrame=").Append(HostFlow.SimulationFrame)
              .Append(" fps=").Append((1f / Math.Max(1e-4f, UTime.smoothDeltaTime)).ToString("0", CultureInfo.InvariantCulture))
              .Append(" focus=").Append(Application.isFocused)
              .Append(" conflict=").Append(Runtime.ConflictDetected);
            foreach (var p in StatusProviders)
            {
                try
                {
                    sb.Append(' ').Append(p());
                }
                catch (Exception ex)
                {
                    sb.Append(" [status provider error: ").Append(ex.Message).Append(']');
                }
            }
            return sb.ToString();
        }

        /// <summary>Other modules add fragments to the status line.</summary>
        public static readonly List<Func<string>> StatusProviders = new List<Func<string>>();
    }
}
