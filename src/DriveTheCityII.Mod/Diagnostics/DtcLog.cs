using System;
using System.Collections.Generic;
using System.Reflection;
using Colossal.Logging;

namespace DriveTheCityII.Diagnostics
{
    /// <summary>
    /// Logging through the game's log manager (file: &lt;user data&gt;\Logs\DriveTheCityII.log). Repeated identical
    /// messages are collapsed into counts (SPEC-MODULES 4.9) and user-profile paths are replaced by placeholders.
    /// </summary>
    public static class DtcLog
    {
        private static ILog s_Log;
        private static readonly Dictionary<string, (float until, int suppressed)> s_RateLimit = new Dictionary<string, (float, int)>();
        private static readonly string s_Profile = Environment.GetEnvironmentVariable("USERPROFILE") ?? "\u0000";

        public static void Init()
        {
            if (s_Log != null)
            {
                return;
            }
            s_Log = LogManager.GetLogger("DriveTheCityII").SetShowsErrorsInUI(false);
        }

        public static string Sanitize(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return text;
            }
            return text.Replace(s_Profile, "%USERPROFILE%");
        }

        public static void Info(string message) => s_Log?.Info(Sanitize(message));

        public static void Warn(string message) => s_Log?.Warn(Sanitize(message));

        public static void Debug(string message) => s_Log?.Debug(Sanitize(message));

        public static void Error(string message, Exception ex = null)
        {
            if (s_Log == null)
            {
                return;
            }
            if (ex != null)
            {
                s_Log.Error(Sanitize(message + " :: " + ex));
            }
            else
            {
                s_Log.Error(Sanitize(message));
            }
        }

        /// <summary>Logs at most once per <paramref name="intervalSeconds"/> for a given key, reporting suppressed counts.</summary>
        public static void Throttled(string key, float now, float intervalSeconds, string message, bool warn = false)
        {
            if (s_RateLimit.TryGetValue(key, out var entry) && now < entry.until)
            {
                s_RateLimit[key] = (entry.until, entry.suppressed + 1);
                return;
            }
            int suppressed = s_RateLimit.TryGetValue(key, out entry) ? entry.suppressed : 0;
            s_RateLimit[key] = (now + intervalSeconds, 0);
            string text = suppressed > 0 ? $"{message} (+{suppressed} similar suppressed)" : message;
            if (warn)
            {
                Warn(text);
            }
            else
            {
                Info(text);
            }
        }
    }

    public static class BuildInfo
    {
        public static string Stamp
        {
            get
            {
                foreach (var a in typeof(BuildInfo).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>())
                {
                    if (a.Key == "DtcBuildStamp")
                    {
                        return a.Value;
                    }
                }
                return "unknown";
            }
        }

        public static string InformationalVersion =>
            typeof(BuildInfo).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "0.0.0";
    }
}
