using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Colossal.PSI.Environment;
using Colossal.Serialization.Entities;
using DriveTheCityII.Core.Persistence;
using DriveTheCityII.Diagnostics;
using Game;
using UnityEngine;

namespace DriveTheCityII.Persistence
{
    /// <summary>Real file operations for the sidecar store: temp file + replace, so a crash never leaves a partial file.</summary>
    public sealed class FileSidecar : ISidecarFiles
    {
        public bool Exists(string path) => File.Exists(path);

        public string ReadText(string path)
        {
            try
            {
                return File.ReadAllText(path, System.Text.Encoding.UTF8);
            }
            catch (IOException)
            {
                return null;
            }
        }

        public void WriteTextAtomic(string path, string text)
        {
            string dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }
            string tmp = path + ".tmp";
            File.WriteAllText(tmp, text ?? string.Empty, System.Text.Encoding.UTF8);
            if (File.Exists(path))
            {
                File.Replace(tmp, path, null);
            }
            else
            {
                File.Move(tmp, path);
            }
        }

        public IEnumerable<string> ListFiles(string directory, string pattern) =>
            Directory.Exists(directory) ? Directory.GetFiles(directory, pattern) : Array.Empty<string>();

        public void Delete(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        public void EnsureDirectory(string directory) => Directory.CreateDirectory(directory);
    }

    /// <summary>
    /// Persistence module (SPEC-MODULES 4.7): owns save identity and the bytes of persisted career data. The Career
    /// module supplies a snapshot through <see cref="CareerProvider"/> and receives restored data through
    /// <see cref="CareerRestorer"/>; it never touches files or the save stream itself.
    /// </summary>
    public static class PersistenceRuntime
    {
        public static string CityId { get; private set; } = string.Empty;
        public static long Revision { get; private set; }
        public static EnvelopeStatus LastLoadStatus { get; private set; } = EnvelopeStatus.NoCareerData;
        public static string LastLoadNote { get; private set; } = string.Empty;
        public static int SavesWritten { get; private set; }

        /// <summary>Returns the career snapshot to persist (null = no career data).</summary>
        public static Func<JsonValue> CareerProvider;

        /// <summary>Called after a city finished loading with the validated career (null = new career) and the outcome.</summary>
        public static Action<JsonValue, EnvelopeStatus, string> CareerRestorer;

        /// <summary>Player data provider (stable data only; never transitional states, INV-CTRL-08).</summary>
        public static Func<JsonValue> PlayerProvider;

        private static string s_PendingText;
        private static bool s_HasPending;
        private static string s_PreservedNewerText;
        private static SidecarStore s_Store;

        public static string SidecarRoot => Path.Combine(EnvPath.kUserDataPath, "ModsData", "DriveTheCityII");

        private static SidecarStore Store => s_Store ??= new SidecarStore(SidecarRoot, new FileSidecar());

        internal static void OnDeserialized(string text)
        {
            s_PendingText = text;
            s_HasPending = true;
        }

        internal static void OnDefaults()
        {
            s_PendingText = null;
            s_HasPending = true;
        }

        /// <summary>Builds the text written into the save; called by the game's serializer for every save and autosave.</summary>
        internal static string BuildForSave()
        {
            if (!string.IsNullOrEmpty(s_PreservedNewerText))
            {
                // Data from a newer mod version: preserved verbatim, never overwritten by this version (SPEC-SAVE).
                return s_PreservedNewerText;
            }
            if (string.IsNullOrEmpty(CityId))
            {
                CityId = SaveEnvelope.NewCityId();
            }
            Revision = Math.Max(Revision, MaxSidecarRevision(CityId)) + 1;
            var env = new SaveEnvelope
            {
                CityId = CityId,
                Revision = Revision,
                SavedUtc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture),
                ModVersion = Mod.Version,
                GameVersion = Application.version,
                Career = SafeCall(CareerProvider),
                Player = SafeCall(PlayerProvider),
            };
            string text = env.ToJsonText();
            try
            {
                Store.SaveRevision(env);
            }
            catch (Exception ex)
            {
                DtcLog.Error("Sidecar backup write failed (the in-save copy is unaffected)", ex);
            }
            SavesWritten++;
            DtcLog.Info($"Persistence: wrote career envelope city={CityId} revision={Revision} bytes={text.Length} career={(env.Career != null)}");
            return text;
        }

        private static JsonValue SafeCall(Func<JsonValue> f)
        {
            if (f == null)
            {
                return null;
            }
            try
            {
                return f();
            }
            catch (Exception ex)
            {
                DtcLog.Error("Snapshot provider failed; career omitted from this save", ex);
                return null;
            }
        }

        private static long MaxSidecarRevision(string cityId)
        {
            long max = 0;
            try
            {
                foreach (string f in new FileSidecar().ListFiles(Store.CityDirectory(cityId), "rev-*.json"))
                {
                    string n = Path.GetFileNameWithoutExtension(f);
                    if (n.Length > 4 && long.TryParse(n.Substring(4), NumberStyles.Integer, CultureInfo.InvariantCulture, out long r))
                    {
                        max = Math.Max(max, r);
                    }
                }
            }
            catch (IOException)
            {
            }
            return max;
        }

        /// <summary>Validates what the loaded save contained and hands the result to Career (load protocol, SPEC-SAVE).</summary>
        internal static void ApplyPendingAfterLoad()
        {
            if (!s_HasPending)
            {
                return;
            }
            s_HasPending = false;
            s_PreservedNewerText = null;
            string text = s_PendingText;
            s_PendingText = null;
            EnvelopeLoadResult res = SaveEnvelope.Parse(text);
            string stamp = DateTime.UtcNow.ToString("yyyyMMddTHHmmssZ", CultureInfo.InvariantCulture);
            JsonValue career = null;
            string note;
            switch (res.Status)
            {
                case EnvelopeStatus.Ok:
                    CityId = res.Envelope.CityId;
                    Revision = res.Envelope.Revision;
                    career = res.Envelope.Career;
                    note = $"restored revision {Revision}";
                    break;
                case EnvelopeStatus.NoCareerData:
                    CityId = SaveEnvelope.NewCityId();
                    Revision = 0;
                    note = "no career data in this save (new career)";
                    break;
                case EnvelopeStatus.NewerSchemaRefused:
                    s_PreservedNewerText = text;
                    note = "career data from a newer version; preserved and left untouched";
                    break;
                default:
                {
                    string qpath = SafeQuarantine(res.Envelope?.CityId ?? "unknown", text, res.Status + ": " + res.Error, stamp);
                    if (res.Envelope != null && Store.TryLoad(res.Envelope.CityId, res.Envelope.Revision, true, out SaveEnvelope backup, out long got))
                    {
                        CityId = backup.CityId;
                        Revision = Math.Max(res.Envelope.Revision, got);
                        career = backup.Career;
                        res.Status = got == res.Envelope.Revision ? EnvelopeStatus.Ok : EnvelopeStatus.ChecksumMismatch;
                        note = $"in-save data invalid ({res.Error}); restored sidecar revision {got}; invalid copy quarantined at {DtcLog.Sanitize(qpath)}";
                    }
                    else
                    {
                        CityId = res.Envelope?.CityId ?? SaveEnvelope.NewCityId();
                        Revision = res.Envelope?.Revision ?? 0;
                        note = $"in-save data invalid ({res.Error}) and no backup; career disabled for this city until a new career is started; quarantined at {DtcLog.Sanitize(qpath)}";
                    }
                    break;
                }
            }
            LastLoadStatus = res.Status;
            LastLoadNote = note;
            DtcLog.Info($"Persistence: load status={res.Status} city={CityId} :: {note}");
            try
            {
                CareerRestorer?.Invoke(career, res.Status, note);
            }
            catch (Exception ex)
            {
                DtcLog.Error("Career restore failed", ex);
            }
        }

        private static string SafeQuarantine(string cityId, string text, string reason, string stamp)
        {
            try
            {
                return Store.Quarantine(cityId, text, reason, stamp);
            }
            catch (Exception ex)
            {
                DtcLog.Error("Quarantine write failed", ex);
                return "(not written)";
            }
        }
    }

    /// <summary>
    /// World system whose serializer writes the career envelope into every city save (IDefaultSerializable: the game
    /// calls SetDefaults for new cities and saves without our data). Only plain strings are written, so a save made
    /// with the mod still loads without it (the block is skipped with a warning; refcache note 01 §5.4).
    /// </summary>
    public partial class DtcSaveSystem : GameSystemBase, IDefaultSerializable
    {
        private const int kFormat = 1;

        protected override void OnUpdate()
        {
        }

        protected override void OnGameLoadingComplete(Purpose purpose, GameMode mode)
        {
            base.OnGameLoadingComplete(purpose, mode);
            if (mode == GameMode.Game)
            {
                PersistenceRuntime.ApplyPendingAfterLoad();
            }
        }

        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
        {
            string text;
            try
            {
                text = PersistenceRuntime.BuildForSave();
            }
            catch (Exception ex)
            {
                DtcLog.Error("Building the career envelope failed; writing an empty record so the save itself succeeds", ex);
                text = string.Empty;
            }
            writer.Write(kFormat);
            writer.Write(text ?? string.Empty);
        }

        public void Deserialize<TReader>(TReader reader) where TReader : IReader
        {
            reader.Read(out int format);
            reader.Read(out string text);
            PersistenceRuntime.OnDeserialized(format >= 1 ? text : null);
        }

        public void SetDefaults(Context context)
        {
            PersistenceRuntime.OnDefaults();
        }
    }
}
