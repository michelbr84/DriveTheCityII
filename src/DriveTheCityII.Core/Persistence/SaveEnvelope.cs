using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace DriveTheCityII.Core.Persistence
{
    /// <summary>Outcome of reading persisted career data (SPEC-SAVE load protocol).</summary>
    public enum EnvelopeStatus
    {
        Ok = 0,
        NoCareerData = 1,
        Corrupt = 2,
        ChecksumMismatch = 3,
        NewerSchemaRefused = 4,
    }

    public sealed class EnvelopeLoadResult
    {
        public EnvelopeStatus Status;
        public SaveEnvelope Envelope;
        public string Error = string.Empty;

        public bool IsOk => Status == EnvelopeStatus.Ok;
    }

    /// <summary>
    /// The record Drive the City II writes inside each city save (and as a sidecar backup): identity (a mod-owned
    /// city id and a revision counter), versions, and the career payload with a checksum (SPEC-SAVE, ADR-0003).
    /// </summary>
    public sealed class SaveEnvelope
    {
        public const string FormatName = "dtc2-career";
        public const int SchemaVersion = 1;

        public string CityId = string.Empty;
        public long Revision;
        public string SavedUtc = string.Empty;
        public string ModVersion = string.Empty;
        public string GameVersion = string.Empty;
        public int Schema = SchemaVersion;

        /// <summary>Career state (owned by the Career module); null when no career exists yet.</summary>
        public JsonValue Career;

        /// <summary>Stable, non-transitional player data (e.g. last stable mode, home id); may be null.</summary>
        public JsonValue Player;

        public static string NewCityId() => Guid.NewGuid().ToString("N");

        public string ToJsonText()
        {
            JsonValue o = JsonValue.Object()
                .Set("format", FormatName)
                .Set("schema", Schema)
                .Set("cityId", CityId)
                .Set("revision", Revision)
                .Set("savedUtc", SavedUtc)
                .Set("modVersion", ModVersion)
                .Set("gameVersion", GameVersion);
            string careerText = Career == null ? string.Empty : Career.ToJson(false);
            o.Set("career", Career ?? JsonValue.Null);
            o.Set("player", Player ?? JsonValue.Null);
            o.Set("checksum", Crc32.Hex(careerText));
            return o.ToJson(false);
        }

        public static EnvelopeLoadResult Parse(string text)
        {
            var r = new EnvelopeLoadResult();
            if (string.IsNullOrEmpty(text))
            {
                r.Status = EnvelopeStatus.NoCareerData;
                return r;
            }
            if (!JsonValue.TryParse(text, out JsonValue o, out string err) || o.Kind != JsonKind.Object)
            {
                r.Status = EnvelopeStatus.Corrupt;
                r.Error = err ?? "not an object";
                return r;
            }
            if (o["format"].AsString() != FormatName)
            {
                r.Status = EnvelopeStatus.Corrupt;
                r.Error = "unknown format";
                return r;
            }
            int schema = o["schema"].AsInt(-1);
            if (schema > SchemaVersion)
            {
                r.Status = EnvelopeStatus.NewerSchemaRefused;
                r.Error = $"schema {schema} is newer than supported {SchemaVersion}";
                return r;
            }
            if (schema < 1)
            {
                r.Status = EnvelopeStatus.Corrupt;
                r.Error = "missing schema";
                return r;
            }
            var env = new SaveEnvelope
            {
                Schema = schema,
                CityId = o["cityId"].AsString(string.Empty),
                Revision = o["revision"].AsLong(),
                SavedUtc = o["savedUtc"].AsString(string.Empty),
                ModVersion = o["modVersion"].AsString(string.Empty),
                GameVersion = o["gameVersion"].AsString(string.Empty),
                Career = o["career"].Kind == JsonKind.Null ? null : o["career"],
                Player = o["player"].Kind == JsonKind.Null ? null : o["player"],
            };
            if (string.IsNullOrEmpty(env.CityId) || env.Revision < 0)
            {
                r.Status = EnvelopeStatus.Corrupt;
                r.Error = "missing identity";
                return r;
            }
            string careerText = env.Career == null ? string.Empty : env.Career.ToJson(false);
            if (o["checksum"].AsString() != Crc32.Hex(careerText))
            {
                r.Status = EnvelopeStatus.ChecksumMismatch;
                r.Error = "career checksum mismatch";
                r.Envelope = env;
                return r;
            }
            // Migration hook: schema 1 is current; future versions upgrade env.Career here, step by step.
            r.Status = EnvelopeStatus.Ok;
            r.Envelope = env;
            return r;
        }
    }

    /// <summary>CRC-32 (IEEE) of UTF-8 text, as 8 hex digits.</summary>
    public static class Crc32
    {
        private static readonly uint[] s_Table = BuildTable();

        private static uint[] BuildTable()
        {
            var t = new uint[256];
            for (uint i = 0; i < 256; i++)
            {
                uint c = i;
                for (int k = 0; k < 8; k++)
                {
                    c = (c & 1) != 0 ? 0xEDB88320u ^ (c >> 1) : c >> 1;
                }
                t[i] = c;
            }
            return t;
        }

        public static uint Compute(byte[] data)
        {
            uint crc = 0xFFFFFFFFu;
            foreach (byte b in data)
            {
                crc = s_Table[(crc ^ b) & 0xFF] ^ (crc >> 8);
            }
            return crc ^ 0xFFFFFFFFu;
        }

        public static string Hex(string text) => Compute(Encoding.UTF8.GetBytes(text ?? string.Empty)).ToString("x8", CultureInfo.InvariantCulture);
    }

    /// <summary>File operations used by the sidecar store (abstracted for tests).</summary>
    public interface ISidecarFiles
    {
        bool Exists(string path);
        string ReadText(string path);

        /// <summary>Writes text so that a crash never leaves a partial file at <paramref name="path"/>.</summary>
        void WriteTextAtomic(string path, string text);

        IEnumerable<string> ListFiles(string directory, string pattern);
        void Delete(string path);
        void EnsureDirectory(string directory);
    }

    /// <summary>
    /// Sidecar backups of the career envelope, one file per (city id, revision), used to recover when the copy inside a
    /// save is unreadable. Invalid data is never deleted automatically; it is copied to a quarantine folder (REQ-SAVE-004).
    /// </summary>
    public sealed class SidecarStore
    {
        private readonly string m_Root;
        private readonly ISidecarFiles m_Files;

        public int KeepRevisions = 12;

        public SidecarStore(string root, ISidecarFiles files)
        {
            m_Root = root;
            m_Files = files;
        }

        public string CityDirectory(string cityId) => Combine(m_Root, Sanitize(cityId));

        public string RevisionPath(string cityId, long revision) =>
            Combine(CityDirectory(cityId), "rev-" + revision.ToString("D8", CultureInfo.InvariantCulture) + ".json");

        public void SaveRevision(SaveEnvelope env)
        {
            string dir = CityDirectory(env.CityId);
            m_Files.EnsureDirectory(dir);
            m_Files.WriteTextAtomic(RevisionPath(env.CityId, env.Revision), env.ToJsonText());
            Prune(dir);
        }

        /// <summary>Loads the exact revision, or the newest valid revision below it.</summary>
        public bool TryLoad(string cityId, long revision, bool allowEarlier, out SaveEnvelope env, out long loadedRevision)
        {
            env = null;
            loadedRevision = -1;
            string dir = CityDirectory(cityId);
            var revisions = new List<long>();
            foreach (string f in m_Files.ListFiles(dir, "rev-*.json"))
            {
                string name = FileName(f);
                if (name.Length > 8 && long.TryParse(name.Substring(4, name.Length - 9), NumberStyles.Integer, CultureInfo.InvariantCulture, out long r))
                {
                    if (r == revision || (allowEarlier && r < revision))
                    {
                        revisions.Add(r);
                    }
                }
            }
            revisions.Sort();
            for (int i = revisions.Count - 1; i >= 0; i--)
            {
                string path = RevisionPath(cityId, revisions[i]);
                EnvelopeLoadResult res = SaveEnvelope.Parse(m_Files.ReadText(path));
                if (res.IsOk && res.Envelope.CityId == cityId)
                {
                    env = res.Envelope;
                    loadedRevision = revisions[i];
                    return true;
                }
            }
            return false;
        }

        public string Quarantine(string cityId, string text, string reason, string utcStamp)
        {
            string dir = Combine(m_Root, "quarantine");
            m_Files.EnsureDirectory(dir);
            string baseName = Sanitize(cityId) + "-" + utcStamp;
            string path = Combine(dir, baseName + ".json");
            m_Files.WriteTextAtomic(path, text ?? string.Empty);
            m_Files.WriteTextAtomic(Combine(dir, baseName + ".reason.txt"), reason ?? string.Empty);
            return path;
        }

        private void Prune(string dir)
        {
            var files = new List<string>(m_Files.ListFiles(dir, "rev-*.json"));
            files.Sort(StringComparer.Ordinal);
            for (int i = 0; i < files.Count - KeepRevisions; i++)
            {
                m_Files.Delete(files[i]);
            }
        }

        private static string Sanitize(string s)
        {
            var sb = new StringBuilder();
            foreach (char c in s ?? string.Empty)
            {
                sb.Append(char.IsLetterOrDigit(c) || c == '-' || c == '_' ? c : '_');
            }
            return sb.Length == 0 ? "unknown" : sb.ToString();
        }

        private static string Combine(string a, string b) => a.EndsWith("/", StringComparison.Ordinal) || a.EndsWith("\\", StringComparison.Ordinal) ? a + b : a + "/" + b;

        private static string FileName(string path)
        {
            int i = Math.Max(path.LastIndexOf('/'), path.LastIndexOf('\\'));
            return i >= 0 ? path.Substring(i + 1) : path;
        }
    }
}
